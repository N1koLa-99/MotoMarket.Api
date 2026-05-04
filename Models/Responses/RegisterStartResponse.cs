namespace MotoMarket.Api.Models.Responses;

public class RegisterStartResponse
{
    public bool Success { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}