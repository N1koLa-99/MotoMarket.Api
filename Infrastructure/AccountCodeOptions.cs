namespace MotoMarket.Api.Infrastructure;

public class AccountCodeOptions
{
    public const string SectionName = "AccountCodes";

    public int EmailVerificationCodeMinutes { get; set; } = 15;
    public int PasswordResetCodeMinutes { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;

    // Сложи дълга random стойност в appsettings / Azure App Settings.
    public string SecretKey { get; set; } = string.Empty;
}