using Microsoft.AspNetCore.Http;
using MotoMarket.Api.Models.Responses;

namespace MotoMarket.Api.Services.Interfaces;

public interface IBlobImageService
{
    Task<List<UploadedListingImageResponse>> UploadAsync(long userId, IReadOnlyCollection<IFormFile> files);
    Task<string> UploadAvatarAsync(long userId, IFormFile file);
    Task DeleteAvatarBlobAsync(string blobName);
    Task DeleteOwnedBlobAsync(long userId, string blobName);
    Task DeleteManyAsync(IEnumerable<string> blobNames);
    Task EnsureUserOwnsBlobsAsync(long userId, IEnumerable<string?> blobNames);
    string GetReadUrl(string blobName);
    string GetBlobUrl(string blobName);
    string? ResolveAvatarUrl(string? logoUrl);
}