using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetUsersAsync(searchTerm, page, pageSize);
        return Ok(result);
    }

    [HttpGet("users/{userId:long}")]
    public async Task<IActionResult> GetUserDetails(long userId)
    {
        var result = await _adminService.GetUserDetailsAsync(userId);
        return Ok(result);
    }

    [HttpGet("users/{userId:long}/listings")]
    public async Task<IActionResult> GetUserListings(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetUserListingsAsync(userId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetPaymentsAsync(searchTerm, page, pageSize);
        return Ok(result);
    }

    [HttpGet("pending-actions")]
    public async Task<IActionResult> GetPendingActions([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminService.GetPendingActionsAsync(status, page, pageSize);
        return Ok(result);
    }
}
