namespace MotoMarket.Api.Models.Entities;

public class ListingPhoto
{
    public long Id { get; set; }
    public long ListingId { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string? BlobName { get; set; }
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}