namespace MotoMarket.Api.Models.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public UserProfileResponse User { get; set; } = default!;
}