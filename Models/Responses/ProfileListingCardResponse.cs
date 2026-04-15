namespace MotoMarket.Api.Models.Responses;
public class ProfileListingCardResponse

{
    public long Id { get; set; }
    public string Title { get; set; } = default!;
    public decimal PriceOriginal { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal PriceEUR { get; set; }
    public short? VehicleYear { get; set; }
    public int? HorsePower { get; set; }
    public int? EngineCC { get; set; }
    public int? Mileage { get; set; }
    public int ViewCount { get; set; }
    public string CurrentPromotionType { get; set; } = "NORMAL";
    public DateTime? PromotionEndAt { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime? LastRefreshAt { get; set; }
    public string? MainCategoryName { get; set; }
    public string? SubCategoryName { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public string? LicenseCategoryName { get; set; }
    public string? CountryName { get; set; }
    public string? RegionName { get; set; }
    public string? CityName { get; set; }
    public string? MainPhotoUrl { get; set; }
    public string? MainPhotoBlobName { get; set; }

}