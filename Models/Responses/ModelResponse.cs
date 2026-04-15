namespace MotoMarket.Api.Models.Responses;

public class ModelResponse
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public int VehicleClassLookupId { get; set; }
    public string Name { get; set; } = default!;
}