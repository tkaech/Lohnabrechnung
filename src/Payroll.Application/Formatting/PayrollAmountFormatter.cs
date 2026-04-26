using System.Globalization;
using Payroll.Domain.Settings;

namespace Payroll.Application.Formatting;

public static class PayrollAmountFormatter
{
    private static readonly CultureInfo AmountCulture = CreateAmountCulture();

    public static string FormatAmount(decimal value)
    {
        return value.ToString("#,##0.00", AmountCulture);
    }

    public static string FormatChf(decimal value)
    {
        return FormatMoney(value, PayrollSettings.DefaultCurrencyCode);
    }

    public static string FormatMoney(decimal value, string? currencyCode)
    {
        return $"{FormatAmount(value)} {NormalizeCurrencyCode(currencyCode)}";
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
    {
        return string.IsNullOrWhiteSpace(currencyCode)
            ? PayrollSettings.DefaultCurrencyCode
            : currencyCode.Trim().ToUpperInvariant();
    }

    private static CultureInfo CreateAmountCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ".";
        culture.NumberFormat.NumberGroupSeparator = PayrollSettings.DefaultThousandsSeparator;
        return culture;
    }
}
