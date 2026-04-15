namespace MotoMarket.Api.Infrastructure;

public class PaidActionsOptions
{
    public const string SectionName = "PaidActions";

    public bool RequireSuccessfulPaymentForPaidActions { get; set; }

    public decimal PrivatePaidListingPriceEUR { get; set; }
    public decimal CompanyPaidListingPriceEUR { get; set; }

    public decimal PrivateRefreshPriceEUR { get; set; }
    public decimal CompanyRefreshPriceEUR { get; set; }

    public decimal TopPriceEUR { get; set; }
    public decimal VipPriceEUR { get; set; }
}