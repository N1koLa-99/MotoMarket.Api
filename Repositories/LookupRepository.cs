using Dapper;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;

namespace MotoMarket.Api.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public LookupRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CountryResponse>> GetCountriesAsync()
    {
        const string sql = @"
SELECT
    Id,
    CountryCode,
    NameBg,
    NameEn,
    DefaultCurrencyCode,
    IsPrimaryMarket
FROM dbo.Countries
WHERE IsActive = 1
ORDER BY IsPrimaryMarket DESC, NameBg;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<CountryResponse>(sql);
    }

    public async Task<IEnumerable<RegionResponse>> GetRegionsByCountryIdAsync(int countryId)
    {
        const string sql = @"
SELECT
    Id,
    CountryId,
    RegionCode,
    NameBg,
    SortOrder
FROM dbo.Regions
WHERE CountryId = @CountryId
ORDER BY SortOrder, NameBg;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<RegionResponse>(sql, new { CountryId = countryId });
    }

    public async Task<IEnumerable<CityResponse>> GetCitiesByRegionIdAsync(int regionId)
    {
        const string sql = @"
SELECT
    Id,
    RegionId,
    NameBg,
    IsMajor,
    SortOrder
FROM dbo.Cities
WHERE RegionId = @RegionId
ORDER BY SortOrder, NameBg;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<CityResponse>(sql, new { RegionId = regionId });
    }

    public async Task<IEnumerable<LookupItemResponse>> GetLookupsByGroupAsync(string groupName)
    {
        const string sql = @"
SELECT
    Id,
    GroupName,
    ParentId,
    Code,
    NameBg,
    SortOrder
FROM dbo.LookupValues
WHERE GroupName = @GroupName
  AND IsActive = 1
ORDER BY SortOrder, NameBg;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LookupItemResponse>(sql, new { GroupName = groupName });
    }

    public async Task<IEnumerable<LookupItemResponse>> GetLookupsByGroupAndParentIdAsync(string groupName, int parentId)
    {
        const string sql = @"
SELECT
    Id,
    GroupName,
    ParentId,
    Code,
    NameBg,
    SortOrder
FROM dbo.LookupValues
WHERE GroupName = @GroupName
  AND ParentId = @ParentId
  AND IsActive = 1
ORDER BY SortOrder, NameBg;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LookupItemResponse>(sql, new
        {
            GroupName = groupName,
            ParentId = parentId
        });
    }

    public async Task<IEnumerable<BrandResponse>> GetBrandsAsync(string? brandType)
    {
        const string sql = @"
SELECT
    Id,
    Name,
    BrandType
FROM dbo.Brands
WHERE IsActive = 1
  AND (@BrandType IS NULL OR BrandType = @BrandType)
ORDER BY Name;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<BrandResponse>(sql, new { BrandType = brandType });
    }

    public async Task<IEnumerable<ModelResponse>> GetModelsAsync(int? brandId, int? vehicleClassLookupId)
    {
        const string sql = @"
SELECT
    Id,
    BrandId,
    VehicleClassLookupId,
    Name
FROM dbo.Models
WHERE IsActive = 1
  AND (@BrandId IS NULL OR BrandId = @BrandId)
  AND (@VehicleClassLookupId IS NULL OR VehicleClassLookupId = @VehicleClassLookupId)
ORDER BY Name;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ModelResponse>(sql, new
        {
            BrandId = brandId,
            VehicleClassLookupId = vehicleClassLookupId
        });
    }

    public async Task<LookupItemResponse?> GetLookupByCodeAsync(string groupName, string code)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    GroupName,
    ParentId,
    Code,
    NameBg,
    SortOrder
FROM dbo.LookupValues
WHERE GroupName = @GroupName
  AND Code = @Code
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LookupItemResponse>(sql, new
        {
            GroupName = groupName,
            Code = code
        });
    }

    public async Task<LookupItemResponse?> GetLookupByIdAsync(int id)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    GroupName,
    ParentId,
    Code,
    NameBg,
    SortOrder
FROM dbo.LookupValues
WHERE Id = @Id
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LookupItemResponse>(sql, new { Id = id });
    }

    public async Task<CountryResponse?> GetCountryByIdAsync(int countryId)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    CountryCode,
    NameBg,
    NameEn,
    DefaultCurrencyCode,
    IsPrimaryMarket
FROM dbo.Countries
WHERE Id = @CountryId
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CountryResponse>(sql, new { CountryId = countryId });
    }

    public async Task<RegionResponse?> GetRegionByIdAsync(int regionId)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    CountryId,
    RegionCode,
    NameBg,
    SortOrder
FROM dbo.Regions
WHERE Id = @RegionId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RegionResponse>(sql, new { RegionId = regionId });
    }

    public async Task<CityResponse?> GetCityByIdAsync(int cityId)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    RegionId,
    NameBg,
    IsMajor,
    SortOrder
FROM dbo.Cities
WHERE Id = @CityId;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CityResponse>(sql, new { CityId = cityId });
    }

    public async Task<BrandResponse?> GetBrandByIdAsync(int brandId)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    Name,
    BrandType
FROM dbo.Brands
WHERE Id = @BrandId
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BrandResponse>(sql, new { BrandId = brandId });
    }

    public async Task<ModelResponse?> GetModelByIdAsync(int modelId)
    {
        const string sql = @"
SELECT TOP 1
    Id,
    BrandId,
    VehicleClassLookupId,
    Name
FROM dbo.Models
WHERE Id = @ModelId
  AND IsActive = 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ModelResponse>(sql, new { ModelId = modelId });
    }
}