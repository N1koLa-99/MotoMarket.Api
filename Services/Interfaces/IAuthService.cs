using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterPrivateAsync(RegisterPrivateRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent);
    Task LogoutAsync(string refreshToken, string? ipAddress);

    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, string? ipAddress);
    Task ChangeEmailAsync(long userId, ChangeEmailRequest request, string? ipAddress);

    Task<UserProfileResponse> UpdatePrivateProfileAsync(long userId, UpdatePrivateProfileRequest request);
    Task DeleteMyProfileAsync(long userId, DeleteProfileRequest request);
    Task<UserProfileResponse> GetMeAsync(long userId);

    string? ConsumeLatestRefreshToken();
}