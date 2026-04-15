using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProfileRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = """
SELECT TOP 1 *
FROM dbo.Users
WHERE Id = @UserId AND IsActive = 1;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<int> GetActiveListingsCountAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Listings
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<int> GetFavoritesCountAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Favorites
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<int> GetPaidListingActionsCountAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Payments
WHERE UserId = @UserId
  AND ServiceType = 'LISTING'
  AND Status = 'PAID';
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<int> GetPaymentsCountAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Payments
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<decimal> GetTotalPaidAmountAsync(long userId)
    {
        const string sql = """
SELECT ISNULL(SUM(AmountEUR), 0)
FROM dbo.Payments
WHERE UserId = @UserId
  AND Status = 'PAID';
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<decimal>(sql, new { UserId = userId });
    }

    public async Task<int> CountOwnListingsAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Listings
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<List<ProfileListingCardResponse>> GetOwnListingsAsync(long userId, int page, int pageSize)
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

    public async Task<int> CountFavoriteListingsAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Favorites f
INNER JOIN dbo.Listings l ON l.Id = f.ListingId
WHERE f.UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<List<ProfileListingCardResponse>> GetFavoriteListingsAsync(long userId, int page, int pageSize)
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
FROM dbo.Favorites f
INNER JOIN dbo.Listings l ON l.Id = f.ListingId
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
WHERE f.UserId = @UserId
ORDER BY f.CreatedAt DESC, l.Id DESC
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

    public async Task<int> CountPaymentsAsync(long userId)
    {
        const string sql = """
SELECT COUNT(1)
FROM dbo.Payments
WHERE UserId = @UserId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<List<PaymentHistoryItemResponse>> GetPaymentsAsync(long userId, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        const string sql = """
SELECT
    Id,
    UserId,
    ListingId,
    ServiceType,
    AmountEUR,
    ProviderName,
    ProviderPaymentId,
    ProviderOrderId,
    ListingTitleSnapshot,
    Status,
    CreatedAt,
    PaidAt,
    Note
FROM dbo.Payments
WHERE UserId = @UserId
ORDER BY CreatedAt DESC, Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<PaymentHistoryItemResponse>(sql, new
        {
            UserId = userId,
            Offset = offset,
            PageSize = pageSize
        });

        return result.ToList();
    }

    public async Task<bool> IsFavoriteAsync(long userId, long listingId)
    {
        const string sql = """
SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM dbo.Favorites
        WHERE UserId = @UserId AND ListingId = @ListingId
    ) THEN CAST(1 AS BIT)
    ELSE CAST(0 AS BIT)
END;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = userId, ListingId = listingId });
    }

    public async Task AddFavoriteAsync(long userId, long listingId)
    {
        const string sql = """
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Favorites
    WHERE UserId = @UserId AND ListingId = @ListingId
)
BEGIN
    INSERT INTO dbo.Favorites (UserId, ListingId)
    VALUES (@UserId, @ListingId);
END
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, ListingId = listingId });
    }

    public async Task RemoveFavoriteAsync(long userId, long listingId)
    {
        const string sql = """
DELETE FROM dbo.Favorites
WHERE UserId = @UserId
  AND ListingId = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, ListingId = listingId });
    }

    public async Task<bool> ListingExistsAsync(long listingId)
    {
        const string sql = """
SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM dbo.Listings
        WHERE Id = @ListingId
    ) THEN CAST(1 AS BIT)
    ELSE CAST(0 AS BIT)
END;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { ListingId = listingId });
    }
}
