using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface ILookupService
{
    Task<IEnumerable<CountryResponse>> GetCountriesAsync();
    Task<IEnumerable<RegionResponse>> GetRegionsByCountryIdAsync(int countryId);
    Task<IEnumerable<CityResponse>> GetCitiesByRegionIdAsync(int regionId);

    Task<IEnumerable<LookupItemResponse>> GetMainCategoriesAsync();
    Task<IEnumerable<LookupItemResponse>> GetVehicleClassesAsync();
    Task<IEnumerable<LookupItemResponse>> GetVehicleTypesAsync(int? vehicleClassId);
    Task<IEnumerable<LookupItemResponse>> GetGearTypesAsync();
    Task<IEnumerable<LookupItemResponse>> GetHelmetTypesAsync(int? helmetParentId);
    Task<IEnumerable<LookupItemResponse>> GetPartTypesAsync();
    Task<IEnumerable<LookupItemResponse>> GetAccessoryTypesAsync();
    Task<IEnumerable<LookupItemResponse>> GetLicenseCategoriesAsync();
    Task<IEnumerable<LookupItemResponse>> GetConditionsAsync();

    Task<IEnumerable<BrandResponse>> GetBrandsAsync(string? brandType);
    Task<IEnumerable<ModelResponse>> GetModelsAsync(int? brandId, int? vehicleClassLookupId);
    Task<IEnumerable<YearOptionResponse>> GetYearsAsync(int startYear = 1950, int? endYear = null);

    Task<object> GetAllLookupDataAsync();
    Task<object> GetFilterConfigAsync();
}