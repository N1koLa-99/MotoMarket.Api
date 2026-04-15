using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AdminRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = """
SELECT TOP 1 *
FROM dbo.Users
WHERE Id = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<int> CountUsersAsync(string? searchTerm)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Users u
WHERE
    @SearchTerm IS NULL
    OR u.Email LIKE '%' + @SearchTerm + '%'
    OR u.Phone LIKE '%' + @SearchTerm + '%'
    OR u.FirstName LIKE '%' + @SearchTerm + '%'
    OR u.LastName LIKE '%' + @SearchTerm + '%'
    OR u.CompanyName LIKE '%' + @SearchTerm + '%'
    OR u.CompanyVatNumber LIKE '%' + @SearchTerm + '%';
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim()
        });
    }

    public async Task<List<AdminUserListItemResponse>> GetUsersAsync(string? searchTerm, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string sql = """
SELECT
    u.Id,
    u.RoleName,
    u.AccountType,
    u.FirstName,
    u.LastName,
    u.CompanyName,
    u.CompanyVatNumber,
    u.ContactPerson,
    u.Email,
    u.Phone,
    u.PublishedListingsTotalCount,
    ISNULL(al.ActiveListingsCount, 0) AS ActiveListingsCount,
    ISNULL(p.PaymentsCount, 0) AS PaymentsCount,
    ISNULL(p.RevenueGeneratedEUR, 0) AS RevenueGeneratedEUR,
    u.CreatedAt,
    u.IsActive
FROM dbo.Users u
OUTER APPLY
(
    SELECT COUNT(1) AS ActiveListingsCount
    FROM dbo.Listings l
    WHERE l.UserId = u.Id
) al
OUTER APPLY
(
    SELECT
        COUNT(1) AS PaymentsCount,
        ISNULL(SUM(CASE WHEN Status = 'PAID' THEN AmountEUR ELSE 0 END), 0) AS RevenueGeneratedEUR
    FROM dbo.Payments p
    WHERE p.UserId = u.Id
) p
WHERE
    @SearchTerm IS NULL
    OR u.Email LIKE '%' + @SearchTerm + '%'
    OR u.Phone LIKE '%' + @SearchTerm + '%'
    OR u.FirstName LIKE '%' + @SearchTerm + '%'
    OR u.LastName LIKE '%' + @SearchTerm + '%'
    OR u.CompanyName LIKE '%' + @SearchTerm + '%'
    OR u.CompanyVatNumber LIKE '%' + @SearchTerm + '%'
ORDER BY u.CreatedAt DESC, u.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<AdminUserListItemResponse>(sql, new
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            Offset = offset,
            PageSize = pageSize
        });

        return result.ToList();
    }

    public async Task<AdminUserDetailsResponse?> GetUserDetailsAsync(long userId)
    {
        const string sql = """
SELECT TOP 1
    u.Id,
    u.RoleName,
    u.AccountType,
    u.FirstName,
    u.LastName,
    u.CompanyName,
    u.CompanyVatNumber,
    u.ContactPerson,
    u.Email,
    u.Phone,
    u.CountryId,
    u.RegionId,
    u.CityId,
    c.NameBg AS CountryName,
    r.NameBg AS RegionName,
    ct.NameBg AS CityName,
    u.PublishedListingsTotalCount,
    ISNULL(al.ActiveListingsCount, 0) AS ActiveListingsCount,
    ISNULL(f.FavoritesCount, 0) AS FavoritesCount,
    ISNULL(p.PaymentsCount, 0) AS PaymentsCount,
    ISNULL(p.RevenueGeneratedEUR, 0) AS RevenueGeneratedEUR,
    u.PrivateFreeUsedCount,
    u.CompanyStarterFreeUsedCount,
    u.CompanyMonthlyFreeUsedCount,
    u.CompanyMonthlyQuotaYear,
    u.CompanyMonthlyQuotaMonth,
    u.CreatedAt,
    u.IsActive
FROM dbo.Users u
LEFT JOIN dbo.Countries c ON c.Id = u.CountryId
LEFT JOIN dbo.Regions r ON r.Id = u.RegionId
LEFT JOIN dbo.Cities ct ON ct.Id = u.CityId
OUTER APPLY
(
    SELECT COUNT(1) AS ActiveListingsCount
    FROM dbo.Listings l
    WHERE l.UserId = u.Id
) al
OUTER APPLY
(
    SELECT COUNT(1) AS FavoritesCount
    FROM dbo.Favorites f
    WHERE f.UserId = u.Id
) f
OUTER APPLY
(
    SELECT
        COUNT(1) AS PaymentsCount,
        ISNULL(SUM(CASE WHEN Status = 'PAID' THEN AmountEUR ELSE 0 END), 0) AS RevenueGeneratedEUR
    FROM dbo.Payments p
    WHERE p.UserId = u.Id
) p
WHERE u.Id = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AdminUserDetailsResponse>(sql, new { UserId = userId });
    }

    public async Task<int> CountUserListingsAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Listings
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<List<ProfileListingCardResponse>> GetUserListingsAsync(long userId, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string sql = """
SELECT
    l.Id,
    l.Title,
    l.PriceOriginal,
    l.CurrencyCode,
    l.PriceEUR,
    l.VehicleYear,
    l.HorsePower,
    l.EngineCC,
    l.Mileage,
    l.ViewCount,
    CASE
        WHEN l.PromotionType = 'VIP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'VIP'
        WHEN l.PromotionType = 'TOP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'TOP'
        ELSE 'NORMAL'
    END AS CurrentPromotionType,
    l.PromotionEndAt,
    l.PublishedAt,
    l.LastRefreshAt,
    mc.NameBg AS MainCategoryName,
    sc1.NameBg AS SubCategoryName,
    b.Name AS BrandName,
    m.Name AS ModelName,
    lc.NameBg AS LicenseCategoryName,
    c.NameBg AS CountryName,
    r.NameBg AS RegionName,
    ct.NameBg AS CityName,
    p.BlobName AS MainPhotoBlobName,
    p.FileUrl AS MainPhotoUrl
FROM dbo.Listings l
LEFT JOIN dbo.LookupValues mc ON mc.Id = l.MainCategoryLookupId
LEFT JOIN dbo.LookupValues sc1 ON sc1.Id = l.SubCategoryLookupId
LEFT JOIN dbo.Brands b ON b.Id = l.BrandId
LEFT JOIN dbo.Models m ON m.Id = l.ModelId
LEFT JOIN dbo.LookupValues lc ON lc.Id = l.LicenseCategoryLookupId
LEFT JOIN dbo.Countries c ON c.Id = l.CountryId
LEFT JOIN dbo.Regions r ON r.Id = l.RegionId
LEFT JOIN dbo.Cities ct ON ct.Id = l.CityId
OUTER APPLY
(
    SELECT TOP 1
        lp.BlobName,
        lp.FileUrl
    FROM dbo.ListingPhotos lp
    WHERE lp.ListingId = l.Id
    ORDER BY lp.IsMain DESC, lp.SortOrder ASC, lp.Id ASC
) p
WHERE l.UserId = @UserId
ORDER BY COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<ProfileListingCardResponse>(sql, new
        {
            UserId = userId,
            Offset = offset,
            PageSize = pageSize
        });

        return result.ToList();
    }

    public async Task<int> CountPaymentsAsync(string? searchTerm)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Payments p
