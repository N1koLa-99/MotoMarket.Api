using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = @"
SELECT TOP 1 *
FROM dbo.Users
WHERE Email = @Email
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = @"
SELECT TOP 1 *
FROM dbo.Users
WHERE Id = @UserId
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<bool> EmailExistsAsync(string email, long? excludeUserId = null)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM dbo.Users
    WHERE Email = @Email
      AND IsActive = 1
      AND (@ExcludeUserId IS NULL OR Id <> @ExcludeUserId)
)
THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new
        {
            Email = email,
            ExcludeUserId = excludeUserId
        });
    }

    public async Task<bool> PhoneExistsAsync(string phone, long? excludeUserId = null)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM dbo.Users
    WHERE Phone = @Phone
      AND IsActive = 1
      AND (@ExcludeUserId IS NULL OR Id <> @ExcludeUserId)
)
THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new
        {
            Phone = phone,
            ExcludeUserId = excludeUserId
        });
    }

    public async Task<bool> CompanyVatExistsAsync(string vatNumber)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM dbo.Users
    WHERE CompanyVatNumber = @VatNumber
)
THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { VatNumber = vatNumber });
    }

    public async Task<long> CreatePrivateUserAsync(User user)
    {
        const string sql = @"
INSERT INTO dbo.Users
(
    RoleName,
    AccountType,
    FirstName,
    LastName,
    Phone,
    Email,
    PasswordHash,
    CountryId,
    RegionId,
    CityId,
    AcceptedPrivacyPolicy,
    PrivacyPolicyAcceptedAtUtc,
    IsActive
)
VALUES
(
    @RoleName,
    @AccountType,
    @FirstName,
    @LastName,
    @Phone,
    @Email,
    @PasswordHash,
    @CountryId,
    @RegionId,
    @CityId,
    @AcceptedPrivacyPolicy,
    @PrivacyPolicyAcceptedAtUtc,
    1
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, user);
    }

    public async Task<long> CreateCompanyUserAsync(User user)
    {
        const string sql = @"
INSERT INTO dbo.Users
(
    RoleName,
    AccountType,
    CompanyName,
    CompanyVatNumber,
    ContactPerson,
    Phone,
    Email,
    PasswordHash,
    CountryId,
    RegionId,
    CityId,
    LogoUrl,
    AcceptedPrivacyPolicy,
    PrivacyPolicyAcceptedAtUtc,
    IsActive
)
VALUES
(
    @RoleName,
    @AccountType,
    @CompanyName,
    @CompanyVatNumber,
    @ContactPerson,
    @Phone,
    @Email,
    @PasswordHash,
    @CountryId,
    @RegionId,
    @CityId,
    @LogoUrl,
    @AcceptedPrivacyPolicy,
    @PrivacyPolicyAcceptedAtUtc,
    1
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, user);
    }

    public async Task UpdateUserPasswordHashAsync(long userId, string newPasswordHash)
    {
        const string sql = @"
UPDATE dbo.Users
SET PasswordHash = @NewPasswordHash
WHERE Id = @UserId;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            NewPasswordHash = newPasswordHash
        });
    }

    public async Task UpdateUserEmailAsync(long userId, string newEmail)
    {
        const string sql = @"
UPDATE dbo.Users
SET Email = @NewEmail
WHERE Id = @UserId;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            NewEmail = newEmail
        });
    }

    public async Task UpdatePrivateProfileAsync(
        long userId,
        string firstName,
        string lastName,
        string phone,
        int countryId,
        int? regionId,
        int? cityId)
    {
        const string sql = @"
UPDATE dbo.Users
SET
    FirstName = @FirstName,
    LastName = @LastName,
    Phone = @Phone,
    CountryId = @CountryId,
    RegionId = @RegionId,
    CityId = @CityId
WHERE Id = @UserId
  AND AccountType = 'PRIVATE'
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            CountryId = countryId,
            RegionId = regionId,
            CityId = cityId
        });
    }

    public async Task SoftDeleteUserAndDeleteRelatedDataAsync(long userId)
    {
        const string sql = @"
SET XACT_ABORT ON;

DECLARE @TargetUserId BIGINT = @UserId;

IF OBJECT_ID('tempdb..#UserListingIds') IS NOT NULL
    DROP TABLE #UserListingIds;

CREATE TABLE #UserListingIds
(
    Id BIGINT NOT NULL PRIMARY KEY
);

-- 1) Всички обяви на потребителя
IF OBJECT_ID(N'dbo.Listings', N'U') IS NOT NULL
BEGIN
    INSERT INTO #UserListingIds (Id)
    SELECT L.Id
    FROM dbo.Listings L
    WHERE L.UserId = @TargetUserId;
END

-- 2) Снимки към обявите
IF OBJECT_ID(N'dbo.ListingPhotos', N'U') IS NOT NULL
BEGIN
    DELETE LP
    FROM dbo.ListingPhotos LP
    INNER JOIN #UserListingIds UL ON UL.Id = LP.ListingId;
END

