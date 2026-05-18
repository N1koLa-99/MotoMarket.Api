using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Services.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

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
    [AllowAnonymous]
    [HttpGet("{listingId:long}/og")]
    public async Task<IActionResult> GetOpenGraph(long listingId)
    {
        try
        {
            var listing = await _listingPresentationService.GetPublicByIdAsync(
                listingId,
                viewerUserId: null,
                incrementViewCount: false);

            var mainPhoto = listing.Photos?
                .FirstOrDefault(p => p.IsMain)?.FileUrl
                ?? listing.Photos?.FirstOrDefault()?.FileUrl
                ?? "";

            var title = listing.Title ?? "Обява";
            var price = listing.PriceOriginal > 0
                ? $"{listing.PriceOriginal:N0} {listing.CurrencyCode}"
                : "Цена при запитване";
            var description = $"{price} · {listing.VehicleYear} · {listing.Mileage?.ToString("N0")} км";
            var url = $"https://moto-zona.com/ListingDetails.html?id={listingId}";

            var html = $"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <title>{HtmlEncoder.Default.Encode(title)}</title>
              <meta property="og:type" content="website" />
              <meta property="og:url" content="{url}" />
              <meta property="og:title" content="{HtmlEncoder.Default.Encode(title)}" />
              <meta property="og:description" content="{HtmlEncoder.Default.Encode(description)}" />
              <meta property="og:image" content="{mainPhoto}" />
              <meta property="og:image:width" content="1200" />
              <meta property="og:image:height" content="630" />
              <meta property="og:site_name" content="Мото Зона" />
              <meta name="twitter:card" content="summary_large_image" />
              <meta name="twitter:title" content="{HtmlEncoder.Default.Encode(title)}" />
              <meta name="twitter:description" content="{HtmlEncoder.Default.Encode(description)}" />
              <meta name="twitter:image" content="{mainPhoto}" />
            </head>
            <body>
              <script>window.location.href = "{url}";</script>
            </body>
            </html>
            """;

            return Content(html, "text/html");
        }
        catch
        {
            return NotFound();
        }
    }
}