using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class LookupService : ILookupService
{
    private readonly ILookupRepository _lookupRepository;

    public LookupService(ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;
    }

    public Task<IEnumerable<CountryResponse>> GetCountriesAsync()
        => _lookupRepository.GetCountriesAsync();

    public Task<IEnumerable<RegionResponse>> GetRegionsByCountryIdAsync(int countryId)
    {
        if (countryId <= 0)
            throw new InvalidOperationException("Невалиден countryId.");

        return _lookupRepository.GetRegionsByCountryIdAsync(countryId);
    }

    public Task<IEnumerable<CityResponse>> GetCitiesByRegionIdAsync(int regionId)
    {
        if (regionId <= 0)
            throw new InvalidOperationException("Невалиден regionId.");

        return _lookupRepository.GetCitiesByRegionIdAsync(regionId);
    }

    public Task<IEnumerable<LookupItemResponse>> GetMainCategoriesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("MainCategory");

    public Task<IEnumerable<LookupItemResponse>> GetVehicleClassesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("VehicleClass");

    public async Task<IEnumerable<LookupItemResponse>> GetVehicleTypesAsync(int? vehicleClassId)
    {
        if (vehicleClassId.HasValue && vehicleClassId.Value > 0)
        {
            return await _lookupRepository.GetLookupsByGroupAndParentIdAsync("VehicleType", vehicleClassId.Value);
        }

        return await _lookupRepository.GetLookupsByGroupAsync("VehicleType");
    }

    public Task<IEnumerable<LookupItemResponse>> GetGearTypesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("GearType");

    public async Task<IEnumerable<LookupItemResponse>> GetHelmetTypesAsync(int? helmetParentId)
    {
        if (helmetParentId.HasValue && helmetParentId.Value > 0)
        {
            return await _lookupRepository.GetLookupsByGroupAndParentIdAsync("HelmetType", helmetParentId.Value);
        }

        return await _lookupRepository.GetLookupsByGroupAsync("HelmetType");
    }

    public Task<IEnumerable<LookupItemResponse>> GetPartTypesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("PartType");

    public Task<IEnumerable<LookupItemResponse>> GetAccessoryTypesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("AccessoryType");

    public Task<IEnumerable<LookupItemResponse>> GetLicenseCategoriesAsync()
        => _lookupRepository.GetLookupsByGroupAsync("LicenseCategory");

    public Task<IEnumerable<LookupItemResponse>> GetConditionsAsync()
        => _lookupRepository.GetLookupsByGroupAsync("Condition");

    public Task<IEnumerable<BrandResponse>> GetBrandsAsync(string? brandType)
    {
        string? normalizedBrandType = null;

        if (!string.IsNullOrWhiteSpace(brandType))
        {
            normalizedBrandType = brandType.Trim().ToUpperInvariant();

            var allowed = new[] { "VEHICLE", "GEAR", "ACCESSORY", "PART", "GENERAL" };
            if (!allowed.Contains(normalizedBrandType))
                throw new InvalidOperationException("Невалиден brandType.");
        }

        return _lookupRepository.GetBrandsAsync(normalizedBrandType);
    }

    public Task<IEnumerable<ModelResponse>> GetModelsAsync(int? brandId, int? vehicleClassLookupId)
    {
        if (brandId.HasValue && brandId.Value <= 0)
            throw new InvalidOperationException("Невалиден brandId.");

        if (vehicleClassLookupId.HasValue && vehicleClassLookupId.Value <= 0)
            throw new InvalidOperationException("Невалиден vehicleClassLookupId.");

        return _lookupRepository.GetModelsAsync(brandId, vehicleClassLookupId);
    }

    public Task<IEnumerable<YearOptionResponse>> GetYearsAsync(int startYear = 1950, int? endYear = null)
    {
        var currentYear = DateTime.UtcNow.Year;
        var finalEndYear = endYear ?? currentYear + 1;

        if (startYear < 1900 || startYear > finalEndYear)
            throw new InvalidOperationException("Невалиден диапазон за години.");

        var result = Enumerable
            .Range(startYear, finalEndYear - startYear + 1)
            .Reverse()
            .Select(year => new YearOptionResponse
            {
                Value = year,
                Label = year.ToString()
            });

        return Task.FromResult(result);
    }

    public async Task<object> GetAllLookupDataAsync()
    {
        var mainCategories = await GetMainCategoriesAsync();
        var vehicleClasses = await GetVehicleClassesAsync();
        var vehicleTypes = await _lookupRepository.GetLookupsByGroupAsync("VehicleType");
        var gearTypes = await GetGearTypesAsync();
        var helmetTypes = await _lookupRepository.GetLookupsByGroupAsync("HelmetType");
        var partTypes = await GetPartTypesAsync();
        var accessoryTypes = await GetAccessoryTypesAsync();
        var licenseCategories = await GetLicenseCategoriesAsync();
        var conditions = await GetConditionsAsync();

        var countries = await GetCountriesAsync();
        var vehicleBrands = await GetBrandsAsync("VEHICLE");
        var gearBrands = await GetBrandsAsync("GEAR");
        var accessoryBrands = await GetBrandsAsync("ACCESSORY");
        var partBrands = await GetBrandsAsync("PART");
        var years = await GetYearsAsync();

        return new
        {
            mainCategories,
            vehicleClasses,
            vehicleTypes,
            gearTypes,
            helmetTypes,
            partTypes,
            accessoryTypes,
            licenseCategories,
            conditions,
            countries,
            vehicleBrands,
            gearBrands,
            accessoryBrands,
            partBrands,
            years
        };
    }

    public async Task<object> GetFilterConfigAsync()
    {
        var mainCategories = (await GetMainCategoriesAsync()).ToList();
        var vehicleClasses = (await GetVehicleClassesAsync()).ToList();
        var gearTypes = (await GetGearTypesAsync()).ToList();
        var partTypes = (await GetPartTypesAsync()).ToList();
        var accessoryTypes = (await GetAccessoryTypesAsync()).ToList();
        var licenseCategories = (await GetLicenseCategoriesAsync()).ToList();
        var conditions = (await GetConditionsAsync()).ToList();

        var helmetGearType = gearTypes.FirstOrDefault(x => x.Code == "HELMET");
        var helmetTypes = helmetGearType != null
            ? (await GetHelmetTypesAsync(helmetGearType.Id)).ToList()
            : new List<LookupItemResponse>();

        return new
        {
            categories = new
            {
                main = mainCategories,
                vehicleClasses,
                gearTypes,
                partTypes,
                accessoryTypes,
                licenseCategories,
                conditions,
                helmetTypes
            },

            filters = new
            {
                vehicle = new
                {
                    requiredFields = new[]
                    {
                        "mainCategoryLookupId",
                        "subCategoryLookupId",
                        "brandId",
                        "title",
                        "priceOriginal",
                        "currencyCode",
                        "countryId",
                        "contactPhone"
                    },
                    optionalFields = new[]
                    {
                        "modelId",
                        "subCategory2LookupId",
                        "licenseCategoryLookupId",
                        "conditionLookupId",
                        "vehicleYear",
                        "horsePower",
                        "engineCC",
                        "mileage",
                        "color",
                        "description",
                        "regionId",
                        "cityId",
                        "contactName"
                    },
                    ranges = new[]
                    {
                        "priceFrom",
                        "priceTo",
                        "yearFrom",
                        "yearTo",
                        "horsePowerFrom",
                        "horsePowerTo",
                        "engineCcFrom",
                        "engineCcTo",
                        "mileageFrom",
                        "mileageTo"
                    },
                    lookupDependencies = new
                    {
                        mainCategory = "MainCategory: VEHICLE",
                        subCategory = "VehicleClass",
                        subCategory2 = "VehicleType by VehicleClass",
                        brand = "Brands by VEHICLE",
                        model = "Models by Brand + VehicleClass",
                        licenseCategory = "LicenseCategory",
                        condition = "Condition"
                    }
                },

                gear = new
                {
                    requiredFields = new[]
                    {
                        "mainCategoryLookupId",
                        "subCategoryLookupId",
                        "brandId",
                        "title",
                        "priceOriginal",
                        "currencyCode",
                        "countryId",
                        "contactPhone"
                    },
                    optionalFields = new[]
                    {
                        "subCategory2LookupId",
                        "itemModelText",
                        "conditionLookupId",
                        "vehicleYear",
                        "description",
                        "regionId",
                        "cityId",
                        "contactName"
                    },
                    ranges = new[]
                    {
                        "priceFrom",
                        "priceTo",
                        "yearFrom",
                        "yearTo"
                    },
                    lookupDependencies = new
                    {
                        mainCategory = "MainCategory: GEAR",
                        subCategory = "GearType",
                        subCategory2 = "HelmetType only when GearType = HELMET",
                        brand = "Brands by GEAR",
                        condition = "Condition"
                    }
                },

                part = new
                {
                    requiredFields = new[]
                    {
                        "mainCategoryLookupId",
                        "subCategoryLookupId",
                        "title",
                        "priceOriginal",
                        "currencyCode",
                        "countryId",
                        "contactPhone"
                    },
                    optionalFields = new[]
                    {
                        "brandId",
                        "itemModelText",
                        "conditionLookupId",
                        "description",
                        "regionId",
                        "cityId",
                        "contactName"
                    },
                    ranges = new[]
                    {
                        "priceFrom",
                        "priceTo"
                    },
                    lookupDependencies = new
                    {
                        mainCategory = "MainCategory: PART",
                        subCategory = "PartType",
                        brand = "Brands by PART",
                        condition = "Condition"
                    }
                },

                accessory = new
                {
                    requiredFields = new[]
                    {
                        "mainCategoryLookupId",
                        "subCategoryLookupId",
                        "title",
                        "priceOriginal",
                        "currencyCode",
                        "countryId",
                        "contactPhone"
                    },
                    optionalFields = new[]
                    {
                        "brandId",
                        "itemModelText",
                        "conditionLookupId",
                        "description",
                        "regionId",
                        "cityId",
                        "contactName"
                    },
                    ranges = new[]
                    {
                        "priceFrom",
                        "priceTo"
                    },
                    lookupDependencies = new
                    {
                        mainCategory = "MainCategory: ACCESSORY",
                        subCategory = "AccessoryType",
                        brand = "Brands by ACCESSORY",
                        condition = "Condition"
                    }
                }
            }
        };
    }
}