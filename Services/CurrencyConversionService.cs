using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Services;

public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly FixedExchangeRatesOptions _options;

    public CurrencyConversionService(IOptions<FixedExchangeRatesOptions> options)
    {
        _options = options.Value;
    }

    public decimal ConvertFromEUR(decimal amountEur, string targetCurrencyCode)
    {
        var rateToEur = GetRateToEUR(targetCurrencyCode);

        if (rateToEur <= 0)
            throw new InvalidOperationException("Невалиден курс за валута.");

        return Math.Round(amountEur / rateToEur, 2, MidpointRounding.AwayFromZero);
    }

    public decimal GetRateToEUR(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new InvalidOperationException("Липсва валута.");

        if (_options.RatesToEUR.TryGetValue(currencyCode.Trim().ToUpperInvariant(), out var rate))
            return rate;

        throw new InvalidOperationException($"Липсва фиксиран курс за валута: {currencyCode}");
    }
}