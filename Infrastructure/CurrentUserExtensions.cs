using System.Security.Claims;

namespace MotoMarket.Api.Infrastructure;

public static class CurrentUserExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Невалиден token.");

        return userId;
    }

    public static string? GetAccountType(this ClaimsPrincipal user)
        => user.FindFirstValue("account_type");
}