namespace MotoMarket.Api.Models.Responses;

public class ListingOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;

    public long? ListingId { get; set; }

    public bool RequiresPayment { get; set; }
    public long? PendingActionId { get; set; }
    public decimal AmountEUR { get; set; }

    public List<string> BlobNamesToDelete { get; set; } = new();
}