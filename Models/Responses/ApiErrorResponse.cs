namespace MotoMarket.Api.Models.Responses;

public class ApiErrorResponse
{
    public string Error { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string TraceId { get; set; } = default!;
    public Dictionary<string, string[]>? FieldErrors { get; set; }
}