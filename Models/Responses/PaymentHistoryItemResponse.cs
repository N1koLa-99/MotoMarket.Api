namespace MotoMarket.Api.Models.Responses;



public class PaymentHistoryItemResponse

{

    public long Id { get; set; }

    public long UserId { get; set; }

    public long? ListingId { get; set; }



    public string ServiceType { get; set; } = default!;

    public decimal AmountEUR { get; set; }



    public string ProviderName { get; set; } = default!;

    public string? ProviderPaymentId { get; set; }

    public string? ProviderOrderId { get; set; }



    public string? ListingTitleSnapshot { get; set; }

    public string Status { get; set; } = default!;



    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }



    public string? Note { get; set; }

}