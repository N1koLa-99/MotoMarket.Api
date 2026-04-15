namespace MotoMarket.Api.Infrastructure;

public class FixedExchangeRatesOptions
{
    public const string SectionName = "FixedExchangeRates";

    public Dictionary<string, decimal> RatesToEUR { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}