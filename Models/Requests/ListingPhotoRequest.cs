using System.ComponentModel.DataAnnotations;

namespace MotoMarket.Api.Models.Requests;

public class ListingPhotoRequest
{
    [Required, MaxLength(255)]
    public string FileName { get; set; } = default!;

    [Required, MaxLength(500)]
    public string FileUrl { get; set; } = default!;

    [MaxLength(500)]
    public string? BlobName { get; set; }

    [Range(0, 1000)]
    public int SortOrder { get; set; }

    public bool IsMain { get; set; }
}