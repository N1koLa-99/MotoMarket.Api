using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Repositories.Interfaces;

public interface ILookupRepository
{
    Task<IEnumerable<CountryResponse>> GetCountriesAsync();
    Task<IEnumerable<RegionResponse>> GetRegionsByCountryIdAsync(int countryId);
    Task<IEnumerable<CityResponse>> GetCitiesByRegionIdAsync(int regionId);

    Task<IEnumerable<LookupItemResponse>> GetLookupsByGroupAsync(string groupName);
    Task<IEnumerable<LookupItemResponse>> GetLookupsByGroupAndParentIdAsync(string groupName, int parentId);

    Task<IEnumerable<BrandResponse>> GetBrandsAsync(string? brandType);
    Task<IEnumerable<ModelResponse>> GetModelsAsync(int? brandId, int? vehicleClassLookupId);

    Task<LookupItemResponse?> GetLookupByCodeAsync(string groupName, string code);
    Task<LookupItemResponse?> GetLookupByIdAsync(int id);
    Task<CountryResponse?> GetCountryByIdAsync(int countryId);
    Task<RegionResponse?> GetRegionByIdAsync(int regionId);
    Task<CityResponse?> GetCityByIdAsync(int cityId);
    Task<BrandResponse?> GetBrandByIdAsync(int brandId);
    Task<ModelResponse?> GetModelByIdAsync(int modelId);
}