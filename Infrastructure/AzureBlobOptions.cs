namespace MotoMarket.Api.Infrastructure;

public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = default!;
    public string ContainerName { get; set; } = default!;
    public int ReadSasMinutes { get; set; } = 60;
    public int TotalBudgetMb { get; set; } = 150;
    public int CompressIfLargerThanMb { get; set; } = 5;
    public int CompressQuality { get; set; } = 82;
}