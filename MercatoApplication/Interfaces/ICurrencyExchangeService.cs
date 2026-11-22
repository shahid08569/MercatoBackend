namespace MercatoApplication.Interfaces;

public interface ICurrencyExchangeService
{
    // Fetches live exchange rate between two currencies (e.g. USD -> PKR)
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency);

    // Converts an amount between two currencies using live market rates
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);

    // Calculates live conversion breakdown across all 4 currencies (PKR, USD, EUR, JPY)
    Task<Dictionary<string, decimal>> GetAllConversionsAsync(decimal amount, string fromCurrency);

    // Returns current live rate dictionary relative to USD base
    Task<Dictionary<string, decimal>> GetLiveRatesAsync();
}