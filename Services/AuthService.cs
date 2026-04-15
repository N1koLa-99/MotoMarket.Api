using System.Linq;
using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IAuthRepository authRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _authRepository = authRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterPrivateAsync(RegisterPrivateRequest request, string? ipAddress, string? userAgent)
    {
        if (!request.AcceptedPrivacyPolicy)
            throw new InvalidOperationException("Трябва да приемеш Политиката за поверителност, за да създадеш профил.");

        var email = NormalizeEmail(request.Email);
        var phone = NormalizePhone(request.Phone);

        if (await _authRepository.EmailExistsAsync(email))
            throw new InvalidOperationException("Вече има акаунт с този имейл.");

        if (await _authRepository.PhoneExistsAsync(phone))
            throw new InvalidOperationException("Вече има акаунт с този телефон.");

        var user = new User
        {
            RoleName = "USER",
            AccountType = "PRIVATE",
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = phone,
            Email = email,
            CountryId = request.CountryId,
            RegionId = request.RegionId,
            CityId = request.CityId,
            AcceptedPrivacyPolicy = true,
            PrivacyPolicyAcceptedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var userId = await _authRepository.CreatePrivateUserAsync(user);
        var createdUser = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new InvalidOperationException("Грешка при създаване на потребителя.");

        return await BuildAuthResponseAsync(createdUser, ipAddress, userAgent);
    }

    public async Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request, string? ipAddress, string? userAgent)
    {
        if (!request.AcceptedPrivacyPolicy)
            throw new InvalidOperationException("Трябва да приемеш Политиката за поверителност, за да създадеш профил.");

        var email = NormalizeEmail(request.Email);
        var vat = request.CompanyVatNumber.Trim();
        var phone = NormalizePhone(request.Phone);

        if (await _authRepository.EmailExistsAsync(email))
            throw new InvalidOperationException("Вече има акаунт с този имейл.");

        if (await _authRepository.PhoneExistsAsync(phone))
            throw new InvalidOperationException("Вече има акаунт с този телефон.");

        if (await _authRepository.CompanyVatExistsAsync(vat))
            throw new InvalidOperationException("Вече има фирма с този Булстат.");

        var user = new User
        {
            RoleName = "USER",
            AccountType = "COMPANY",
            CompanyName = request.CompanyName.Trim(),
            CompanyVatNumber = vat,
            ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim(),
            Phone = phone,
            Email = email,
            CountryId = request.CountryId,
            RegionId = request.RegionId,
            CityId = request.CityId,
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            AcceptedPrivacyPolicy = true,
            PrivacyPolicyAcceptedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var userId = await _authRepository.CreateCompanyUserAsync(user);
        var createdUser = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new InvalidOperationException("Грешка при създаване на фирмения акаунт.");

        return await BuildAuthResponseAsync(createdUser, ipAddress, userAgent);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _authRepository.GetUserByEmailAsync(email);

        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Невалиден имейл или парола.");

        var isValidPassword = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!isValidPassword)
            throw new UnauthorizedAccessException("Невалиден имейл или парола.");

        return await BuildAuthResponseAsync(user, ipAddress, userAgent);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Липсва refresh token.");

        var tokenHash = _jwtTokenService.HashToken(refreshToken);
        var existingToken = await _authRepository.GetRefreshTokenByHashAsync(tokenHash);

        if (existingToken is null || !existingToken.IsActive)
            throw new UnauthorizedAccessException("Невалиден refresh token.");

        var user = await _authRepository.GetUserByIdAsync(existingToken.UserId);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Потребителят не е активен.");

        var newRefreshTokenPlain = _jwtTokenService.CreateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashToken(newRefreshTokenPlain);

        await _authRepository.RevokeRefreshTokenAsync(existingToken.Id, newRefreshTokenHash, ipAddress);

        _lastRefreshTokenPlain = newRefreshTokenPlain;

        await _authRepository.InsertRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        });

        var accessToken = _jwtTokenService.CreateAccessToken(user, out var accessTokenExpiresAtUtc);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            User = MapUser(user)
        };
    }

    public async Task LogoutAsync(string refreshToken, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = _jwtTokenService.HashToken(refreshToken);
        var existingToken = await _authRepository.GetRefreshTokenByHashAsync(tokenHash);

        if (existingToken is null || !existingToken.IsActive)
            return;

        await _authRepository.RevokeRefreshTokenAsync(existingToken.Id, null, ipAddress);
    }

    public async Task<UserProfileResponse> UpdatePrivateProfileAsync(long userId, UpdatePrivateProfileRequest request)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");

        if (user.AccountType != "PRIVATE")
            throw new InvalidOperationException("Само private акаунт може да редактира този профил през този endpoint.");

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var phone = NormalizePhone(request.Phone);

        if (await _authRepository.PhoneExistsAsync(phone, user.Id))
            throw new InvalidOperationException("Този телефон вече се използва.");

        await _authRepository.UpdatePrivateProfileAsync(
            user.Id,
            firstName,
            lastName,
            phone,
            request.CountryId,
            request.RegionId,
            request.CityId);

        var updatedUser = await _authRepository.GetUserByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Грешка при обновяване на профила.");

        return MapUser(updatedUser);
    }

    public async Task DeleteMyProfileAsync(long userId, DeleteProfileRequest request)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");

        var isValidPassword = _passwordService.VerifyPassword(user, user.PasswordHash, request.CurrentPassword);
        if (!isValidPassword)
            throw new UnauthorizedAccessException("Текущата парола е грешна.");

        await _authRepository.SoftDeleteUserAndDeleteRelatedDataAsync(user.Id);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, string? ipAddress)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");

        if (user.AccountType != "PRIVATE")
            throw new InvalidOperationException("Фирмените акаунти нямат право да сменят паролата си.");

        var isValidPassword = _passwordService.VerifyPassword(user, user.PasswordHash, request.CurrentPassword);
        if (!isValidPassword)
            throw new UnauthorizedAccessException("Текущата парола е грешна.");

        if (request.CurrentPassword == request.NewPassword)
            throw new InvalidOperationException("Новата парола трябва да е различна от старата.");

        var newHash = _passwordService.HashPassword(user, request.NewPassword);
        await _authRepository.UpdateUserPasswordHashAsync(user.Id, newHash);

        await _authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, ipAddress);
    }

    public async Task ChangeEmailAsync(long userId, ChangeEmailRequest request, string? ipAddress)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");

        if (user.AccountType != "PRIVATE")
            throw new InvalidOperationException("Фирмените акаунти нямат право да сменят имейла си.");

        var isValidPassword = _passwordService.VerifyPassword(user, user.PasswordHash, request.CurrentPassword);
        if (!isValidPassword)
            throw new UnauthorizedAccessException("Текущата парола е грешна.");

        var newEmail = NormalizeEmail(request.NewEmail);

        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Новият имейл е същият като текущия.");

        if (await _authRepository.EmailExistsAsync(newEmail, user.Id))
            throw new InvalidOperationException("Този имейл вече се използва.");

        await _authRepository.UpdateUserEmailAsync(user.Id, newEmail);

        await _authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, ipAddress);
    }

    public async Task<UserProfileResponse> GetMeAsync(long userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Потребителят не е намерен.");

        return MapUser(user);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, string? ipAddress, string? userAgent)
    {
        var accessToken = _jwtTokenService.CreateAccessToken(user, out var accessTokenExpiresAtUtc);

        var refreshTokenPlain = _jwtTokenService.CreateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(refreshTokenPlain);

        await _authRepository.InsertRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        });

        _lastRefreshTokenPlain = refreshTokenPlain;

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            User = MapUser(user)
        };
    }

    private string? _lastRefreshTokenPlain;

    public string? ConsumeLatestRefreshToken()
    {
        var value = _lastRefreshTokenPlain;
        _lastRefreshTokenPlain = null;
        return value;
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim();

        var hasLeadingPlus = phone.StartsWith("+");
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digitsOnly))
            throw new InvalidOperationException("Невалиден телефонен номер.");

        return hasLeadingPlus ? $"+{digitsOnly}" : digitsOnly;
    }

    private static UserProfileResponse MapUser(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            RoleName = user.RoleName,
            AccountType = user.AccountType,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            CompanyVatNumber = user.CompanyVatNumber,
            ContactPerson = user.ContactPerson,
            Phone = user.Phone,
            Email = user.Email,
            CountryId = user.CountryId,
            RegionId = user.RegionId,
            CityId = user.CityId,
            LogoUrl = user.LogoUrl,
            PublishedListingsTotalCount = user.PublishedListingsTotalCount,
            PrivateFreeUsedCount = user.PrivateFreeUsedCount,
            CompanyStarterFreeUsedCount = user.CompanyStarterFreeUsedCount,
            CompanyMonthlyFreeUsedCount = user.CompanyMonthlyFreeUsedCount
        };
    }
}