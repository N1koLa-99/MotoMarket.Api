using MotoMarket.Api.Models.Entities;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(long userId);

    Task<bool> EmailExistsAsync(string email, long? excludeUserId = null);
    Task<bool> PhoneExistsAsync(string phone, long? excludeUserId = null);
    Task<bool> CompanyVatExistsAsync(string vatNumber);

    Task<long> CreatePrivateUserAsync(User user);
    Task<long> CreateCompanyUserAsync(User user);

    Task UpdateUserPasswordHashAsync(long userId, string newPasswordHash);
    Task UpdateUserEmailAsync(long userId, string newEmail);

    Task UpdatePrivateProfileAsync(
        long userId,
        string firstName,
        string lastName,
        string phone,
        int countryId,
        int? regionId,
        int? cityId);

    Task SoftDeleteUserAndDeleteRelatedDataAsync(long userId);

    Task<long> InsertRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
    Task RevokeRefreshTokenAsync(long refreshTokenId, string? replacedByTokenHash, string? revokedByIp);
    Task RevokeAllActiveRefreshTokensForUserAsync(long userId, string? revokedByIp);
}