using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;
using System.Text.Json;

namespace MotoMarket.Api.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;
    private readonly IBlobImageService _blobImageService;
    private readonly IListingValidationService _listingValidationService;
    private readonly PaidActionsOptions _paidOptions;

    public ListingService(
        IListingRepository listingRepository,
        IBlobImageService blobImageService,
        IListingValidationService listingValidationService,
        IOptions<PaidActionsOptions> paidOptions)
    {
        _listingRepository = listingRepository;
        _blobImageService = blobImageService;
        _listingValidationService = listingValidationService;
        _paidOptions = paidOptions.Value;
    }

    public async Task<ListingOperationResponse> CreateAsync(long userId, CreateListingRequest request)
    {
        ValidatePhotos(request.Photos);
        request.RequestedPromotionType = NormalizePromotion(request.RequestedPromotionType, allowNormal: true);
        await _listingValidationService.ValidateCreateAsync(request);

        var user = await GetActiveUser(userId);
        var sanitizedPhotos = await NormalizePhotosAsync(userId, request.Photos);

        var priceEur = CalculatePriceEur(request.PriceOriginal, request.ExchangeRateToEUR);
        var listingFee = GetListingCreationFee(user);
        var promoFee = GetPromotionFee(request.RequestedPromotionType);
        var totalFee = listingFee + promoFee;

        if (totalFee > 0 && _paidOptions.RequireSuccessfulPaymentForPaidActions)
        {
            var pendingId = await _listingRepository.InsertPendingActionAsync(new PendingListingAction
            {
                UserId = userId,
                ListingId = null,
                ActionType = "CREATE",
                PayloadJson = JsonSerializer.Serialize(request),
                AmountEUR = totalFee,
                CurrencyCode = "EUR",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            });

            return new ListingOperationResponse
            {
                Success = true,
                RequiresPayment = true,
                PendingActionId = pendingId,
                AmountEUR = totalFee,
                Message = "Създадена е чакаща операция. Обявата ще се публикува само след успешно плащане."
            };
        }

        var now = DateTime.UtcNow;

        var listing = new Listing
        {
            UserId = userId,
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
            PromotionType = request.RequestedPromotionType,
            PromotionStartAt = request.RequestedPromotionType == "NORMAL" ? null : now,
            PromotionEndAt = request.RequestedPromotionType == "NORMAL" ? null : now.AddDays(7),
            LastRefreshAt = null,
            PublishedAt = now,
            UpdatedAt = now
        };

        var listingId = await _listingRepository.InsertListingAsync(listing);
        await _listingRepository.ReplaceListingPhotosAsync(listingId, sanitizedPhotos);
        await _listingRepository.IncreaseUserPublishedCountersAsync(userId, user.AccountType);

        if (listingFee > 0)
        {
            await _listingRepository.InsertPaymentAsync(
                userId,
                listingId,
                "LISTING",
                listingFee,
                "PAID",
                "Bypassed payment because RequireSuccessfulPaymentForPaidActions=false");
        }

        if (promoFee > 0)
        {
            await _listingRepository.InsertPaymentAsync(
                userId,
                listingId,
                request.RequestedPromotionType == "VIP" ? "VIP" : "TOP",
                promoFee,
                "PAID",
                "Bypassed payment because RequireSuccessfulPaymentForPaidActions=false");
        }

        return new ListingOperationResponse
        {
            Success = true,
            ListingId = listingId,
            AmountEUR = totalFee,
            Message = "Обявата е публикувана успешно."
        };
    }

    public async Task<ListingOperationResponse> UpdateAsync(long userId, long listingId, UpdateListingRequest request)
    {
        ValidatePhotos(request.Photos);
        await _listingValidationService.ValidateUpdateAsync(request);

        await GetOwnedListing(userId, listingId);
        var sanitizedPhotos = await NormalizePhotosAsync(userId, request.Photos);

        var oldBlobNames = await _listingRepository.GetListingBlobNamesAsync(listingId);
        var newBlobNames = sanitizedPhotos
            .Where(x => !string.IsNullOrWhiteSpace(x.BlobName))
            .Select(x => x.BlobName!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var blobsToDelete = oldBlobNames
            .Where(x => !newBlobNames.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var priceEur = CalculatePriceEur(request.PriceOriginal, request.ExchangeRateToEUR);

        await _listingRepository.UpdateListingAsync(listingId, request, priceEur);
        await _listingRepository.ReplaceListingPhotosAsync(listingId, sanitizedPhotos);

        if (blobsToDelete.Count > 0)
        {
            await _blobImageService.DeleteManyAsync(blobsToDelete);
        }

        return new ListingOperationResponse
        {
            Success = true,
            ListingId = listingId,
            BlobNamesToDelete = blobsToDelete,
            Message = "Обявата е редактирана успешно."
        };
    }

    public async Task<ListingOperationResponse> DeleteAsync(long userId, long listingId)
    {
        await GetOwnedListing(userId, listingId);

        var blobNames = await _listingRepository.GetListingBlobNamesAsync(listingId);
        await _listingRepository.DeleteListingAsync(listingId);

        if (blobNames.Count > 0)
        {
            await _blobImageService.DeleteManyAsync(blobNames);
        }

        return new ListingOperationResponse
        {
            Success = true,
            ListingId = listingId,
            BlobNamesToDelete = blobNames,
            Message = "Обявата е изтрита успешно."
        };
    }

    public async Task<ListingOperationResponse> RefreshAsync(long userId, long listingId)
    {
        var user = await GetActiveUser(userId);
        await GetOwnedListing(userId, listingId);

        var fee = user.AccountType == "PRIVATE"
            ? _paidOptions.PrivateRefreshPriceEUR
            : _paidOptions.CompanyRefreshPriceEUR;

        if (fee > 0 && _paidOptions.RequireSuccessfulPaymentForPaidActions)
        {
            var pendingId = await _listingRepository.InsertPendingActionAsync(new PendingListingAction
            {
                UserId = userId,
                ListingId = listingId,
                ActionType = "REFRESH",
                PayloadJson = "{}",
                AmountEUR = fee,
                CurrencyCode = "EUR",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            });

            return new ListingOperationResponse
            {
                Success = true,
                RequiresPayment = true,
                PendingActionId = pendingId,
                AmountEUR = fee,
                Message = "Refresh ще се изпълни само след успешно плащане."
            };
        }

        await _listingRepository.RefreshListingAsync(listingId, DateTime.UtcNow);
        await _listingRepository.InsertPaymentAsync(
            userId,
            listingId,
            "REFRESH",
            fee,
            "PAID",
            "Bypassed payment because RequireSuccessfulPaymentForPaidActions=false");

        return new ListingOperationResponse
        {
            Success = true,
            ListingId = listingId,
            AmountEUR = fee,
            Message = "Обявата е refresh-ната успешно."
        };
    }

    public async Task<ListingOperationResponse> PromoteAsync(long userId, long listingId, PromoteListingRequest request)
    {
        var user = await GetActiveUser(userId);
        var listing = await GetOwnedListing(userId, listingId);

        var target = NormalizePromotion(request.TargetPromotionType, allowNormal: false);

        if (listing.PromotionType == "VIP" && listing.PromotionEndAt.HasValue && listing.PromotionEndAt.Value > DateTime.UtcNow)
            throw new InvalidOperationException("Обявата вече е VIP.");

        if (listing.PromotionType == "TOP" && target == "TOP" && listing.PromotionEndAt.HasValue && listing.PromotionEndAt.Value > DateTime.UtcNow)
            throw new InvalidOperationException("Обявата вече е TOP.");

        var fee = GetPromotionFee(target);
        var actionType = target == "VIP" ? "PROMOTE_VIP" : "PROMOTE_TOP";

        if (fee > 0 && _paidOptions.RequireSuccessfulPaymentForPaidActions)
        {
            var pendingId = await _listingRepository.InsertPendingActionAsync(new PendingListingAction
            {
                UserId = userId,
                ListingId = listingId,
                ActionType = actionType,
                PayloadJson = JsonSerializer.Serialize(request),
                AmountEUR = fee,
                CurrencyCode = "EUR",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            });

            return new ListingOperationResponse
            {
                Success = true,
                RequiresPayment = true,
                PendingActionId = pendingId,
                AmountEUR = fee,
                Message = "Промоцията ще се изпълни само след успешно плащане."
            };
        }

        var now = DateTime.UtcNow;
        await _listingRepository.UpdateListingPromotionAsync(listingId, target, now, now.AddDays(7));
        await _listingRepository.InsertPaymentAsync(
            userId,
            listingId,
            target == "VIP" ? "VIP" : "TOP",
            fee,
            "PAID",
            "Bypassed payment because RequireSuccessfulPaymentForPaidActions=false");

        return new ListingOperationResponse
        {
            Success = true,
            ListingId = listingId,
            AmountEUR = fee,
            Message = $"Обявата е направена {target} успешно."
        };
    }

    public async Task<ListingDetailsResponse> GetByIdAsync(long listingId)
    {
        var listing = await _listingRepository.GetListingByIdAsync(listingId)
            ?? throw new InvalidOperationException("Обявата не е намерена.");

        var photos = await _listingRepository.GetListingPhotosAsync(listingId);

        return new ListingDetailsResponse
        {
            Id = listing.Id,
            UserId = listing.UserId,
            Title = listing.Title,
            Description = listing.Description,
            MainCategoryLookupId = listing.MainCategoryLookupId,
            SubCategoryLookupId = listing.SubCategoryLookupId,
            SubCategory2LookupId = listing.SubCategory2LookupId,
            BrandId = listing.BrandId,
            ModelId = listing.ModelId,
            ItemModelText = listing.ItemModelText,
            LicenseCategoryLookupId = listing.LicenseCategoryLookupId,
            ConditionLookupId = listing.ConditionLookupId,
            VehicleYear = listing.VehicleYear,
            HorsePower = listing.HorsePower,
            EngineCC = listing.EngineCC,
            Mileage = listing.Mileage,
            Color = listing.Color,
            PriceOriginal = listing.PriceOriginal,
            CurrencyCode = listing.CurrencyCode,
            ExchangeRateToEUR = listing.ExchangeRateToEUR,
            PriceEUR = listing.PriceEUR,
            CountryId = listing.CountryId,
            RegionId = listing.RegionId,
            CityId = listing.CityId,
            ContactName = listing.ContactName,
            ContactPhone = listing.ContactPhone,
            ViewCount = listing.ViewCount,
            PromotionType = GetCurrentPromotionType(listing),
            PromotionStartAt = listing.PromotionStartAt,
            PromotionEndAt = listing.PromotionEndAt,
            LastRefreshAt = listing.LastRefreshAt,
            PublishedAt = listing.PublishedAt,
            Photos = photos.Select(x => new ListingPhotoResponse
            {
                Id = x.Id,
                FileName = x.FileName,
                FileUrl = !string.IsNullOrWhiteSpace(x.BlobName)
                    ? _blobImageService.GetReadUrl(x.BlobName!)
                    : x.FileUrl,
                BlobName = x.BlobName,
                SortOrder = x.SortOrder,
                IsMain = x.IsMain
            }).ToList()
        };
    }

    public async Task<PublicListingSearchResponse> SearchPublicAsync(PublicListingSearchRequest request)
    {
        ValidatePublicSearchRequest(request);

        var totalCount = await _listingRepository.CountPublicSearchAsync(request);
        var items = await _listingRepository.SearchPublicAsync(request);

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.MainPhotoBlobName))
            {
                item.MainPhotoUrl = _blobImageService.GetReadUrl(item.MainPhotoBlobName);
            }
        }

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PublicListingSearchResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        };
    }

    public async Task<PublicListingDetailsResponse> GetPublicByIdAsync(long listingId, bool incrementViewCount = true)
    {
        var details = await _listingRepository.GetPublicDetailsAsync(listingId)
            ?? throw new InvalidOperationException("Обявата не е намерена.");

        if (incrementViewCount)
        {
            await _listingRepository.IncrementViewCountAsync(listingId);
            details = await _listingRepository.GetPublicDetailsAsync(listingId)
                ?? throw new InvalidOperationException("Обявата не е намерена.");
        }

        var photos = await _listingRepository.GetListingPhotosAsync(listingId);

        details.Photos = photos.Select(x => new PublicListingPhotoResponse
        {
            Id = x.Id,
            FileName = x.FileName,
            FileUrl = !string.IsNullOrWhiteSpace(x.BlobName)
                ? _blobImageService.GetReadUrl(x.BlobName!)
                : x.FileUrl,
            BlobName = x.BlobName,
            SortOrder = x.SortOrder,
            IsMain = x.IsMain
        }).ToList();

        return details;
    }

    private async Task<User> GetActiveUser(long userId)
    {
        return await _listingRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");
    }

    private async Task<Listing> GetOwnedListing(long userId, long listingId)
    {
        var listing = await _listingRepository.GetListingByIdAsync(listingId)
            ?? throw new InvalidOperationException("Обявата не е намерена.");

        if (listing.UserId != userId)
            throw new UnauthorizedAccessException("Нямаш право върху тази обява.");

        return listing;
    }

    private async Task<List<ListingPhotoRequest>> NormalizePhotosAsync(long userId, List<ListingPhotoRequest> photos)
    {
        await _blobImageService.EnsureUserOwnsBlobsAsync(userId, photos.Select(x => x.BlobName));

        var duplicates = photos
            .Where(x => !string.IsNullOrWhiteSpace(x.BlobName))
            .GroupBy(x => x.BlobName!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException("Има дублирани снимки в заявката.");

        return photos
            .Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x.BlobName))
                    throw new InvalidOperationException("Всяка снимка трябва да има BlobName.");

                return new ListingPhotoRequest
                {
                    FileName = Path.GetFileName(x.FileName?.Trim() ?? string.Empty),
                    FileUrl = _blobImageService.GetBlobUrl(x.BlobName),
                    BlobName = x.BlobName,
                    SortOrder = x.SortOrder,
                    IsMain = x.IsMain
                };
            })
            .ToList();
    }

    private decimal GetListingCreationFee(User user)
    {
        if (user.AccountType == "PRIVATE")
        {
            return user.PrivateFreeUsedCount < 3 ? 0 : _paidOptions.PrivatePaidListingPriceEUR;
        }

        if (user.CompanyStarterFreeUsedCount < 30)
            return 0;

        var now = DateTime.UtcNow;
        var isCurrentMonth = user.CompanyMonthlyQuotaYear == now.Year && user.CompanyMonthlyQuotaMonth == now.Month;
        var usedThisMonth = isCurrentMonth ? user.CompanyMonthlyFreeUsedCount : 0;

        return usedThisMonth < 10 ? 0 : _paidOptions.CompanyPaidListingPriceEUR;
    }

    private decimal GetPromotionFee(string promotionType)
    {
        return promotionType switch
        {
            "TOP" => _paidOptions.TopPriceEUR,
            "VIP" => _paidOptions.VipPriceEUR,
            _ => 0
        };
    }

    private static decimal CalculatePriceEur(decimal original, decimal rate)
    {
        return Math.Round(original * rate, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidatePhotos(List<ListingPhotoRequest> photos)
    {
        if (photos == null || photos.Count == 0)
            throw new InvalidOperationException("Трябва да има поне една снимка.");

        if (!photos.Any(x => x.IsMain))
            throw new InvalidOperationException("Трябва да има главна снимка.");

        if (photos.Count(x => x.IsMain) > 1)
            throw new InvalidOperationException("Главната снимка може да е само една.");
    }

    private static void ValidatePublicSearchRequest(PublicListingSearchRequest request)
    {
        if (request.Page <= 0)
            throw new InvalidOperationException("Невалидна страница.");

        if (request.PageSize <= 0 || request.PageSize > 100)
            throw new InvalidOperationException("Невалиден pageSize.");

        if (request.PriceFrom.HasValue && request.PriceTo.HasValue && request.PriceFrom > request.PriceTo)
            throw new InvalidOperationException("PriceFrom не може да е по-голямо от PriceTo.");

        if (request.YearFrom.HasValue && request.YearTo.HasValue && request.YearFrom > request.YearTo)
            throw new InvalidOperationException("YearFrom не може да е по-голямо от YearTo.");

        if (request.HorsePowerFrom.HasValue && request.HorsePowerTo.HasValue && request.HorsePowerFrom > request.HorsePowerTo)
            throw new InvalidOperationException("HorsePowerFrom не може да е по-голямо от HorsePowerTo.");

        if (request.EngineCcFrom.HasValue && request.EngineCcTo.HasValue && request.EngineCcFrom > request.EngineCcTo)
            throw new InvalidOperationException("EngineCcFrom не може да е по-голямо от EngineCcTo.");

        if (request.MileageFrom.HasValue && request.MileageTo.HasValue && request.MileageFrom > request.MileageTo)
            throw new InvalidOperationException("MileageFrom не може да е по-голямо от MileageTo.");

        var allowedSorts = new[] { "newest", "priceasc", "pricedesc", "yeardesc", "yearasc", "oldest" };
        if (!allowedSorts.Contains((request.SortBy ?? "newest").Trim().ToLowerInvariant()))
            throw new InvalidOperationException("Невалиден sortBy.");
    }

    private static string NormalizePromotion(string value, bool allowNormal)
    {
        var normalized = (value ?? "NORMAL").Trim().ToUpperInvariant();

        var allowed = allowNormal
            ? new[] { "NORMAL", "TOP", "VIP" }
            : new[] { "TOP", "VIP" };

        if (!allowed.Contains(normalized))
            throw new InvalidOperationException("Невалиден promotion type.");

        return normalized;
    }

    private static string GetCurrentPromotionType(Listing listing)
    {
        if (listing.PromotionType == "VIP" && listing.PromotionEndAt.HasValue && listing.PromotionEndAt.Value > DateTime.UtcNow)
            return "VIP";

        if (listing.PromotionType == "TOP" && listing.PromotionEndAt.HasValue && listing.PromotionEndAt.Value > DateTime.UtcNow)
            return "TOP";

        return "NORMAL";
    }
}