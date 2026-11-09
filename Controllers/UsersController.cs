using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IProfileService _profileService;

    public UsersController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("{userId:long}")]
    public async Task<IActionResult> GetPublicProfile(long userId)
    {
        try
        {
            var result = await _profileService.GetPublicProfileAsync(userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Потребителят не е намерен." });
        }
    }

    [HttpGet("{userId:long}/listings")]
    public async Task<IActionResult> GetUserListings(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _profileService.GetPublicListingsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Потребителят не е намерен." });
        }
    }
}