-- 3) История на цените
IF OBJECT_ID(N'dbo.ListingPriceHistory', N'U') IS NOT NULL
BEGIN
    DELETE LPH
    FROM dbo.ListingPriceHistory LPH
    INNER JOIN #UserListingIds UL ON UL.Id = LPH.ListingId;
END

-- 4) Pending actions
IF OBJECT_ID(N'dbo.PendingListingActions', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.PendingListingActions')
          AND name = 'ListingId'
    )
    BEGIN
        DELETE PLA
        FROM dbo.PendingListingActions PLA
        INNER JOIN #UserListingIds UL ON UL.Id = PLA.ListingId;
    END

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.PendingListingActions')
          AND name = 'UserId'
    )
    BEGIN
        DELETE
        FROM dbo.PendingListingActions
        WHERE UserId = @TargetUserId;
    END
END

-- 5) Favorites
IF OBJECT_ID(N'dbo.Favorites', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Favorites')
          AND name = 'UserId'
    )
    AND EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Favorites')
          AND name = 'ListingId'
    )
    BEGIN
        DELETE F
        FROM dbo.Favorites F
        WHERE F.UserId = @TargetUserId
           OR F.ListingId IN (SELECT Id FROM #UserListingIds);
    END
    ELSE IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Favorites')
          AND name = 'UserId'
    )
    BEGIN
        DELETE
        FROM dbo.Favorites
        WHERE UserId = @TargetUserId;
    END
END

-- 6) Payments
IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Payments')
          AND name = 'UserId'
    )
    AND EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Payments')
          AND name = 'ListingId'
    )
    BEGIN
        DELETE P
        FROM dbo.Payments P
        WHERE P.UserId = @TargetUserId
           OR P.ListingId IN (SELECT Id FROM #UserListingIds);
    END
    ELSE IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Payments')
          AND name = 'UserId'
    )
    BEGIN
        DELETE
        FROM dbo.Payments
        WHERE UserId = @TargetUserId;
    END
    ELSE IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Payments')
          AND name = 'ListingId'
    )
    BEGIN
        DELETE P
        FROM dbo.Payments P
        INNER JOIN #UserListingIds UL ON UL.Id = P.ListingId;
    END
END

-- 7) Refresh tokens
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NOT NULL
BEGIN
    DELETE RT
    FROM dbo.RefreshTokens RT
    WHERE RT.UserId = @TargetUserId;
END

-- 8) Обяви
IF OBJECT_ID(N'dbo.Listings', N'U') IS NOT NULL
BEGIN
    DELETE L
    FROM dbo.Listings L
    WHERE L.UserId = @TargetUserId;
END

-- 9) User -> inactive + anonymized
UPDATE dbo.Users
SET
    IsActive = 0,
    FirstName = NULL,
    LastName = NULL,
    CompanyName = NULL,
    CompanyVatNumber = NULL,
    ContactPerson = NULL,
    Phone = CONCAT('deleted_', Id),
    Email = CONCAT('deleted_', Id, '@deleted.local'),
    PasswordHash = '',
    RegionId = NULL,
    CityId = NULL,
    LogoUrl = NULL,
    PublishedListingsTotalCount = 0,
    PrivateFreeUsedCount = 0,
    CompanyStarterFreeUsedCount = 0,
    CompanyMonthlyFreeUsedCount = 0,
    CompanyMonthlyQuotaYear = NULL,
    CompanyMonthlyQuotaMonth = NULL
WHERE Id = @TargetUserId;
";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, new { UserId = userId }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<long> InsertRefreshTokenAsync(RefreshToken token)
    {
        const string sql = @"
INSERT INTO dbo.RefreshTokens
(
    UserId,
    TokenHash,
    ExpiresAt,
    CreatedAt,
    CreatedByIp,
    UserAgent
)
VALUES
(
    @UserId,
    @TokenHash,
    @ExpiresAt,
    @CreatedAt,
    @CreatedByIp,
    @UserAgent
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, token);
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        const string sql = @"
SELECT TOP 1 *
FROM dbo.RefreshTokens
WHERE TokenHash = @TokenHash;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { TokenHash = tokenHash });
    }

    public async Task RevokeRefreshTokenAsync(long refreshTokenId, string? replacedByTokenHash, string? revokedByIp)
    {
        const string sql = @"
UPDATE dbo.RefreshTokens
SET
    RevokedAt = SYSUTCDATETIME(),
    ReplacedByTokenHash = @ReplacedByTokenHash,
    RevokedByIp = @RevokedByIp
WHERE Id = @RefreshTokenId
  AND RevokedAt IS NULL;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            RefreshTokenId = refreshTokenId,
            ReplacedByTokenHash = replacedByTokenHash,
            RevokedByIp = revokedByIp
        });
    }

    public async Task RevokeAllActiveRefreshTokensForUserAsync(long userId, string? revokedByIp)
    {
        const string sql = @"
UPDATE dbo.RefreshTokens
SET
    RevokedAt = SYSUTCDATETIME(),
    RevokedByIp = @RevokedByIp
WHERE UserId = @UserId
  AND RevokedAt IS NULL
  AND ExpiresAt > SYSUTCDATETIME();";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            RevokedByIp = revokedByIp
        });
    }
}