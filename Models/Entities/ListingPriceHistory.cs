namespace MotoMarket.Api.Models.Entities;

public class ListingPriceHistory
{
    public long Id { get; set; }
    public long ListingId { get; set; }

    public string ChangeType { get; set; } = default!; // UP / DOWN

    public decimal? OldPriceOriginal { get; set; }
    public decimal NewPriceOriginal { get; set; }

    public decimal? OldPriceEUR { get; set; }
    public decimal NewPriceEUR { get; set; }

    public string CurrencyCode { get; set; } = default!;
    public decimal ExchangeRateToEUR { get; set; }

    public DateTime ChangedAt { get; set; }
}