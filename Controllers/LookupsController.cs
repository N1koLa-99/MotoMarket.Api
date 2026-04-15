using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var result = await _lookupService.GetAllLookupDataAsync();
        return Ok(result);
    }

    [HttpGet("filter-config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFilterConfig()
    {
        var result = await _lookupService.GetFilterConfigAsync();
        return Ok(result);
    }

    [HttpGet("countries")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCountries()
    {
        var result = await _lookupService.GetCountriesAsync();
        return Ok(result);
    }

    [HttpGet("regions/{countryId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRegions(int countryId)
    {
        var result = await _lookupService.GetRegionsByCountryIdAsync(countryId);
        return Ok(result);
    }

    [HttpGet("cities/{regionId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCities(int regionId)
    {
        var result = await _lookupService.GetCitiesByRegionIdAsync(regionId);
        return Ok(result);
    }

    [HttpGet("main-categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMainCategories()
    {
        var result = await _lookupService.GetMainCategoriesAsync();
        return Ok(result);
    }

    [HttpGet("vehicle-classes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVehicleClasses()
    {
        var result = await _lookupService.GetVehicleClassesAsync();
        return Ok(result);
    }

    [HttpGet("vehicle-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVehicleTypes([FromQuery] int? vehicleClassId)
    {
        var result = await _lookupService.GetVehicleTypesAsync(vehicleClassId);
        return Ok(result);
    }

    [HttpGet("gear-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGearTypes()
    {
        var result = await _lookupService.GetGearTypesAsync();
        return Ok(result);
    }

    [HttpGet("helmet-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHelmetTypes([FromQuery] int? helmetParentId)
    {
        var result = await _lookupService.GetHelmetTypesAsync(helmetParentId);
        return Ok(result);
    }

    [HttpGet("part-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPartTypes()
    {
        var result = await _lookupService.GetPartTypesAsync();
        return Ok(result);
    }

    [HttpGet("accessory-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAccessoryTypes()
    {
        var result = await _lookupService.GetAccessoryTypesAsync();
        return Ok(result);
    }

    [HttpGet("license-categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLicenseCategories()
    {
        var result = await _lookupService.GetLicenseCategoriesAsync();
        return Ok(result);
    }

    [HttpGet("conditions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConditions()
    {
        var result = await _lookupService.GetConditionsAsync();
        return Ok(result);
    }

    [HttpGet("brands")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands([FromQuery] string? brandType)
    {
        var result = await _lookupService.GetBrandsAsync(brandType);
        return Ok(result);
    }

    [HttpGet("models")]
    [AllowAnonymous]
    public async Task<IActionResult> GetModels([FromQuery] int? brandId, [FromQuery] int? vehicleClassLookupId)
    {
        var result = await _lookupService.GetModelsAsync(brandId, vehicleClassLookupId);
        return Ok(result);
    }

    [HttpGet("years")]
    [AllowAnonymous]
    public async Task<IActionResult> GetYears([FromQuery] int startYear = 1950, [FromQuery] int? endYear = null)
    {
        var result = await _lookupService.GetYearsAsync(startYear, endYear);
        return Ok(result);
    }
}