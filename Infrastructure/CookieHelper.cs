namespace MotoMarket.Api.Infrastructure;

public static class CookieHelper
{
    public const string RefreshTokenCookieName = "refreshToken";

    public static void SetRefreshTokenCookie(HttpResponse response, string refreshToken, int refreshTokenDays)
    {
        response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenDays),
            IsEssential = true
        });
    }

    public static void DeleteRefreshTokenCookie(HttpResponse response)
    {
        response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
}