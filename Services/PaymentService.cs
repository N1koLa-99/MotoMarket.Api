using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace MotoMarket.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IListingRepository _listingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBlobImageService _blobImageService;
    private readonly IMyPosSignatureService _signatureService;
    private readonly MyPosOptions _myPosOptions;

    public PaymentService(
        IListingRepository listingRepository,
        IPaymentRepository paymentRepository,
        IBlobImageService blobImageService,
        IMyPosSignatureService signatureService,
        IOptions<MyPosOptions> myPosOptions)
    {
        _listingRepository = listingRepository;
        _paymentRepository = paymentRepository;
        _blobImageService = blobImageService;
        _signatureService = signatureService;
        _myPosOptions = myPosOptions.Value;
    }

    public async Task<MyPosCheckoutStartResponse> StartMyPosCheckoutAsync(long userId, long pendingActionId)
    {
        var pending = await _paymentRepository.GetPendingActionByIdAsync(pendingActionId)
            ?? throw new InvalidOperationException("Pending action не е намерен.");

        if (pending.UserId != userId)
            throw new UnauthorizedAccessException("Нямаш право върху тази операция.");

        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Операцията вече не е активна.");

        if (pending.ExpiresAt.HasValue && pending.ExpiresAt.Value < DateTime.UtcNow)
        {
            await CleanupPendingActionAsync(pending, markAsFailed: true);
            throw new InvalidOperationException("Операцията е изтекла.");
        }

        // DEV MODE: виртуално success плащане
        if (_myPosOptions.SimulateSuccessfulPayments)
        {
            var simulatedProviderPaymentId = $"SIM-{pending.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            var listingId = await ExecutePendingActionOnceAsync(pending, simulatedProviderPaymentId);

            return new MyPosCheckoutStartResponse
            {
                PendingActionId = pending.Id,
                IsSimulated = true,
                IsCompleted = true,
                ListingId = listingId,

                Message = "Плащането е симулирано успешно и действието е изпълнено.",
                GatewayUrl = string.Empty,
                Method = "POST",
                OrderId = $"SIM-ORDER-{pending.Id}",
                Fields = new Dictionary<string, string>()
            };
        }

        var orderId = $"PA-{pending.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        await _paymentRepository.UpdatePendingActionProviderOrderIdAsync(pending.Id, orderId);

        var notifyUrl = $"{_myPosOptions.BaseCallbackUrl.TrimEnd('/')}/api/payments/mypos/notify";
        var okUrl = $"{_myPosOptions.BaseCallbackUrl.TrimEnd('/')}/api/payments/mypos/ok";
        var cancelUrl = $"{_myPosOptions.BaseCallbackUrl.TrimEnd('/')}/api/payments/mypos/cancel";

        var fields = new Dictionary<string, string>
        {
            ["IPCmethod"] = "IPCPurchase",
            ["IPCVersion"] = "1.4",
            ["IPCLanguage"] = string.IsNullOrWhiteSpace(_myPosOptions.Language) ? "EN" : _myPosOptions.Language,
            ["SID"] = _myPosOptions.Sid,
            ["WalletNumber"] = _myPosOptions.WalletNumber,
            ["KeyIndex"] = _myPosOptions.KeyIndex.ToString(),
            ["Amount"] = pending.AmountEUR.ToString("0.00", CultureInfo.InvariantCulture),
            ["Currency"] = "EUR",
            ["OrderID"] = orderId,
            ["URL_OK"] = okUrl,
            ["URL_Cancel"] = cancelUrl,
            ["URL_Notify"] = notifyUrl,
            ["ItemName"] = BuildItemName(pending),
            ["Note"] = $"Pending action {pending.Id} - {pending.ActionType}"
        };

        fields["Signature"] = _signatureService.Sign(fields);

        return new MyPosCheckoutStartResponse
        {
            PendingActionId = pending.Id,
            IsSimulated = false,
            IsCompleted = false,
            GatewayUrl = _myPosOptions.ApiUrl,
            Method = "POST",
            OrderId = orderId,
            Fields = fields
        };
    }

    public async Task<MyPosCallbackResultResponse> HandleMyPosNotifyAsync(IReadOnlyList<KeyValuePair<string, string>> formFields)
    {
        var signature = GetRequiredField(formFields, "Signature");

        var isValid = _signatureService.Verify(formFields, signature);
        if (!isValid)
            throw new UnauthorizedAccessException("Невалиден myPOS подпис.");

        var orderId = GetRequiredField(formFields, "OrderID");
        var amountText = GetRequiredField(formFields, "Amount");
        var currency = GetRequiredField(formFields, "Currency");
        var providerPaymentId = GetOptionalField(formFields, "IPC_Trnref");

        var pending = await _paymentRepository.GetPendingActionByProviderOrderIdAsync(orderId)
            ?? throw new InvalidOperationException("Pending action по OrderID не е намерен.");

        if (pending.ExpiresAt.HasValue && pending.ExpiresAt.Value < DateTime.UtcNow)
        {
            await CleanupPendingActionAsync(pending, markAsFailed: true);
            throw new InvalidOperationException("Pending action е изтекъл.");
        }

        // duplicate notify protection
        if (string.Equals(pending.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            return new MyPosCallbackResultResponse
            {
                Success = true,
                Message = "Операцията вече е обработена.",
                PendingActionId = pending.Id,
                ListingId = pending.ListingId
            };
        }

        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Pending action вече не е в PENDING статус.");

        if (!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            throw new InvalidOperationException("Невалидна сума от myPOS.");

        if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Невалидна валута от myPOS.");

        if (amount != pending.AmountEUR)
            throw new InvalidOperationException("Платената сума не съвпада с очакваната.");

        var listingId = await ExecutePendingActionOnceAsync(pending, providerPaymentId);

        return new MyPosCallbackResultResponse
        {
            Success = true,
            Message = "Платената операция е изпълнена успешно.",
            PendingActionId = pending.Id,
            ListingId = listingId

        };
    }

    public async Task<MyPosCallbackResultResponse> HandleMyPosCancelAsync(IReadOnlyList<KeyValuePair<string, string>> formFields)
    {
        var signature = GetRequiredField(formFields, "Signature");

        var isValid = _signatureService.Verify(formFields, signature);
        if (!isValid)
            throw new UnauthorizedAccessException("Невалиден myPOS подпис.");

        var orderId = GetRequiredField(formFields, "OrderID");

        var pending = await _paymentRepository.GetPendingActionByProviderOrderIdAsync(orderId)
            ?? throw new InvalidOperationException("Pending action по OrderID не е намерен.");

        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return new MyPosCallbackResultResponse
            {
                Success = true,
                Message = "Операцията вече не е активна.",
                PendingActionId = pending.Id,
                ListingId = pending.ListingId
            };
        }

        await CleanupPendingActionAsync(pending, markAsCancelled: true);

        return new MyPosCallbackResultResponse
        {
            Success = true,
            Message = "Плащането е отказано/прекратено.",
            PendingActionId = pending.Id,
            ListingId = pending.ListingId
        };
    }

    public async Task<int> CleanupExpiredPendingActionsAsync(int take = 100)
    {
        var expired = await _paymentRepository.GetExpiredPendingActionsAsync(take);
        var cleaned = 0;

        foreach (var pending in expired)
        {
            await CleanupPendingActionAsync(pending, markAsFailed: true);
            cleaned++;
        }

        return cleaned;
    }

    private async Task<long?> ExecutePendingActionOnceAsync(PendingListingAction pending, string? providerPaymentId)
    {
        if (!string.Equals(pending.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            return pending.ListingId;

        var listingId = await ExecutePendingActionAsync(pending);

        await _listingRepository.InsertPaymentAsync(
            pending.UserId,
            listingId,
            MapServiceType(pending.ActionType),
            pending.AmountEUR,
            "PAID",
            $"Payment confirmed. PendingActionId={pending.Id}; ProviderPaymentId={providerPaymentId}; ProviderOrderId={pending.ProviderOrderId}");

        await _paymentRepository.MarkPendingActionCompletedAsync(pending.Id, providerPaymentId);

        return listingId;
    }

    private async Task<long?> ExecutePendingActionAsync(PendingListingAction pending)
    {
        return pending.ActionType switch
        {
            "CREATE" => await ExecuteCreateAsync(pending),
            "REFRESH" => await ExecuteRefreshAsync(pending),
            "PROMOTE_TOP" => await ExecutePromoteAsync(pending, "TOP"),
            "PROMOTE_VIP" => await ExecutePromoteAsync(pending, "VIP"),
            _ => throw new InvalidOperationException("Неподдържан pending action type.")
        };
    }

    private async Task<long?> ExecuteCreateAsync(PendingListingAction pending)

    {
        var request = JsonSerializer.Deserialize<CreateListingRequest>(pending.PayloadJson)
            ?? throw new InvalidOperationException("Невалиден payload за CREATE.");

        ValidatePhotos(request.Photos);
        await _blobImageService.EnsureUserOwnsBlobsAsync(pending.UserId, request.Photos.Select(x => x.BlobName));

        var user = await _listingRepository.GetUserByIdAsync(pending.UserId)
            ?? throw new InvalidOperationException("Потребителят не е намерен.");

        var sanitizedPhotos = request.Photos
            .Select(x => new ListingPhotoRequest
            {
                FileName = Path.GetFileName(x.FileName),
                FileUrl = _blobImageService.GetBlobUrl(x.BlobName!),
                BlobName = x.BlobName,
                SortOrder = x.SortOrder,
                IsMain = x.IsMain
            })
            .ToList();

        var now = DateTime.UtcNow;
        var priceEur = Math.Round(request.PriceOriginal * request.ExchangeRateToEUR, 2, MidpointRounding.AwayFromZero);

        var listing = new Listing
        {
            UserId = pending.UserId,
            MainCategoryLookupId = request.MainCategoryLookupId,
            SubCategoryLookupId = request.SubCategoryLookupId,
            SubCategory2LookupId = request.SubCategory2LookupId,
            BrandId = request.BrandId,
            ModelId = request.ModelId,
            ItemModelText = request.ItemModelText?.Trim(),
            LicenseCategoryLookupId = request.LicenseCategoryLookupId,
            ConditionLookupId = request.ConditionLookupId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            VehicleYear = request.VehicleYear,
            HorsePower = request.HorsePower,
            EngineCC = request.EngineCC,
            Mileage = request.Mileage,
            Color = request.Color?.Trim(),
            PriceOriginal = request.PriceOriginal,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            ExchangeRateToEUR = request.ExchangeRateToEUR,
            PriceEUR = priceEur,
            CountryId = request.CountryId,
            RegionId = request.RegionId,
            CityId = request.CityId,
            ContactName = request.ContactName?.Trim(),
            ContactPhone = request.ContactPhone.Trim(),
            PromotionType = NormalizePromotion(request.RequestedPromotionType),
            PromotionStartAt = NormalizePromotion(request.RequestedPromotionType) == "NORMAL" ? null : now,
            PromotionEndAt = NormalizePromotion(request.RequestedPromotionType) == "NORMAL" ? null : now.AddDays(7),
            LastRefreshAt = null,
            PublishedAt = now,
            UpdatedAt = now
        };

        var listingId = await _listingRepository.InsertListingAsync(listing);
        await _listingRepository.ReplaceListingPhotosAsync(listingId, sanitizedPhotos);
        await _listingRepository.IncreaseUserPublishedCountersAsync(pending.UserId, user.AccountType);

        return listingId;
    }

    private async Task<long?> ExecuteRefreshAsync(PendingListingAction pending)
    {
        if (!pending.ListingId.HasValue)
            throw new InvalidOperationException("Липсва listingId за REFRESH.");

        await _listingRepository.RefreshListingAsync(pending.ListingId.Value, DateTime.UtcNow);
        return pending.ListingId.Value;
    }

    private async Task<long?> ExecutePromoteAsync(PendingListingAction pending, string targetPromotion)
    {
        if (!pending.ListingId.HasValue)
            throw new InvalidOperationException("Липсва listingId за promotion.");

        var now = DateTime.UtcNow;
        await _listingRepository.UpdateListingPromotionAsync(pending.ListingId.Value, targetPromotion, now, now.AddDays(7));
        return pending.ListingId.Value;

    }

    private async Task CleanupPendingActionAsync(
        PendingListingAction pending,
        bool markAsFailed = false,
        bool markAsCancelled = false)
    {
        if (string.Equals(pending.ActionType, "CREATE", StringComparison.OrdinalIgnoreCase))
        {
            var request = JsonSerializer.Deserialize<CreateListingRequest>(pending.PayloadJson);
            if (request?.Photos != null && request.Photos.Count > 0)
            {
                var blobNames = request.Photos
                    .Where(x => !string.IsNullOrWhiteSpace(x.BlobName))
                    .Select(x => x.BlobName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (blobNames.Count > 0)
                    await _blobImageService.DeleteManyAsync(blobNames);
            }
        }

        if (markAsCancelled)
        {
            await _paymentRepository.MarkPendingActionCancelledAsync(pending.Id);
            return;
        }

        if (markAsFailed)
            await _paymentRepository.MarkPendingActionFailedAsync(pending.Id);
    }

    private static string BuildItemName(PendingListingAction pending)
    {
        return pending.ActionType switch
        {
            "CREATE" => "Moto listing publish",
            "REFRESH" => "Moto listing refresh",
            "PROMOTE_TOP" => "Moto listing TOP",
            "PROMOTE_VIP" => "Moto listing VIP",
            _ => "Moto site payment"
        };
    }

    private static string MapServiceType(string pendingActionType)
    {
        return pendingActionType switch
        {
            "CREATE" => "LISTING",
            "REFRESH" => "REFRESH",
            "PROMOTE_TOP" => "TOP",
            "PROMOTE_VIP" => "VIP",
            _ => "LISTING"
        };
    }

    private static void ValidatePhotos(List<ListingPhotoRequest> photos)
    {
        if (photos == null || photos.Count == 0)
            throw new InvalidOperationException("Трябва да има поне една снимка.");

        if (!photos.Any(x => x.IsMain))
            throw new InvalidOperationException("Трябва да има главна снимка.");

        if (photos.Count(x => x.IsMain) > 1)
            throw new InvalidOperationException("Главната снимка може да е само една.");

        if (photos.Any(x => string.IsNullOrWhiteSpace(x.BlobName)))
            throw new InvalidOperationException("Всяка снимка трябва да има BlobName.");
    }

    private static string NormalizePromotion(string? promotionType)
    {
        var value = (promotionType ?? "NORMAL").Trim().ToUpperInvariant();
        return value switch
        {
            "NORMAL" => "NORMAL",
            "TOP" => "TOP",
            "VIP" => "VIP",
            _ => throw new InvalidOperationException("Невалиден promotion type.")
        };
    }

    private static string GetRequiredField(IReadOnlyList<KeyValuePair<string, string>> fields, string key)
    {
        var value = fields.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)).Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Липсва поле: {key}");

        return value;
    }

    private static string? GetOptionalField(IReadOnlyList<KeyValuePair<string, string>> fields, string key)
    {
        return fields.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }
}