using MotoMarket.Api.Models.Entities;

namespace MotoMarket.Api.Services.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, out DateTime expiresAtUtc);
    string CreateRefreshToken();
    string HashToken(string token);
}