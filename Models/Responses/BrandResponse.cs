namespace MotoMarket.Api.Models.Responses;

public class BrandResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? BrandType { get; set; }
}