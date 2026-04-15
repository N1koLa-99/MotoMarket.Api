namespace MotoMarket.Api.Models.Responses;

public class CityResponse
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public string NameBg { get; set; } = default!;
    public bool IsMajor { get; set; }
    public int SortOrder { get; set; }
}