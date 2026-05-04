using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MotoMarket.Api.Services;

public class AuthService : IAuthService
{
    private const string PurposeEmailConfirmation = "EMAIL_CONFIRMATION";
    private const string PurposePasswordReset = "PASSWORD_RESET";

    private readonly IAuthRepository _authRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IEmailSender _emailSender;

    private readonly JwtOptions _jwtOptions;
    private readonly AccountCodeOptions _accountCodeOptions;

    private string? _lastRefreshTokenPlain;

    public AuthService(
        IAuthRepository authRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IAccountCodeRepository accountCodeRepository,
        IEmailSender emailSender,
        IOptions<AccountCodeOptions> accountCodeOptions)
    {
        _authRepository = authRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _accountCodeRepository = accountCodeRepository;
        _emailSender = emailSender;
        _accountCodeOptions = accountCodeOptions.Value;
    }

    public async Task<RegisterStartResponse> RegisterPrivateAsync(
        RegisterPrivateRequest request,
        string? ipAddress,
        string? userAgent)
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
            IsActive = true,
            EmailConfirmed = false,
            EmailConfirmedAtUtc = null
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var userId = await _authRepository.CreatePrivateUserAsync(user);

        var createdUser = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new InvalidOperationException("Грешка при създаване на потребителя.");

        await SendAccountCodeAsync(createdUser, PurposeEmailConfirmation, ipAddress);

