namespace MotoMarket.Api.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}