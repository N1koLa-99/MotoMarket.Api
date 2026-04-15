namespace MotoMarket.Api.Models.Responses;

public class MyPosCheckoutStartResponse
{
    public long PendingActionId { get; set; }

    public bool IsSimulated { get; set; }
    public bool IsCompleted { get; set; }

    public long? ListingId { get; set; }
    public string? Message { get; set; }

    public string GatewayUrl { get; set; } = default!;
    public string Method { get; set; } = "POST";
    public string OrderId { get; set; } = default!;
    public Dictionary<string, string> Fields { get; set; } = new();
}
