namespace MotoMarket.Api.Infrastructure;

public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = default!;
    public string ContainerName { get; set; } = default!;
    public int ReadSasMinutes { get; set; } = 60;
    public int MaxFileSizeMb { get; set; } = 10;
}