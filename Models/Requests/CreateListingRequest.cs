using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class CreateListingRequest
{
    [Required]
    public int MainCategoryLookupId { get; set; }

    public int? SubCategoryLookupId { get; set; }
    public int? SubCategory2LookupId { get; set; }

    public int? BrandId { get; set; }
    public int? ModelId { get; set; }

    [MaxLength(100)]
    public string? ItemModelText { get; set; }

    public int? LicenseCategoryLookupId { get; set; }
    public int? ConditionLookupId { get; set; }

    [Required, MaxLength(200), MinLength(3)]
    public string Title { get; set; } = default!;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public short? VehicleYear { get; set; }
    public int? HorsePower { get; set; }
    public int? EngineCC { get; set; }
    public int? Mileage { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [Range(0, 999999999)]
    public decimal PriceOriginal { get; set; }

    [Required, MaxLength(3), MinLength(3)]
    public string CurrencyCode { get; set; } = "EUR";

    [Range(typeof(decimal), "0.00000001", "999999999")]
    public decimal ExchangeRateToEUR { get; set; } = 1;

    [Required]
    public int CountryId { get; set; }

    public int? RegionId { get; set; }
    public int? CityId { get; set; }

    [MaxLength(120)]
    public string? ContactName { get; set; }

    [Required, MaxLength(30), MinLength(5)]
    public string ContactPhone { get; set; } = default!;

    [Required]
    public List<ListingPhotoRequest> Photos { get; set; } = new();

    [MaxLength(10)]
    public string RequestedPromotionType { get; set; } = "NORMAL";
}