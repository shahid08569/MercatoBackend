using System.Text.Json;
using MercatoApplication.Common;
using MercatoApplication.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MercatoInfrastructure.Services;

public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CurrencyExchangeService> _logger;

    private const string CacheKey = "Live_Forex_Exchange_Rates";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    // Safe baseline fallbacks in case external network is unreachable
    private static readonly Dictionary<string, decimal> FallbackRatesToUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        { SupportedCurrencies.USD, 1.00m },
        { SupportedCurrencies.PKR, 276.50m },
        { SupportedCurrencies.EUR, 0.92m },
        { SupportedCurrencies.JPY, 155.00m }
    };

    public CurrencyExchangeService(HttpClient httpClient, IMemoryCache cache, ILogger<CurrencyExchangeService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    // Returns current live exchange rates relative to USD (cached for 12 hours)
    public async Task<Dictionary<string, decimal>> GetLiveRatesAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? cachedRates) && cachedRates != null)
        {
            return cachedRates;
        }

        try
        {
            // Free public international open exchange rates API (daily updated)
            var response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("rates", out var ratesElement))
                {
                    var liveRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    {
                        { SupportedCurrencies.USD, 1.00m }
                    };

                    foreach (var curr in SupportedCurrencies.AllowedCurrencies)
                    {
                        if (curr == SupportedCurrencies.USD) continue;

                        if (ratesElement.TryGetProperty(curr, out var rateProp) && rateProp.TryGetDecimal(out var rate))
                        {
                            liveRates[curr] = rate;
                        }
                        else
                        {
                            liveRates[curr] = FallbackRatesToUsd[curr];
                        }
                    }

                    _cache.Set(CacheKey, liveRates, CacheDuration);
                    _logger.LogInformation("Successfully fetched and cached live Forex rates: 1 USD = {PkrRate} PKR", liveRates[SupportedCurrencies.PKR]);
                    return liveRates;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch live exchange rates from external API. Using fallback rates.");
        }

        return FallbackRatesToUsd;
    }

    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        fromCurrency = fromCurrency.Trim().ToUpper();
        toCurrency = toCurrency.Trim().ToUpper();

        if (fromCurrency == toCurrency)
            return 1.0m;

        var rates = await GetLiveRatesAsync();

        if (!rates.TryGetValue(fromCurrency, out var fromRate) ||
            !rates.TryGetValue(toCurrency, out var toRate) || fromRate == 0)
        {
            return 1.0m;
        }

        return toRate / fromRate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (amount == 0 || fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency);
        var converted = amount * rate;

        // JPY does not use fractional cents/decimals in commercial pricing
        return toCurrency.Equals(SupportedCurrencies.JPY, StringComparison.OrdinalIgnoreCase)
            ? Math.Round(converted, 0)
            : Math.Round(converted, 2);
    }

    public async Task<Dictionary<string, decimal>> GetAllConversionsAsync(decimal amount, string fromCurrency)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in SupportedCurrencies.AllowedCurrencies)
        {
            result[target] = await ConvertAsync(amount, fromCurrency, target);
        }
        return result;
    }
}