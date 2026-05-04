using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Entities;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using System.Text;

namespace MotoMarket.Api.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ListingRepository(ISqlConnectionFactory connectionFactory)
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

    public async Task<Listing?> GetListingByIdAsync(long listingId)
    {
        const string sql = """
SELECT TOP 1 *
FROM dbo.Listings
WHERE Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Listing>(sql, new { ListingId = listingId });
    }

    public async Task<List<ListingPhoto>> GetListingPhotosAsync(long listingId)
    {
        const string sql = """
SELECT
    Id,
    ListingId,
    FileName,
    FileUrl,
    BlobName,
    SortOrder,
    IsMain
FROM dbo.ListingPhotos
WHERE ListingId = @ListingId
ORDER BY IsMain DESC, SortOrder, Id;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<ListingPhoto>(sql, new { ListingId = listingId });
        return result.ToList();
    }

    public async Task<List<string>> GetListingBlobNamesAsync(long listingId)
    {
        const string sql = """
SELECT BlobName
FROM dbo.ListingPhotos
WHERE ListingId = @ListingId
  AND BlobName IS NOT NULL;
""";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<string>(sql, new { ListingId = listingId });
        return result.ToList();
    }

    public async Task<long> InsertListingAsync(Listing listing)
    {
        const string sql = """
INSERT INTO dbo.Listings
(
    UserId,
    MainCategoryLookupId,
    SubCategoryLookupId,
    SubCategory2LookupId,
    BrandId,
    ModelId,
    ItemModelText,
    LicenseCategoryLookupId,
    ConditionLookupId,
    Title,
    Description,
    VehicleYear,
    HorsePower,
    EngineCC,
    Mileage,
    Color,
    PriceOriginal,
    CurrencyCode,
    ExchangeRateToEUR,
    PriceEUR,
    CountryId,
    RegionId,
    CityId,
    ContactName,
    ContactPhone,
    PromotionType,
    PromotionStartAt,
    PromotionEndAt,
    LastRefreshAt,
    PublishedAt,
    UpdatedAt
)
VALUES
(
    @UserId,
    @MainCategoryLookupId,
    @SubCategoryLookupId,
    @SubCategory2LookupId,
    @BrandId,
    @ModelId,
    @ItemModelText,
    @LicenseCategoryLookupId,
    @ConditionLookupId,
    @Title,
    @Description,
    @VehicleYear,
    @HorsePower,
    @EngineCC,
    @Mileage,
    @Color,
    @PriceOriginal,
    @CurrencyCode,
    @ExchangeRateToEUR,
    @PriceEUR,
    @CountryId,
    @RegionId,
    @CityId,
    @ContactName,
    @ContactPhone,
    @PromotionType,
    @PromotionStartAt,
    @PromotionEndAt,
    @LastRefreshAt,
    @PublishedAt,
    @UpdatedAt
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, listing);
    }

    public async Task UpdateListingAsync(long listingId, UpdateListingRequest request, decimal priceEur)
    {
        const string sql = """
UPDATE dbo.Listings
SET
    MainCategoryLookupId = @MainCategoryLookupId,
    SubCategoryLookupId = @SubCategoryLookupId,
    SubCategory2LookupId = @SubCategory2LookupId,
    BrandId = @BrandId,
    ModelId = @ModelId,
    ItemModelText = @ItemModelText,
    LicenseCategoryLookupId = @LicenseCategoryLookupId,
    ConditionLookupId = @ConditionLookupId,
    Title = @Title,
    Description = @Description,
    VehicleYear = @VehicleYear,
    HorsePower = @HorsePower,
    EngineCC = @EngineCC,
    Mileage = @Mileage,
    Color = @Color,
    PriceOriginal = @PriceOriginal,
    CurrencyCode = @CurrencyCode,
    ExchangeRateToEUR = @ExchangeRateToEUR,
    PriceEUR = @PriceEUR,
    CountryId = @CountryId,
    RegionId = @RegionId,
    CityId = @CityId,
    ContactName = @ContactName,
    ContactPhone = @ContactPhone,
    UpdatedAt = @UpdatedAt
WHERE Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            ListingId = listingId,
            request.MainCategoryLookupId,
            request.SubCategoryLookupId,
            request.SubCategory2LookupId,
            request.BrandId,
            request.ModelId,
            request.ItemModelText,
            request.LicenseCategoryLookupId,
            request.ConditionLookupId,
            request.Title,
            request.Description,
            request.VehicleYear,
            request.HorsePower,
            request.EngineCC,
            request.Mileage,
            request.Color,
            request.PriceOriginal,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            request.ExchangeRateToEUR,
            PriceEUR = priceEur,
            request.CountryId,
            request.RegionId,
            request.CityId,
            request.ContactName,
            request.ContactPhone,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task ReplaceListingPhotosAsync(long listingId, List<ListingPhotoRequest> photos)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var tx = connection.BeginTransaction();

        const string deleteSql = "DELETE FROM dbo.ListingPhotos WHERE ListingId = @ListingId;";
        await connection.ExecuteAsync(deleteSql, new { ListingId = listingId }, tx);

        const string insertSql = """
INSERT INTO dbo.ListingPhotos
(
    ListingId,
    FileName,
    FileUrl,
    BlobName,
    SortOrder,
    IsMain
)
VALUES
(
    @ListingId,
    @FileName,
    @FileUrl,
    @BlobName,
    @SortOrder,
    @IsMain
);
""";

        foreach (var photo in photos)
        {
            await connection.ExecuteAsync(insertSql, new
            {
                ListingId = listingId,
                photo.FileName,
                photo.FileUrl,
                photo.BlobName,
                photo.SortOrder,
                photo.IsMain
            }, tx);
        }

        tx.Commit();
    }

    public async Task UpdateListingPromotionAsync(long listingId, string promotionType, DateTime startAt, DateTime endAt)
    {
        const string sql = """
UPDATE dbo.Listings
SET
    PromotionType = @PromotionType,
    PromotionStartAt = @StartAt,
    PromotionEndAt = @EndAt
WHERE Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            ListingId = listingId,
            PromotionType = promotionType,
            StartAt = startAt,
            EndAt = endAt
        });
    }

    public async Task RefreshListingAsync(long listingId, DateTime refreshAt)
    {
        const string sql = """
UPDATE dbo.Listings
SET
    LastRefreshAt = @RefreshAt
WHERE Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { ListingId = listingId, RefreshAt = refreshAt });
    }

    public async Task IncrementViewCountAsync(long listingId)
    {
        const string sql = """
UPDATE dbo.Listings
SET ViewCount = ViewCount + 1
WHERE Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { ListingId = listingId });
    }

    public async Task DeleteListingAsync(long listingId)
    {
        const string sql = "DELETE FROM dbo.Listings WHERE Id = @ListingId;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { ListingId = listingId });
    }

    public async Task IncreaseUserPublishedCountersAsync(long userId, string accountType)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (accountType == "PRIVATE")
        {
            const string sql = """
UPDATE dbo.Users
SET
    PublishedListingsTotalCount = PublishedListingsTotalCount + 1,
    PrivateFreeUsedCount =
        CASE WHEN PrivateFreeUsedCount < 3 THEN PrivateFreeUsedCount + 1 ELSE PrivateFreeUsedCount END
WHERE Id = @UserId;
""";
            await connection.ExecuteAsync(sql, new { UserId = userId });
            return;
        }

        const string companySql = """
DECLARE @nowYear SMALLINT = YEAR(GETDATE());
DECLARE @nowMonth TINYINT = MONTH(GETDATE());

UPDATE dbo.Users
SET
    PublishedListingsTotalCount = PublishedListingsTotalCount + 1,
    CompanyMonthlyQuotaYear =
        CASE
            WHEN CompanyStarterFreeUsedCount < 30 THEN CompanyMonthlyQuotaYear
            ELSE @nowYear
        END,
    CompanyMonthlyQuotaMonth =
        CASE
            WHEN CompanyStarterFreeUsedCount < 30 THEN CompanyMonthlyQuotaMonth
            ELSE @nowMonth
        END,
    CompanyStarterFreeUsedCount =
        CASE
            WHEN CompanyStarterFreeUsedCount < 30 THEN CompanyStarterFreeUsedCount + 1
            ELSE CompanyStarterFreeUsedCount
        END,
    CompanyMonthlyFreeUsedCount =
        CASE
            WHEN CompanyStarterFreeUsedCount < 30 THEN CompanyMonthlyFreeUsedCount
            WHEN CompanyMonthlyQuotaYear = @nowYear AND CompanyMonthlyQuotaMonth = @nowMonth
                THEN CASE WHEN CompanyMonthlyFreeUsedCount < 10 THEN CompanyMonthlyFreeUsedCount + 1 ELSE CompanyMonthlyFreeUsedCount END
            ELSE 1
        END
WHERE Id = @UserId;
""";

        await connection.ExecuteAsync(companySql, new { UserId = userId });
    }

    public async Task<long> InsertPaymentAsync(long userId, long? listingId, string serviceType, decimal amountEUR, string status, string? note)
    {
        const string sql = """
INSERT INTO dbo.Payments
(
    UserId,
    ListingId,
    ServiceType,
    AmountEUR,
    ProviderName,
    Status,
    CreatedAt,
    PaidAt,
    Note
)
VALUES
(
    @UserId,
    @ListingId,
    @ServiceType,
    @AmountEUR,
    N'myPOS',
    @Status,
    SYSUTCDATETIME(),
    CASE WHEN @Status = 'PAID' THEN SYSUTCDATETIME() ELSE NULL END,
    @Note
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            UserId = userId,
            ListingId = listingId,
            ServiceType = serviceType,
            AmountEUR = amountEUR,
            Status = status,
            Note = note
        });
    }

    public async Task<long> InsertPendingActionAsync(PendingListingAction action)
    {
        const string sql = """
INSERT INTO dbo.PendingListingActions
(
    UserId,
    ListingId,
    ActionType,
    PayloadJson,
    AmountEUR,
    CurrencyCode,
    Status,
    CreatedAt,
    ExpiresAt
)
VALUES
(
    @UserId,
    @ListingId,
    @ActionType,
    @PayloadJson,
    @AmountEUR,
    @CurrencyCode,
    @Status,
    @CreatedAt,
    @ExpiresAt
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(sql, action);
    }

    public async Task<int> CountPublicSearchAsync(PublicListingSearchRequest request)
    {
        var sql = new StringBuilder();
        sql.AppendLine("""
SELECT COUNT(1)
FROM dbo.Listings l
WHERE 1 = 1
""");

        AppendSearchWhere(sql, request);

        var parameters = BuildSearchParameters(request);

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql.ToString(), parameters);
    }

    public async Task<List<PublicListingCardResponse>> SearchPublicAsync(PublicListingSearchRequest request)
    {
        var orderBy = BuildOrderBy(request.SortBy);
        var offset = (request.Page - 1) * request.PageSize;

        var sql = new StringBuilder();
        sql.AppendLine("""
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
    ph.ChangeType AS LastPriceChangeType,
    ph.OldPriceOriginal AS PreviousPriceOriginal,
    ph.OldPriceEUR AS PreviousPriceEUR,
    ph.ChangedAt AS LastPriceChangeAt,
    CASE
        WHEN l.PromotionType = 'VIP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'VIP'
        WHEN l.PromotionType = 'TOP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'TOP'
        ELSE 'NORMAL'
    END AS CurrentPromotionType,
    mc.NameBg AS MainCategoryName,
    sc1.NameBg AS SubCategoryName,
    sc2.NameBg AS SubCategory2Name,
    b.Name AS BrandName,
    m.Name AS ModelName,
    lc.NameBg AS LicenseCategoryName,
    c.NameBg AS CountryName,
    r.NameBg AS RegionName,
    ct.NameBg AS CityName,
    l.PublishedAt,
    l.LastRefreshAt,
    p.BlobName AS MainPhotoBlobName,
    p.FileUrl AS MainPhotoUrl
FROM dbo.Listings l
LEFT JOIN dbo.LookupValues mc ON mc.Id = l.MainCategoryLookupId
LEFT JOIN dbo.LookupValues sc1 ON sc1.Id = l.SubCategoryLookupId
LEFT JOIN dbo.LookupValues sc2 ON sc2.Id = l.SubCategory2LookupId
LEFT JOIN dbo.Brands b ON b.Id = l.BrandId
LEFT JOIN dbo.Models m ON m.Id = l.ModelId
LEFT JOIN dbo.LookupValues lc ON lc.Id = l.LicenseCategoryLookupId
LEFT JOIN dbo.Countries c ON c.Id = l.CountryId
LEFT JOIN dbo.Regions r ON r.Id = l.RegionId
LEFT JOIN dbo.Cities ct ON ct.Id = l.CityId
OUTER APPLY
(
    SELECT TOP 1
        h.ChangeType,
        h.OldPriceOriginal,
        h.OldPriceEUR,
        h.ChangedAt
    FROM dbo.ListingPriceHistory h
    WHERE h.ListingId = l.Id
      AND h.ChangeType IN ('UP', 'DOWN')
    ORDER BY h.ChangedAt DESC, h.Id DESC
) ph
OUTER APPLY
(
    SELECT TOP 1
        lp.BlobName,
        lp.FileUrl
    FROM dbo.ListingPhotos lp
    WHERE lp.ListingId = l.Id
    ORDER BY lp.IsMain DESC, lp.SortOrder ASC, lp.Id ASC
) p
WHERE 1 = 1
""");

        AppendSearchWhere(sql, request);
        sql.AppendLine(orderBy);
        sql.AppendLine("OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

        var parameters = BuildSearchParameters(request);
        parameters.Add("Offset", offset);

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<PublicListingCardResponse>(sql.ToString(), parameters);
        return result.ToList();
    }

    public async Task<PublicListingDetailsResponse?> GetPublicDetailsAsync(long listingId)
    {
        const string sql = """
SELECT TOP 1
    l.Id,
    l.UserId,
    l.Title,
    l.Description,
    l.MainCategoryLookupId,
    l.SubCategoryLookupId,
    l.SubCategory2LookupId,
    l.BrandId,
    l.ModelId,
    l.ItemModelText,
    l.LicenseCategoryLookupId,
    l.ConditionLookupId,
    l.VehicleYear,
    l.HorsePower,
    l.EngineCC,
    l.Mileage,
    l.Color,
    l.PriceOriginal,
    l.CurrencyCode,
    l.ExchangeRateToEUR,
    l.PriceEUR,
    l.CountryId,
    l.RegionId,
    l.CityId,
    l.ContactName,
    l.ContactPhone,
    l.ViewCount,
    ph.ChangeType AS LastPriceChangeType,
    ph.OldPriceOriginal AS PreviousPriceOriginal,
    ph.OldPriceEUR AS PreviousPriceEUR,
    ph.ChangedAt AS LastPriceChangeAt,
    CASE
        WHEN l.PromotionType = 'VIP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'VIP'
        WHEN l.PromotionType = 'TOP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 'TOP'
        ELSE 'NORMAL'
    END AS CurrentPromotionType,
    l.PromotionStartAt,
    l.PromotionEndAt,
    l.LastRefreshAt,
    l.PublishedAt,
    mc.NameBg AS MainCategoryName,
    sc1.NameBg AS SubCategoryName,
    sc2.NameBg AS SubCategory2Name,
    b.Name AS BrandName,
    m.Name AS ModelName,
    lc.NameBg AS LicenseCategoryName,
    cond.NameBg AS ConditionName,
    c.NameBg AS CountryName,
    r.NameBg AS RegionName,
    ct.NameBg AS CityName
FROM dbo.Listings l
LEFT JOIN dbo.LookupValues mc ON mc.Id = l.MainCategoryLookupId
LEFT JOIN dbo.LookupValues sc1 ON sc1.Id = l.SubCategoryLookupId
LEFT JOIN dbo.LookupValues sc2 ON sc2.Id = l.SubCategory2LookupId
LEFT JOIN dbo.Brands b ON b.Id = l.BrandId
LEFT JOIN dbo.Models m ON m.Id = l.ModelId
LEFT JOIN dbo.LookupValues lc ON lc.Id = l.LicenseCategoryLookupId
LEFT JOIN dbo.LookupValues cond ON cond.Id = l.ConditionLookupId
LEFT JOIN dbo.Countries c ON c.Id = l.CountryId
LEFT JOIN dbo.Regions r ON r.Id = l.RegionId
LEFT JOIN dbo.Cities ct ON ct.Id = l.CityId
OUTER APPLY
(
    SELECT TOP 1
        h.ChangeType,
        h.OldPriceOriginal,
        h.OldPriceEUR,
        h.ChangedAt
    FROM dbo.ListingPriceHistory h
    WHERE h.ListingId = l.Id
      AND h.ChangeType IN ('UP', 'DOWN')
    ORDER BY h.ChangedAt DESC, h.Id DESC
) ph
WHERE l.Id = @ListingId;
""";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PublicListingDetailsResponse>(sql, new { ListingId = listingId });
    }

    private static DynamicParameters BuildSearchParameters(PublicListingSearchRequest request)
    {
        var parameters = new DynamicParameters();

        parameters.Add("SearchTerm", string.IsNullOrWhiteSpace(request.SearchTerm) ? null : request.SearchTerm.Trim());
        parameters.Add("MainCategoryLookupId", request.MainCategoryLookupId);
        parameters.Add("SubCategoryLookupId", request.SubCategoryLookupId);
        parameters.Add("SubCategory2LookupId", request.SubCategory2LookupId);
        parameters.Add("BrandId", request.BrandId);
        parameters.Add("ModelId", request.ModelId);
        parameters.Add("LicenseCategoryLookupId", request.LicenseCategoryLookupId);
        parameters.Add("ConditionLookupId", request.ConditionLookupId);
        parameters.Add("CountryId", request.CountryId);
        parameters.Add("RegionId", request.RegionId);
        parameters.Add("CityId", request.CityId);
        parameters.Add("PriceFrom", request.PriceFrom);
        parameters.Add("PriceTo", request.PriceTo);
        parameters.Add("YearFrom", request.YearFrom);
        parameters.Add("YearTo", request.YearTo);
        parameters.Add("HorsePowerFrom", request.HorsePowerFrom);
        parameters.Add("HorsePowerTo", request.HorsePowerTo);
        parameters.Add("EngineCcFrom", request.EngineCcFrom);
        parameters.Add("EngineCcTo", request.EngineCcTo);
        parameters.Add("MileageFrom", request.MileageFrom);
        parameters.Add("MileageTo", request.MileageTo);
        parameters.Add("PageSize", request.PageSize);

        return parameters;
    }

    private static void AppendSearchWhere(StringBuilder sql, PublicListingSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            sql.AppendLine("""
AND
(
    l.Title LIKE '%' + @SearchTerm + '%'
    OR l.Description LIKE '%' + @SearchTerm + '%'
)
""");
        }

        if (request.MainCategoryLookupId.HasValue)
            sql.AppendLine("AND l.MainCategoryLookupId = @MainCategoryLookupId");

        if (request.SubCategoryLookupId.HasValue)
            sql.AppendLine("AND l.SubCategoryLookupId = @SubCategoryLookupId");

        if (request.SubCategory2LookupId.HasValue)
            sql.AppendLine("AND l.SubCategory2LookupId = @SubCategory2LookupId");

        if (request.BrandId.HasValue)
            sql.AppendLine("AND l.BrandId = @BrandId");

        if (request.ModelId.HasValue)
            sql.AppendLine("AND l.ModelId = @ModelId");

        if (request.LicenseCategoryLookupId.HasValue)
            sql.AppendLine("AND l.LicenseCategoryLookupId = @LicenseCategoryLookupId");

        if (request.ConditionLookupId.HasValue)
            sql.AppendLine("AND l.ConditionLookupId = @ConditionLookupId");

        if (request.CountryId.HasValue)
            sql.AppendLine("AND l.CountryId = @CountryId");

        if (request.RegionId.HasValue)
            sql.AppendLine("AND l.RegionId = @RegionId");

        if (request.CityId.HasValue)
            sql.AppendLine("AND l.CityId = @CityId");

        if (request.PriceFrom.HasValue)
            sql.AppendLine("AND l.PriceEUR >= @PriceFrom");

        if (request.PriceTo.HasValue)
            sql.AppendLine("AND l.PriceEUR <= @PriceTo");

        if (request.YearFrom.HasValue)
            sql.AppendLine("AND l.VehicleYear >= @YearFrom");

        if (request.YearTo.HasValue)
            sql.AppendLine("AND l.VehicleYear <= @YearTo");

        if (request.HorsePowerFrom.HasValue)
            sql.AppendLine("AND l.HorsePower >= @HorsePowerFrom");

        if (request.HorsePowerTo.HasValue)
            sql.AppendLine("AND l.HorsePower <= @HorsePowerTo");

        if (request.EngineCcFrom.HasValue)
            sql.AppendLine("AND l.EngineCC >= @EngineCcFrom");

        if (request.EngineCcTo.HasValue)
            sql.AppendLine("AND l.EngineCC <= @EngineCcTo");

        if (request.MileageFrom.HasValue)
            sql.AppendLine("AND l.Mileage >= @MileageFrom");

        if (request.MileageTo.HasValue)
            sql.AppendLine("AND l.Mileage <= @MileageTo");
    }

    private static string BuildOrderBy(string? sortBy)
    {
        var normalized = (sortBy ?? "newest").Trim().ToLowerInvariant();

        var baseOrder = """
ORDER BY
CASE
    WHEN l.PromotionType = 'VIP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 1
    WHEN l.PromotionType = 'TOP' AND l.PromotionEndAt IS NOT NULL AND l.PromotionEndAt > SYSUTCDATETIME() THEN 2
    ELSE 3
END ASC,
""";

        return normalized switch
        {
            "priceasc" => baseOrder + "l.PriceEUR ASC, COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC",
            "pricedesc" => baseOrder + "l.PriceEUR DESC, COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC",
            "yeardesc" => baseOrder + "l.VehicleYear DESC, COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC",
            "yearasc" => baseOrder + "l.VehicleYear ASC, COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC",
            "oldest" => baseOrder + "COALESCE(l.LastRefreshAt, l.PublishedAt) ASC, l.Id ASC",
            _ => baseOrder + "COALESCE(l.LastRefreshAt, l.PublishedAt) DESC, l.Id DESC"
        };
    }

    public async Task InsertListingPriceHistoryAsync(ListingPriceHistory history)
    {
        const string sql = """
INSERT INTO dbo.ListingPriceHistory
(
    ListingId,
    ChangeType,
    OldPriceOriginal,
    NewPriceOriginal,
    OldPriceEUR,
    NewPriceEUR,
    CurrencyCode,
    ExchangeRateToEUR,
    ChangedAt
)
VALUES
(
    @ListingId,
    @ChangeType,
    @OldPriceOriginal,
    @NewPriceOriginal,
    @OldPriceEUR,
    @NewPriceEUR,
    @CurrencyCode,
    @ExchangeRateToEUR,
    @ChangedAt
);
""";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, history);
    }
}