using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Services.Interfaces;
using System.Security.Claims;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly IListingPresentationService _listingPresentationService;

    public ListingsController(
        IListingService listingService,
        IListingPresentationService listingPresentationService)
    {
        _listingService = listingService;
        _listingPresentationService = listingPresentationService;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<IActionResult> SearchPublic([FromQuery] PublicListingSearchRequest request)
    {
        var viewerUserId = TryGetCurrentUserId();
        var result = await _listingPresentationService.SearchPublicAsync(request, viewerUserId);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("public/{listingId:long}")]
    public async Task<IActionResult> GetPublicById(long listingId, [FromQuery] bool incrementView = true)
    {
        var viewerUserId = TryGetCurrentUserId();
        var result = await _listingPresentationService.GetPublicByIdAsync(listingId, viewerUserId, incrementView);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request)
    {
        var userId = User.GetUserId();
        var result = await _listingService.CreateAsync(userId, request);
        return Ok(result);
    }

    [Authorize]
    [HttpPut("{listingId:long}")]
    public async Task<IActionResult> Update(long listingId, [FromBody] UpdateListingRequest request)
    {
        var userId = User.GetUserId();
        var result = await _listingService.UpdateAsync(userId, listingId, request);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{listingId:long}")]
    public async Task<IActionResult> Delete(long listingId)
    {
        var userId = User.GetUserId();
        var result = await _listingService.DeleteAsync(userId, listingId);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{listingId:long}/refresh")]
    public async Task<IActionResult> Refresh(long listingId)
    {
        var userId = User.GetUserId();
        var result = await _listingService.RefreshAsync(userId, listingId);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{listingId:long}/promote")]
    public async Task<IActionResult> Promote(long listingId, [FromBody] PromoteListingRequest request)
    {
        var userId = User.GetUserId();
        var result = await _listingService.PromoteAsync(userId, listingId, request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{listingId:long}")]
    public async Task<IActionResult> GetById(long listingId)
    {
        var result = await _listingService.GetByIdAsync(listingId);
        return Ok(result);
    }

    private long? TryGetCurrentUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return null;

        var claimValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId");

        return long.TryParse(claimValue, out var userId) ? userId : null;
    }
}