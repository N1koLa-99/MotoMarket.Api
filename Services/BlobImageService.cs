using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Responses;
using MotoMarket.Api.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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

        var totalBudgetBytes = (_options.TotalBudgetMb <= 0 ? 150 : _options.TotalBudgetMb) * 1024L * 1024L;
        var compressThresholdBytes = (_options.CompressIfLargerThanMb <= 0 ? 5 : _options.CompressIfLargerThanMb) * 1024L * 1024L;
        var initialQuality = _options.CompressQuality <= 0 ? 82 : _options.CompressQuality;

        var preparedFiles = new List<(string BlobName, string ContentType, MemoryStream Data, string OriginalName)>();

        try
        {
            // Стъпка 1: Подготви всички файлове (компресирай ако > праг)
            foreach (var file in files)
            {
                if (file == null || file.Length <= 0)
                    throw new InvalidOperationException("Има празен файл.");

                var safeFileName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"Непозволен файлов формат: {safeFileName}");

                MemoryStream ms;
                string contentType;
                string blobName;

                if (file.Length > compressThresholdBytes)
                {
                    ms = await CompressToWebpAsync(file, initialQuality);
                    contentType = "image/webp";
                    blobName = BuildOwnedBlobName(userId, ".webp");
                }
                else
                {
                    ms = new MemoryStream();
                    await using var inputStream = file.OpenReadStream();
                    await inputStream.CopyToAsync(ms);
                    ms.Position = 0;
                    contentType = NormalizeContentType(file.ContentType, extension);
                    blobName = BuildOwnedBlobName(userId, extension);
                }

                preparedFiles.Add((blobName, contentType, ms, safeFileName));
            }

            // Стъпка 2: Ако общият размер надвишава бюджета — рекомпресирай
            if (preparedFiles.Sum(x => x.Data.Length) > totalBudgetBytes)
                await RecompressToFitBudgetAsync(preparedFiles, totalBudgetBytes);

            // Стъпка 3: Качи всичко в Azure
            var result = new List<UploadedListingImageResponse>();

            foreach (var (blobName, contentType, data, originalName) in preparedFiles)
            {
                data.Position = 0;
                var blobClient = _containerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(data, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                });

                result.Add(new UploadedListingImageResponse
                {
                    FileName = originalName,
                    FileUrl = blobClient.Uri.ToString(),
                    BlobName = blobName,
                    ReadUrl = GetReadUrl(blobName),
                    ContentType = contentType,
                    Size = data.Length
                });
            }

            return result;
        }
        finally
        {
            foreach (var (_, _, data, _) in preparedFiles)
                await data.DisposeAsync();
        }
    }

    public async Task<string> UploadAvatarAsync(long userId, IFormFile file)
    {
        if (file == null || file.Length <= 0)
            throw new InvalidOperationException("Няма избран файл.");

        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException($"Непозволен файлов формат.");

        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        MemoryStream ms;
        string contentType;

        if (file.Length > 1 * 1024L * 1024L)
        {
            ms = await CompressToWebpAsync(file, 85);
            contentType = "image/webp";
        }
        else
        {
            ms = new MemoryStream();
            await using var inputStream = file.OpenReadStream();
            await inputStream.CopyToAsync(ms);
            ms.Position = 0;
            contentType = NormalizeContentType(file.ContentType, extension);
        }

        await using (ms)
        {
            var blobName = $"avatars/user-{userId}/{Guid.NewGuid():N}.webp";
            ms.Position = 0;
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(ms, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });
            return blobName;
        }
    }

    public async Task DeleteAvatarBlobAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return;

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    public string? ResolveAvatarUrl(string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return null;

        return GetReadUrl(blobName);
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

    // ───────────────────────────── Compression helpers ─────────────────────────────

    private static async Task<MemoryStream> CompressToWebpAsync(IFormFile file, int quality)
    {
        var ms = new MemoryStream();
        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);
        await image.SaveAsync(ms, new WebpEncoder
        {
            Quality = quality,
            Method = WebpEncodingMethod.Default
        });
        ms.Position = 0;
        return ms;
    }

    private static async Task RecompressToFitBudgetAsync(
        List<(string BlobName, string ContentType, MemoryStream Data, string OriginalName)> files,
        long budgetBytes)
    {
        // Само WebP файловете могат да се рекомпресират
        var compressibleIndexes = files
            .Select((f, i) => (f, i))
            .Where(x => x.f.ContentType == "image/webp")
            .Select(x => x.i)
            .ToList();

        if (compressibleIndexes.Count == 0)
            return;

        // Намалявай quality стъпка по стъпка
        for (var quality = 72; quality >= 10; quality -= 10)
        {
            foreach (var index in compressibleIndexes)
            {
                var current = files[index];
                var recompressed = await RecompressWebpAsync(current.Data, quality);
                await current.Data.DisposeAsync();
                files[index] = (current.BlobName, current.ContentType, recompressed, current.OriginalName);
            }

            if (files.Sum(x => x.Data.Length) <= budgetBytes)
                return;
        }

        // Последна мярка: намали резолюцията наполовина
        foreach (var index in compressibleIndexes)
        {
            var current = files[index];
            var halved = await HalveResolutionAsync(current.Data);
            await current.Data.DisposeAsync();
            files[index] = (current.BlobName, current.ContentType, halved, current.OriginalName);
        }
    }

    private static async Task<MemoryStream> RecompressWebpAsync(MemoryStream source, int quality)
    {
        source.Position = 0;
        using var image = await Image.LoadAsync(source);
        var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder
        {
            Quality = quality,
            Method = WebpEncodingMethod.Default
        });
        ms.Position = 0;
        return ms;
    }

    private static async Task<MemoryStream> HalveResolutionAsync(MemoryStream source)
    {
        source.Position = 0;
        using var image = await Image.LoadAsync(source);
        image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2));
        var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder { Quality = 10 });
        ms.Position = 0;
        return ms;
    }

    // ───────────────────────────── Private helpers ──────────────────────────────────

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