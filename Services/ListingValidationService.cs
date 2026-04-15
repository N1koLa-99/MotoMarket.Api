using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Repositories.Interfaces;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class ListingValidationService : IListingValidationService
{
    private readonly ILookupRepository _lookupRepository;

    public ListingValidationService(ILookupRepository lookupRepository)
    {
        _lookupRepository = lookupRepository;
    }

    public Task ValidateCreateAsync(CreateListingRequest request)
        => ValidateCoreAsync(
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
            request.CurrencyCode,
            request.ExchangeRateToEUR,
            request.CountryId,
            request.RegionId,
            request.CityId,
            request.ContactName,
            request.ContactPhone);

    public Task ValidateUpdateAsync(UpdateListingRequest request)
        => ValidateCoreAsync(
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
            request.CurrencyCode,
            request.ExchangeRateToEUR,
            request.CountryId,
            request.RegionId,
            request.CityId,
            request.ContactName,
            request.ContactPhone);

    private async Task ValidateCoreAsync(
        int mainCategoryLookupId,
        int? subCategoryLookupId,
        int? subCategory2LookupId,
        int? brandId,
        int? modelId,
        string? itemModelText,
        int? licenseCategoryLookupId,
        int? conditionLookupId,
        string title,
        string? description,
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage,
        string? color,
        decimal priceOriginal,
        string currencyCode,
        decimal exchangeRateToEUR,
        int countryId,
        int? regionId,
        int? cityId,
        string? contactName,
        string contactPhone)
    {
        ValidateCommon(title, description, priceOriginal, currencyCode, exchangeRateToEUR, contactPhone, contactName, itemModelText, color);

        var mainCategory = await RequireLookup(mainCategoryLookupId, "MainCategoryLookupId");
        var country = await _lookupRepository.GetCountryByIdAsync(countryId)
            ?? throw new InvalidOperationException("Невалидна държава.");

        await ValidateLocationAsync(country, regionId, cityId);

        if (conditionLookupId.HasValue)
        {
            var condition = await RequireLookup(conditionLookupId.Value, "ConditionLookupId");
            if (!string.Equals(condition.GroupName, "Condition", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Невалиден condition lookup.");
        }

        switch (mainCategory.Code)
        {
            case "VEHICLE":
                await ValidateVehicleAsync(
                    subCategoryLookupId,
                    subCategory2LookupId,
                    brandId,
                    modelId,
                    licenseCategoryLookupId,
                    vehicleYear,
                    horsePower,
                    engineCC,
                    mileage);
                break;

            case "GEAR":
                await ValidateGearAsync(
                    subCategoryLookupId,
                    subCategory2LookupId,
                    brandId,
                    modelId,
                    licenseCategoryLookupId,
                    vehicleYear,
                    horsePower,
                    engineCC,
                    mileage,
                    color);
                break;

            case "PART":
                await ValidatePartAsync(
                    subCategoryLookupId,
                    subCategory2LookupId,
                    brandId,
                    modelId,
                    licenseCategoryLookupId,
                    vehicleYear,
                    horsePower,
                    engineCC,
                    mileage,
                    color);
                break;

            case "ACCESSORY":
                await ValidateAccessoryAsync(
                    subCategoryLookupId,
                    subCategory2LookupId,
                    brandId,
                    modelId,
                    licenseCategoryLookupId,
                    vehicleYear,
                    horsePower,
                    engineCC,
                    mileage,
                    color);
                break;

            default:
                throw new InvalidOperationException("Неподдържана main category.");
        }
    }

    private static void ValidateCommon(
        string title,
        string? description,
        decimal priceOriginal,
        string currencyCode,
        decimal exchangeRateToEUR,
        string contactPhone,
        string? contactName,
        string? itemModelText,
        string? color)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            throw new InvalidOperationException("Заглавието е задължително и трябва да е поне 3 символа.");

        if (description?.Length > 4000)
            throw new InvalidOperationException("Описанието е твърде дълго.");

        if (priceOriginal < 0)
            throw new InvalidOperationException("Цената не може да е отрицателна.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new InvalidOperationException("Валутата трябва да е 3 символа.");

        if (exchangeRateToEUR <= 0)
            throw new InvalidOperationException("ExchangeRateToEUR трябва да е по-голямо от 0.");

        if (string.IsNullOrWhiteSpace(contactPhone) || contactPhone.Trim().Length < 5)
            throw new InvalidOperationException("Телефонът е задължителен.");

        if (!string.IsNullOrWhiteSpace(contactName) && contactName.Trim().Length > 120)
            throw new InvalidOperationException("ContactName е твърде дълго.");

        if (!string.IsNullOrWhiteSpace(itemModelText) && itemModelText.Trim().Length > 100)
            throw new InvalidOperationException("ItemModelText е твърде дълго.");

        if (!string.IsNullOrWhiteSpace(color) && color.Trim().Length > 50)
            throw new InvalidOperationException("Color е твърде дълго.");
    }

    private async Task ValidateLocationAsync(
        Models.Responses.CountryResponse country,
        int? regionId,
        int? cityId)
    {
        var isBulgaria = string.Equals(country.CountryCode, "BG", StringComparison.OrdinalIgnoreCase);

        if (isBulgaria)
        {
            if (!regionId.HasValue)
                throw new InvalidOperationException("За България областта е задължителна.");

            var region = await _lookupRepository.GetRegionByIdAsync(regionId.Value)
                ?? throw new InvalidOperationException("Невалидна област.");

            if (region.CountryId != country.Id)
                throw new InvalidOperationException("Областта не принадлежи на избраната държава.");

            if (!cityId.HasValue)
                throw new InvalidOperationException("За България градът е задължителен.");

            var city = await _lookupRepository.GetCityByIdAsync(cityId.Value)
                ?? throw new InvalidOperationException("Невалиден град.");

            if (city.RegionId != region.Id)
                throw new InvalidOperationException("Градът не принадлежи на избраната област.");

            return;
        }

        if (regionId.HasValue || cityId.HasValue)
            throw new InvalidOperationException("За чужбина не трябва да има област и град.");
    }

    private async Task ValidateVehicleAsync(
        int? subCategoryLookupId,
        int? subCategory2LookupId,
        int? brandId,
        int? modelId,
        int? licenseCategoryLookupId,
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage)
    {
        if (!subCategoryLookupId.HasValue)
            throw new InvalidOperationException("За vehicle е задължителен vehicle class.");

        var vehicleClass = await RequireLookup(subCategoryLookupId.Value, "SubCategoryLookupId");
        if (!string.Equals(vehicleClass.GroupName, "VehicleClass", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SubCategoryLookupId трябва да е VehicleClass.");

        if (subCategory2LookupId.HasValue)
        {
            var vehicleType = await RequireLookup(subCategory2LookupId.Value, "SubCategory2LookupId");
            if (!string.Equals(vehicleType.GroupName, "VehicleType", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SubCategory2LookupId трябва да е VehicleType.");

            if (vehicleType.ParentId != vehicleClass.Id)
                throw new InvalidOperationException("Vehicle type не принадлежи на избрания vehicle class.");
        }

        if (!brandId.HasValue)
            throw new InvalidOperationException("За vehicle марката е задължителна.");

        var brand = await _lookupRepository.GetBrandByIdAsync(brandId.Value)
            ?? throw new InvalidOperationException("Невалидна марка.");

        if (!string.Equals(brand.BrandType, "VEHICLE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Марката не е vehicle brand.");

        if (modelId.HasValue)
        {
            var model = await _lookupRepository.GetModelByIdAsync(modelId.Value)
                ?? throw new InvalidOperationException("Невалиден модел.");

            if (model.BrandId != brand.Id)
                throw new InvalidOperationException("Моделът не принадлежи на избраната марка.");

            if (model.VehicleClassLookupId != vehicleClass.Id)
                throw new InvalidOperationException("Моделът не принадлежи на избрания vehicle class.");
        }

        if (licenseCategoryLookupId.HasValue)
        {
            var licenseCategory = await RequireLookup(licenseCategoryLookupId.Value, "LicenseCategoryLookupId");
            if (!string.Equals(licenseCategory.GroupName, "LicenseCategory", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Невалидна license category.");
        }

        ValidateVehicleNumbers(vehicleYear, horsePower, engineCC, mileage);
    }

    private async Task ValidateGearAsync(
        int? subCategoryLookupId,
        int? subCategory2LookupId,
        int? brandId,
        int? modelId,
        int? licenseCategoryLookupId,
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage,
        string? color)
    {
        if (!subCategoryLookupId.HasValue)
            throw new InvalidOperationException("За gear е задължителен gear type.");

        var gearType = await RequireLookup(subCategoryLookupId.Value, "SubCategoryLookupId");
        if (!string.Equals(gearType.GroupName, "GearType", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SubCategoryLookupId трябва да е GearType.");

        if (brandId.HasValue)
        {
            var brand = await _lookupRepository.GetBrandByIdAsync(brandId.Value)
                ?? throw new InvalidOperationException("Невалидна марка.");

            if (!string.Equals(brand.BrandType, "GEAR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Марката не е gear brand.");
        }

        if (modelId.HasValue)
            throw new InvalidOperationException("За gear не се използва ModelId.");

        if (licenseCategoryLookupId.HasValue)
            throw new InvalidOperationException("За gear не се използва LicenseCategoryLookupId.");

        if (horsePower.HasValue || engineCC.HasValue || mileage.HasValue)
            throw new InvalidOperationException("За gear не се използват vehicle числовите полета.");

        if (subCategory2LookupId.HasValue)
        {
            var gearTypeIsHelmet = string.Equals(gearType.Code, "HELMET", StringComparison.OrdinalIgnoreCase);
            if (!gearTypeIsHelmet)
                throw new InvalidOperationException("SubCategory2LookupId е позволено само при helmet.");

            var helmetType = await RequireLookup(subCategory2LookupId.Value, "SubCategory2LookupId");
            if (!string.Equals(helmetType.GroupName, "HelmetType", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SubCategory2LookupId трябва да е HelmetType.");

            if (helmetType.ParentId != gearType.Id)
                throw new InvalidOperationException("Helmet type не принадлежи на избрания gear type.");
        }

        if (vehicleYear.HasValue && (vehicleYear < 1900 || vehicleYear > DateTime.UtcNow.Year + 1))
            throw new InvalidOperationException("Невалидна година.");

        if (!string.IsNullOrWhiteSpace(color) && color.Trim().Length > 50)
            throw new InvalidOperationException("Невалиден цвят.");
    }

    private async Task ValidatePartAsync(
        int? subCategoryLookupId,
        int? subCategory2LookupId,
        int? brandId,
        int? modelId,
        int? licenseCategoryLookupId,
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage,
        string? color)
    {
        if (!subCategoryLookupId.HasValue)
            throw new InvalidOperationException("За part е задължителен part type.");

        var partType = await RequireLookup(subCategoryLookupId.Value, "SubCategoryLookupId");
        if (!string.Equals(partType.GroupName, "PartType", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SubCategoryLookupId трябва да е PartType.");

        if (subCategory2LookupId.HasValue)
            throw new InvalidOperationException("За part не се използва SubCategory2LookupId.");

        if (brandId.HasValue)
        {
            var brand = await _lookupRepository.GetBrandByIdAsync(brandId.Value)
                ?? throw new InvalidOperationException("Невалидна марка.");

            if (!(string.Equals(brand.BrandType, "PART", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(brand.BrandType, "GENERAL", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Марката не е подходяща за part.");
            }
        }

        if (modelId.HasValue)
            throw new InvalidOperationException("За part не се използва ModelId.");

        if (licenseCategoryLookupId.HasValue)
            throw new InvalidOperationException("За part не се използва LicenseCategoryLookupId.");

        if (horsePower.HasValue || engineCC.HasValue || mileage.HasValue)
            throw new InvalidOperationException("За part не се използват vehicle числовите полета.");

        if (vehicleYear.HasValue && (vehicleYear < 1900 || vehicleYear > DateTime.UtcNow.Year + 1))
            throw new InvalidOperationException("Невалидна година.");

        if (!string.IsNullOrWhiteSpace(color) && color.Trim().Length > 50)
            throw new InvalidOperationException("Невалиден цвят.");
    }

    private async Task ValidateAccessoryAsync(
        int? subCategoryLookupId,
        int? subCategory2LookupId,
        int? brandId,
        int? modelId,
        int? licenseCategoryLookupId,
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage,
        string? color)
    {
        if (!subCategoryLookupId.HasValue)
            throw new InvalidOperationException("За accessory е задължителен accessory type.");

        var accessoryType = await RequireLookup(subCategoryLookupId.Value, "SubCategoryLookupId");
        if (!string.Equals(accessoryType.GroupName, "AccessoryType", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SubCategoryLookupId трябва да е AccessoryType.");

        if (subCategory2LookupId.HasValue)
            throw new InvalidOperationException("За accessory не се използва SubCategory2LookupId.");

        if (brandId.HasValue)
        {
            var brand = await _lookupRepository.GetBrandByIdAsync(brandId.Value)
                ?? throw new InvalidOperationException("Невалидна марка.");

            if (!(string.Equals(brand.BrandType, "ACCESSORY", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(brand.BrandType, "GENERAL", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Марката не е подходяща за accessory.");
            }
        }

        if (modelId.HasValue)
            throw new InvalidOperationException("За accessory не се използва ModelId.");

        if (licenseCategoryLookupId.HasValue)
            throw new InvalidOperationException("За accessory не се използва LicenseCategoryLookupId.");

        if (horsePower.HasValue || engineCC.HasValue || mileage.HasValue)
            throw new InvalidOperationException("За accessory не се използват vehicle числовите полета.");

        if (vehicleYear.HasValue && (vehicleYear < 1900 || vehicleYear > DateTime.UtcNow.Year + 1))
            throw new InvalidOperationException("Невалидна година.");

        if (!string.IsNullOrWhiteSpace(color) && color.Trim().Length > 50)
            throw new InvalidOperationException("Невалиден цвят.");
    }

    private static void ValidateVehicleNumbers(
        short? vehicleYear,
        int? horsePower,
        int? engineCC,
        int? mileage)
    {
        if (vehicleYear.HasValue && (vehicleYear < 1900 || vehicleYear > DateTime.UtcNow.Year + 1))
            throw new InvalidOperationException("Невалидна година.");

        if (horsePower.HasValue && (horsePower <= 0 || horsePower > 5000))
            throw new InvalidOperationException("Невалидни конски сили.");

        if (engineCC.HasValue && (engineCC <= 0 || engineCC > 15000))
            throw new InvalidOperationException("Невалидни кубици.");

        if (mileage.HasValue && mileage < 0)
            throw new InvalidOperationException("Пробегът не може да е отрицателен.");
    }

    private async Task<Models.Responses.LookupItemResponse> RequireLookup(int id, string fieldName)
    {
        var lookup = await _lookupRepository.GetLookupByIdAsync(id);
        if (lookup == null)
            throw new InvalidOperationException($"{fieldName} е невалидно.");

        return lookup;
    }
}