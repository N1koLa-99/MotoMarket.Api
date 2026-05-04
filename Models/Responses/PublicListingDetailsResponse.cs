namespace MotoMarket.Api.Models.Responses;

public class PublicListingDetailsResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public int MainCategoryLookupId { get; set; }
    public int? SubCategoryLookupId { get; set; }
    public int? SubCategory2LookupId { get; set; }

    public int? BrandId { get; set; }
    public int? ModelId { get; set; }
    public string? ItemModelText { get; set; }

    public int? LicenseCategoryLookupId { get; set; }
    public int? ConditionLookupId { get; set; }

    public short? VehicleYear { get; set; }
    public int? HorsePower { get; set; }
    public int? EngineCC { get; set; }
    public int? Mileage { get; set; }
    public string? Color { get; set; }

    public decimal PriceOriginal { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal ExchangeRateToEUR { get; set; }
    public decimal PriceEUR { get; set; }

    public string DisplayCurrencyCode { get; set; } = "EUR";
    public decimal DisplayPrice { get; set; }

    public int CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public string? ContactName { get; set; }
    public string ContactPhone { get; set; } = default!;

    public int ViewCount { get; set; }
    public string CurrentPromotionType { get; set; } = "NORMAL";
    public DateTime? PromotionStartAt { get; set; }
    public DateTime? PromotionEndAt { get; set; }
    public DateTime? LastRefreshAt { get; set; }
    public DateTime PublishedAt { get; set; }

    public string? MainCategoryName { get; set; }
    public string? SubCategoryName { get; set; }
    public string? SubCategory2Name { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public string? LicenseCategoryName { get; set; }
    public string? ConditionName { get; set; }
    public string? CountryName { get; set; }
    public string? RegionName { get; set; }
    public string? CityName { get; set; }

    public PublicSellerResponse? Seller { get; set; }

    public List<PublicListingPhotoResponse> Photos { get; set; } = new();

    public string? LastPriceChangeType { get; set; }   // UP / DOWN
    public decimal? PreviousPriceOriginal { get; set; }
    public decimal? PreviousPriceEUR { get; set; }
    public DateTime? LastPriceChangeAt { get; set; }

    public decimal? PreviousDisplayPrice { get; set; }

    public bool HasPriceChange =>
        string.Equals(LastPriceChangeType, "UP", System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(LastPriceChangeType, "DOWN", System.StringComparison.OrdinalIgnoreCase);
}

public class PublicSellerResponse
{
    public long UserId { get; set; }
    public string AccountType { get; set; } = default!;
    public string SellerTypeLabel { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? LogoUrl { get; set; }
}

public class PublicListingPhotoResponse
{
    public long Id { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? BlobName { get; set; }
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}