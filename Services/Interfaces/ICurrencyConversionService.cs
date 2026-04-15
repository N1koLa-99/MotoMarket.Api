namespace MotoMarket.Api.Services.Interfaces;

public interface ICurrencyConversionService
{
    decimal ConvertFromEUR(decimal amountEur, string targetCurrencyCode);
    decimal GetRateToEUR(string currencyCode);
}