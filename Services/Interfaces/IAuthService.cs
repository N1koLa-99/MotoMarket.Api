using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IAuthService
{
    Task<RegisterStartResponse> RegisterPrivateAsync(RegisterPrivateRequest request, string? ipAddress, string? userAgent);
    Task<RegisterStartResponse> RegisterCompanyAsync(RegisterCompanyRequest request, string? ipAddress, string? userAgent);

    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent);
    Task LogoutAsync(string refreshToken, string? ipAddress);

    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task ResendEmailVerificationAsync(ResendEmailVerificationRequest request, string? ipAddress);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress);
    Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress);

    Task<UserProfileResponse> GetMeAsync(long userId);
    Task<UserProfileResponse> UpdatePrivateProfileAsync(long userId, UpdatePrivateProfileRequest request);
    Task DeleteMyProfileAsync(long userId, DeleteProfileRequest request);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, string? ipAddress);
    Task ChangeEmailAsync(long userId, ChangeEmailRequest request, string? ipAddress);

    string? ConsumeLatestRefreshToken();
}