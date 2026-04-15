using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class PublicListingSearchRequest
{
    [MaxLength(200)]
    public string? SearchTerm { get; set; }

    public int? MainCategoryLookupId { get; set; }
    public int? SubCategoryLookupId { get; set; }
    public int? SubCategory2LookupId { get; set; }

    public int? BrandId { get; set; }
    public int? ModelId { get; set; }

    public int? LicenseCategoryLookupId { get; set; }
    public int? ConditionLookupId { get; set; }

    public int? CountryId { get; set; }
    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }

    public short? YearFrom { get; set; }
    public short? YearTo { get; set; }

    public int? HorsePowerFrom { get; set; }
    public int? HorsePowerTo { get; set; }

    public int? EngineCcFrom { get; set; }
    public int? EngineCcTo { get; set; }

    public int? MileageFrom { get; set; }
    public int? MileageTo { get; set; }

    [MaxLength(20)]
    public string SortBy { get; set; } = "newest"; // newest / priceAsc / priceDesc / yearDesc / yearAsc / oldest

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}