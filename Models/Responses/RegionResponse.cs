namespace MotoMarket.Api.Models.Responses;

public class RegionResponse
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string? RegionCode { get; set; }
    public string NameBg { get; set; } = default!;
    public int SortOrder { get; set; }
}