        return new RegisterStartResponse
        {
            Success = true,
            RequiresEmailVerification = true,
            Email = createdUser.Email,
            Message = "Профилът е създаден. Изпратихме код за потвърждение на имейла."
        };
    }

    public async Task<RegisterStartResponse> RegisterCompanyAsync(
        RegisterCompanyRequest request,
        string? ipAddress,
        string? userAgent)
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
            IsActive = true,
            EmailConfirmed = false,
            EmailConfirmedAtUtc = null
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var userId = await _authRepository.CreateCompanyUserAsync(user);

        var createdUser = await _authRepository.GetUserByIdAsync(userId)
            ?? throw new InvalidOperationException("Грешка при създаване на фирмения акаунт.");

        await SendAccountCodeAsync(createdUser, PurposeEmailConfirmation, ipAddress);

        return new RegisterStartResponse
        {
            Success = true,
            RequiresEmailVerification = true,
            Email = createdUser.Email,
            Message = "Фирменият профил е създаден. Изпратихме код за потвърждение на имейла."
        };
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

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Имейлът не е потвърден. Провери пощата си и въведи кода за потвърждение.");

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

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Имейлът не е потвърден.");

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

    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _authRepository.GetUserByEmailAsync(email)
            ?? throw new InvalidOperationException("Невалиден имейл или код.");

        if (!user.IsActive)
            throw new InvalidOperationException("Невалиден имейл или код.");

        if (user.EmailConfirmed)
            return;

        await ValidateAndConsumeCodeAsync(user.Id, PurposeEmailConfirmation, request.Code);

        await _accountCodeRepository.ConfirmUserEmailAsync(user.Id);
    }

    public async Task ResendEmailVerificationAsync(
        ResendEmailVerificationRequest request,
        string? ipAddress)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _authRepository.GetUserByEmailAsync(email);

        if (user is null || !user.IsActive || user.EmailConfirmed)
            return;

        await SendAccountCodeAsync(user, PurposeEmailConfirmation, ipAddress);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _authRepository.GetUserByEmailAsync(email);

        if (user is null || !user.IsActive || !user.EmailConfirmed)
            return;

        await SendAccountCodeAsync(user, PurposePasswordReset, ipAddress);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, string? ipAddress)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new InvalidOperationException("Новата парола и потвърждението не съвпадат.");

        var email = NormalizeEmail(request.Email);

        var user = await _authRepository.GetUserByEmailAsync(email)
            ?? throw new InvalidOperationException("Невалиден имейл или код.");

        if (!user.IsActive)
            throw new InvalidOperationException("Невалиден имейл или код.");

        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Имейлът не е потвърден.");

        await ValidateAndConsumeCodeAsync(user.Id, PurposePasswordReset, request.Code);

        var newHash = _passwordService.HashPassword(user, request.NewPassword);

        await _authRepository.UpdateUserPasswordHashAsync(user.Id, newHash);

        await _authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, ipAddress);
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

    public string? ConsumeLatestRefreshToken()
    {
        var value = _lastRefreshTokenPlain;
        _lastRefreshTokenPlain = null;
        return value;
    }

    private async Task SendAccountCodeAsync(User user, string purpose, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(_accountCodeOptions.SecretKey))
            throw new InvalidOperationException("AccountCodes:SecretKey липсва.");

        await _accountCodeRepository.ConsumeActiveCodesAsync(user.Id, purpose);

        var code = CreateSixDigitCode();
        var codeHash = HashAccountCode(user.Id, purpose, code);

        var minutes = purpose == PurposePasswordReset
            ? _accountCodeOptions.PasswordResetCodeMinutes
            : _accountCodeOptions.EmailVerificationCodeMinutes;

        await _accountCodeRepository.InsertAsync(new UserAccountCode
        {
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(minutes),
            CreatedByIp = ipAddress
        });

        if (purpose == PurposeEmailConfirmation)
        {
            await _emailSender.SendAsync(
                user.Email,
                "Код за потвърждение на имейл - Moto Zona",
                $"Твоят код за потвърждение е: {code}\n\nКодът е валиден {minutes} минути.",
                $@"
<p>Здравей,</p>
<p>Твоят код за потвърждение е:</p>
<h2 style=""letter-spacing:3px;"">{code}</h2>
<p>Кодът е валиден {minutes} минути.</p>
<p>Moto Zona</p>");
        }
        else if (purpose == PurposePasswordReset)
        {
            await _emailSender.SendAsync(
                user.Email,
                "Код за смяна на парола - Moto Zona",
                $"Твоят код за смяна на парола е: {code}\n\nКодът е валиден {minutes} минути.",
                $@"
<p>Здравей,</p>
<p>Твоят код за смяна на парола е:</p>
<h2 style=""letter-spacing:3px;"">{code}</h2>
<p>Кодът е валиден {minutes} минути.</p>
<p>Ако не си заявил смяна на парола, игнорирай този имейл.</p>
<p>Moto Zona</p>");
        }
    }

    private async Task ValidateAndConsumeCodeAsync(long userId, string purpose, string rawCode)
    {
        var code = await _accountCodeRepository.GetLatestActiveCodeAsync(
            userId,
            purpose,
            DateTime.UtcNow);

        if (code is null)
            throw new InvalidOperationException("Кодът е невалиден или е изтекъл.");

        if (code.AttemptCount >= _accountCodeOptions.MaxAttempts)
            throw new InvalidOperationException("Кодът е блокиран след твърде много грешни опити. Заяви нов код.");

        var incomingHash = HashAccountCode(userId, purpose, rawCode.Trim());

        var isValid = SecureEquals(incomingHash, code.CodeHash);

        if (!isValid)
        {
            await _accountCodeRepository.IncreaseAttemptCountAsync(code.Id);
            throw new InvalidOperationException("Кодът е невалиден или е изтекъл.");
        }

        await _accountCodeRepository.MarkConsumedAsync(code.Id);
    }

    private static string CreateSixDigitCode()
    {
        var number = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return number.ToString("D6", CultureInfo.InvariantCulture);
    }

    private string HashAccountCode(long userId, string purpose, string code)
    {
        var secretBytes = Encoding.UTF8.GetBytes(_accountCodeOptions.SecretKey);
        var payload = $"{userId}|{purpose}|{code}";

        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        return Convert.ToBase64String(hashBytes);
    }

    private static bool SecureEquals(string leftBase64, string rightBase64)
    {
        try
        {
            var leftBytes = Convert.FromBase64String(leftBase64);
            var rightBytes = Convert.FromBase64String(rightBase64);

            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        catch
        {
            return false;
        }
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