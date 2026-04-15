using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;
using System.Reflection;

namespace MotoMarket.Api.Services;

public class ListingPresentationService : IListingPresentationService
{
    private readonly IListingRepository _listingRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IBlobImageService _blobImageService;
    private readonly ICurrencyConversionService _currencyConversionService;

    public ListingPresentationService(
        IListingRepository listingRepository,
        ILookupRepository lookupRepository,
        IBlobImageService blobImageService,
        ICurrencyConversionService currencyConversionService)
    {
        _listingRepository = listingRepository;
        _lookupRepository = lookupRepository;
        _blobImageService = blobImageService;
        _currencyConversionService = currencyConversionService;
    }

    public async Task<PublicListingSearchResponse> SearchPublicAsync(PublicListingSearchRequest request, long? viewerUserId = null)
    {
        ValidatePublicSearchRequest(request);

        var totalCount = await _listingRepository.CountPublicSearchAsync(request);
        var items = await _listingRepository.SearchPublicAsync(request);

        var displayCurrencyCode = await ResolveViewerCurrencyCodeAsync(viewerUserId);

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.MainPhotoBlobName))
            {
                item.MainPhotoUrl = _blobImageService.GetReadUrl(item.MainPhotoBlobName);
            }

            item.DisplayCurrencyCode = displayCurrencyCode;
            item.DisplayPrice = _currencyConversionService.ConvertFromEUR(item.PriceEUR, displayCurrencyCode);
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
            DisplayCurrencyCode = displayCurrencyCode,
            Items = items
        };
    }

    public async Task<PublicListingDetailsResponse> GetPublicByIdAsync(long listingId, long? viewerUserId = null, bool incrementViewCount = true)
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

        var displayCurrencyCode = await ResolveViewerCurrencyCodeAsync(viewerUserId);
        details.DisplayCurrencyCode = displayCurrencyCode;
        details.DisplayPrice = _currencyConversionService.ConvertFromEUR(details.PriceEUR, displayCurrencyCode);

        var seller = await _listingRepository.GetUserByIdAsync(details.UserId);
        if (seller != null)
        {
            details.Seller = new PublicSellerResponse
            {
                UserId = seller.Id,
                AccountType = seller.AccountType,
                SellerTypeLabel = string.Equals(seller.AccountType, "COMPANY", StringComparison.OrdinalIgnoreCase)
                    ? "Фирма"
                    : "Частно лице",
                DisplayName = BuildSellerDisplayName(seller),
                Phone = seller.Phone,
                LogoUrl = TryGetSellerLogoUrl(seller)
            };
        }

        return details;
    }

    private async Task<string> ResolveViewerCurrencyCodeAsync(long? viewerUserId)
    {
        if (!viewerUserId.HasValue)
            return "EUR";

        var viewer = await _listingRepository.GetUserByIdAsync(viewerUserId.Value);
        if (viewer == null)
            return "EUR";

        var country = await _lookupRepository.GetCountryByIdAsync(viewer.CountryId);
        if (country == null || string.IsNullOrWhiteSpace(country.DefaultCurrencyCode))
            return "EUR";

        return country.DefaultCurrencyCode.Trim().ToUpperInvariant();
    }

    private static string BuildSellerDisplayName(User seller)
    {
        if (string.Equals(seller.AccountType, "COMPANY", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(seller.CompanyName))
                return seller.CompanyName.Trim();

            if (!string.IsNullOrWhiteSpace(seller.ContactPerson))
                return seller.ContactPerson.Trim();

            return "Фирма";
        }

        var fullName = $"{seller.FirstName} {seller.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Частно лице" : fullName;
    }

    private static string? TryGetSellerLogoUrl(User seller)
    {
        var type = seller.GetType();

        var logoUrl = ReadStringProperty(type, seller, "LogoUrl");
        if (!string.IsNullOrWhiteSpace(logoUrl))
            return logoUrl;

        var companyLogoUrl = ReadStringProperty(type, seller, "CompanyLogoUrl");
        if (!string.IsNullOrWhiteSpace(companyLogoUrl))
            return companyLogoUrl;

        return null;
    }

    private static string? ReadStringProperty(Type type, object instance, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null || property.PropertyType != typeof(string))
            return null;

        return property.GetValue(instance) as string;
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
}