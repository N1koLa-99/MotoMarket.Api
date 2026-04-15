namespace MotoMarket.Api.Models.Responses;

public class UploadedListingImageResponse
{
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string ReadUrl { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long Size { get; set; }
}