using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetDashboardAsync(userId);
        return Ok(result);
    }

    [HttpGet("my-listings")]
    public async Task<IActionResult> GetMyListings([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetMyListingsAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetMyFavoritesAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetMyPaymentsAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("favorites/{listingId:long}/check")]
    public async Task<IActionResult> CheckFavorite(long listingId)
    {
        var userId = User.GetUserId();
        var isFavorite = await _profileService.IsFavoriteAsync(userId, listingId);

        return Ok(new
        {
            listingId,
            isFavorite
        });
    }

    [HttpPost("favorites/{listingId:long}")]
    public async Task<IActionResult> AddFavorite(long listingId)
    {
        var userId = User.GetUserId();
        await _profileService.AddFavoriteAsync(userId, listingId);

        return Ok(new
        {
            success = true,
            message = "Обявата е добавена в favorites."
        });
    }

    [HttpDelete("favorites/{listingId:long}")]
    public async Task<IActionResult> RemoveFavorite(long listingId)
    {
        var userId = User.GetUserId();
        await _profileService.RemoveFavoriteAsync(userId, listingId);

        return Ok(new
        {
            success = true,
            message = "Обявата е премахната от favorites."
        });
    }
}
