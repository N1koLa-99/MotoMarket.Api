namespace MotoMarket.Api.Models.Responses;

public class ListingDetailsResponse
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

    public int CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public string? ContactName { get; set; }
    public string ContactPhone { get; set; } = default!;

    public int ViewCount { get; set; }
    public string PromotionType { get; set; } = default!;
    public DateTime? PromotionStartAt { get; set; }
    public DateTime? PromotionEndAt { get; set; }
    public DateTime? LastRefreshAt { get; set; }
    public DateTime PublishedAt { get; set; }

    public List<ListingPhotoResponse> Photos { get; set; } = new();
}

public class ListingPhotoResponse
{
    public long Id { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? BlobName { get; set; }
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}