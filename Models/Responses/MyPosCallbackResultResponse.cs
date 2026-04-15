namespace MotoMarket.Api.Models.Responses;

public class MyPosCallbackResultResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public long? PendingActionId { get; set; }
    public long? ListingId { get; set; }
}