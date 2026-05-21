using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Services.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listingService;
    private readonly IListingPresentationService _listingPresentationService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ListingsController(
        IListingService listingService,
        IListingPresentationService listingPresentationService,
        IHttpClientFactory httpClientFactory)
    {
        _listingService = listingService;
        _listingPresentationService = listingPresentationService;
        _httpClientFactory = httpClientFactory;
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

    [AllowAnonymous]
    [HttpGet("{listingId:long}/og-image")]
    public async Task<IActionResult> GetOpenGraphImage(long listingId)
    {
        const string fallbackImageUrl = "https://moto-zona.com/ImagesVideos/LogoMotoZonaNew.png";

        try
        {
            var listing = await _listingPresentationService.GetPublicByIdAsync(listingId, null, false);

            if (listing is null)
            {
                return Redirect(fallbackImageUrl);
            }

            var mainPhotoUrl = GetMainPhotoUrl(listing);

            if (string.IsNullOrWhiteSpace(mainPhotoUrl))
            {
                return Redirect(fallbackImageUrl);
            }

            var client = _httpClientFactory.CreateClient();

            using var response = await client.GetAsync(mainPhotoUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return Redirect(fallbackImageUrl);
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            Response.Headers["Cache-Control"] = "public,max-age=3600";

            return File(imageBytes, contentType);
        }
        catch
        {
            return Redirect(fallbackImageUrl);
        }
    }

    [AllowAnonymous]
    [HttpGet("{listingId:long}/og")]
    public async Task<IActionResult> GetOpenGraph(long listingId)
    {
        try
        {
            var listing = await _listingPresentationService.GetPublicByIdAsync(listingId, null, false);

            if (listing is null)
            {
                return NotFound();
            }

            const string siteUrl = "https://moto-zona.com";
            const string apiUrl = "https://motomarketapi.azurewebsites.net";

            var redirectUrl = $"{siteUrl}/ListingDetails.html?id={listingId}";
            var shareUrl = redirectUrl;
            var imageUrl = $"{apiUrl}/api/listings/{listingId}/og-image";

            var title = string.IsNullOrWhiteSpace(listing.Title)
                ? "Обява"
                : listing.Title.Trim();

            var currencyCode = string.IsNullOrWhiteSpace(listing.CurrencyCode)
                ? "EUR"
                : listing.CurrencyCode.Trim().ToUpperInvariant();

            var priceText = listing.PriceOriginal > 0
                ? $"{listing.PriceOriginal:N0} {currencyCode}"
                : "Цена при запитване";

            var descriptionParts = new List<string>
        {
            priceText
        };

            if (listing.VehicleYear > 0)
            {
                descriptionParts.Add(listing.VehicleYear.ToString());
            }

            if (listing.Mileage.HasValue && listing.Mileage.Value > 0)
            {
                descriptionParts.Add($"{listing.Mileage.Value:N0} км");
            }

            var description = string.Join(" · ", descriptionParts);

            var encodedTitle = HtmlEncoder.Default.Encode(title);
            var encodedDescription = HtmlEncoder.Default.Encode(description);
            var encodedShareUrl = HtmlEncoder.Default.Encode(shareUrl);
            var encodedRedirectUrl = HtmlEncoder.Default.Encode(redirectUrl);
            var encodedImageUrl = HtmlEncoder.Default.Encode(imageUrl);
            var jsRedirectUrl = JavaScriptEncoder.Default.Encode(redirectUrl);

            var html = $"""
        <!DOCTYPE html>
        <html lang="bg">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>{encodedTitle}</title>
          <meta name="description" content="{encodedDescription}" />
          <meta name="robots" content="noindex,nofollow" />
          <link rel="canonical" href="{encodedRedirectUrl}" />
          <meta http-equiv="refresh" content="0;url={encodedRedirectUrl}" />

          <meta property="og:locale" content="bg_BG" />
          <meta property="og:type" content="product" />
          <meta property="og:site_name" content="Мото Зона" />
          <meta property="og:url" content="{encodedShareUrl}" />
          <meta property="og:title" content="{encodedTitle}" />
          <meta property="og:description" content="{encodedDescription}" />
          <meta property="og:image" content="{encodedImageUrl}" />
          <meta property="og:image:secure_url" content="{encodedImageUrl}" />
          <meta property="og:image:width" content="1200" />
          <meta property="og:image:height" content="630" />
          <meta property="og:image:alt" content="{encodedTitle}" />

          <meta name="twitter:card" content="summary_large_image" />
          <meta name="twitter:title" content="{encodedTitle}" />
          <meta name="twitter:description" content="{encodedDescription}" />
          <meta name="twitter:image" content="{encodedImageUrl}" />
          <meta name="twitter:url" content="{encodedShareUrl}" />
        </head>
        <body>
          <noscript>
            <a href="{encodedRedirectUrl}">Продължи към обявата</a>
          </noscript>

          <script>
            window.location.replace("{jsRedirectUrl}");
          </script>
        </body>
        </html>
        """;

            return Content(html, "text/html; charset=utf-8");
        }
        catch
        {
            return NotFound();
        }
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

    private static string? GetMainPhotoUrl(dynamic listing)
    {
        if (listing?.Photos is null)
        {
            return null;
        }

        foreach (var photo in listing.Photos)
        {
            if (photo is not null && photo.IsMain && !string.IsNullOrWhiteSpace(photo.FileUrl))
            {
                return photo.FileUrl;
            }
        }

        foreach (var photo in listing.Photos)
        {
            if (photo is not null && !string.IsNullOrWhiteSpace(photo.FileUrl))
            {
                return photo.FileUrl;
            }
        }

        return null;
    }
}