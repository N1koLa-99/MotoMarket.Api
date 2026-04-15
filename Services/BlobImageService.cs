using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class BlobImageService : IBlobImageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly AzureBlobOptions _options;
    private readonly BlobContainerClient _containerClient;

    public BlobImageService(IOptions<AzureBlobOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException("AzureBlob:ConnectionString is missing.");

        if (string.IsNullOrWhiteSpace(_options.ContainerName))
            throw new InvalidOperationException("AzureBlob:ContainerName is missing.");

        _containerClient = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
    }

    public async Task<List<UploadedListingImageResponse>> UploadAsync(long userId, IReadOnlyCollection<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            throw new InvalidOperationException("Няма избрани снимки.");

        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var result = new List<UploadedListingImageResponse>();

        foreach (var file in files)
        {
            ValidateFile(file);

            var safeFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
            var contentType = NormalizeContentType(file.ContentType, extension);

            var blobName = BuildOwnedBlobName(userId, extension);
            var blobClient = _containerClient.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            result.Add(new UploadedListingImageResponse
            {
                FileName = safeFileName,
                FileUrl = blobClient.Uri.ToString(),
                BlobName = blobName,
                ReadUrl = GetReadUrl(blobName),
                ContentType = contentType,
                Size = file.Length
            });
        }

        return result;
    }

    public async Task DeleteOwnedBlobAsync(long userId, string blobName)
    {
        ValidateOwnedPrefix(userId, blobName);

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task DeleteManyAsync(IEnumerable<string> blobNames)
    {
        if (blobNames == null)
            return;

        var uniqueBlobNames = blobNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var blobName in uniqueBlobNames)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }

    public async Task EnsureUserOwnsBlobsAsync(long userId, IEnumerable<string?> blobNames)
    {
        if (blobNames == null)
            return;

        var cleanNames = blobNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var blobName in cleanNames)
        {
            ValidateOwnedPrefix(userId, blobName);

            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync();

            if (!exists.Value)
                throw new InvalidOperationException($"Снимката не съществува: {blobName}");
        }
    }

    public string GetReadUrl(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new InvalidOperationException("Липсва blob name.");

        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Blob client cannot generate SAS URI. Check your Azure Blob connection string.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.ReadSasMinutes <= 0 ? 60 : _options.ReadSasMinutes)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public string GetBlobUrl(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new InvalidOperationException("Липсва blob name.");

        return _containerClient.GetBlobClient(blobName).Uri.ToString();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length <= 0)
            throw new InvalidOperationException("Има празен файл.");

        var maxBytes = (_options.MaxFileSizeMb <= 0 ? 10 : _options.MaxFileSizeMb) * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new InvalidOperationException($"Файлът {file.FileName} е по-голям от допустимото.");

        var safeFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Непозволен файлов формат: {safeFileName}");
    }

    private static string NormalizeContentType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
            return contentType;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string BuildOwnedBlobName(long userId, string extension)
    {
        var now = DateTime.UtcNow;
        return $"listings/user-{userId}/{now:yyyy/MM}/{Guid.NewGuid():N}{extension}";
    }

    private static void ValidateOwnedPrefix(long userId, string blobName)
    {
        var expectedPrefix = $"listings/user-{userId}/";

        if (!blobName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Нямаш право върху тази снимка.");
    }
}