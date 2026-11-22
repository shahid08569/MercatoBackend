namespace MercatoApplication.Common;

public static class SupportedCurrencies
{
    public const string PKR = "PKR"; // Pakistani Rupee
    public const string USD = "USD"; // US Dollar
    public const string EUR = "EUR"; // Euro
    public const string JPY = "JPY"; // Japanese Yen

    // All allowed currencies in the marketplace
    public static readonly string[] AllowedCurrencies = [PKR, USD, EUR, JPY];

    // Validates whether the given currency is supported
    public static bool IsValid(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && AllowedCurrencies.Contains(currency.Trim().ToUpper());
}