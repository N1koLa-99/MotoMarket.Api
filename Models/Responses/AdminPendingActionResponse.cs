namespace MotoMarket.Api.Models.Responses;

public class AdminPendingActionResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? ListingId { get; set; }

    public string ActionType { get; set; } = default!;
    public decimal AmountEUR { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public string Status { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ProviderName { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderOrderId { get; set; }

    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
}