LEFT JOIN dbo.Users u ON u.Id = p.UserId
WHERE
    @SearchTerm IS NULL
    OR u.Email LIKE '%' + @SearchTerm + '%'
    OR u.Phone LIKE '%' + @SearchTerm + '%'
    OR p.ServiceType LIKE '%' + @SearchTerm + '%'
    OR p.Status LIKE '%' + @SearchTerm + '%'
    OR p.ProviderPaymentId LIKE '%' + @SearchTerm + '%'
    OR p.ProviderOrderId LIKE '%' + @SearchTerm + '%';
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim()
        });
    }

    public async Task<List<PaymentHistoryItemResponse>> GetPaymentsAsync(string? searchTerm, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string sql = """
SELECT
    p.Id,
    p.UserId,
    p.ListingId,
    p.ServiceType,
    p.AmountEUR,
    p.ProviderName,
    p.ProviderPaymentId,
    p.ProviderOrderId,
    p.ListingTitleSnapshot,
    p.Status,
    p.CreatedAt,
    p.PaidAt,
    p.Note
FROM dbo.Payments p
LEFT JOIN dbo.Users u ON u.Id = p.UserId
WHERE
    @SearchTerm IS NULL
    OR u.Email LIKE '%' + @SearchTerm + '%'
    OR u.Phone LIKE '%' + @SearchTerm + '%'
    OR p.ServiceType LIKE '%' + @SearchTerm + '%'
    OR p.Status LIKE '%' + @SearchTerm + '%'
    OR p.ProviderPaymentId LIKE '%' + @SearchTerm + '%'
    OR p.ProviderOrderId LIKE '%' + @SearchTerm + '%'
ORDER BY p.CreatedAt DESC, p.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<PaymentHistoryItemResponse>(sql, new
        {
            SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim(),
            Offset = offset,
            PageSize = pageSize
        });

        return result.ToList();
    }

    public async Task<int> CountPendingActionsAsync(string? status)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.PendingListingActions
WHERE @Status IS NULL OR Status = @Status;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant()
        });
    }

    public async Task<List<AdminPendingActionResponse>> GetPendingActionsAsync(string? status, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string sql = """
SELECT
    pa.Id,
    pa.UserId,
    pa.ListingId,
    pa.ActionType,
    pa.AmountEUR,
    pa.CurrencyCode,
    pa.Status,
    pa.CreatedAt,
    pa.ExpiresAt,
    pa.CompletedAt,
    pa.ProviderName,
    pa.ProviderPaymentId,
    pa.ProviderOrderId,
    u.Email AS UserEmail,
    u.Phone AS UserPhone
FROM dbo.PendingListingActions pa
LEFT JOIN dbo.Users u ON u.Id = pa.UserId
WHERE @Status IS NULL OR pa.Status = @Status
ORDER BY pa.CreatedAt DESC, pa.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<AdminPendingActionResponse>(sql, new
        {
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant(),
            Offset = offset,
            PageSize = pageSize
        });

        return result.ToList();
    }
}