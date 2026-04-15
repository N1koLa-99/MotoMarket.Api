namespace MotoMarket.Api.Models.Responses;

public class CountryResponse
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = default!;
    public string NameBg { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string DefaultCurrencyCode { get; set; } = default!;
    public bool IsPrimaryMarket { get; set; }
}