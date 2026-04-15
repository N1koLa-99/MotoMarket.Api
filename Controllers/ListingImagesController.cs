using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/listing-images")]
public class ListingImagesController : ControllerBase
{
    private readonly IBlobImageService _blobImageService;

    public ListingImagesController(IBlobImageService blobImageService)
    {
        _blobImageService = blobImageService;
    }

    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _blobImageService.UploadAsync(userId, files);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                detail = ex.InnerException?.Message
            });
        }
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string blobName)
    {
        var userId = User.GetUserId();
        await _blobImageService.DeleteOwnedBlobAsync(userId, blobName);

        return Ok(new
        {
            success = true,
            message = "Снимката е изтрита успешно."
        });
    }
}