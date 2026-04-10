using System.Globalization;
using Payroll.Domain.Settings;

namespace Payroll.Desktop.Formatting;

public static class NumericFormatManager
{
    private static string _decimalSeparator = PayrollSettings.DefaultDecimalSeparator;

    static NumericFormatManager()
    {
        ApplyDecimalSeparator(_decimalSeparator);
    }

    public static string DecimalSeparator => _decimalSeparator;

    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentCulture;

    public static void ApplyDecimalSeparator(string? separator)
    {
        _decimalSeparator = NormalizeDecimalSeparator(separator);
        CurrentCulture = CreateCulture(_decimalSeparator);
        CultureInfo.DefaultThreadCurrentCulture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;
    }

    public static string NormalizeDecimalSeparator(string? separator)
    {
        return separator == "." ? "." : ",";
    }

    public static string FormatDecimal(decimal value, string format)
    {
        return value.ToString(format, CurrentCulture);
    }

    public static string? FormatNullableDecimal(decimal? value, string format)
    {
        return value.HasValue ? value.Value.ToString(format, CurrentCulture) : null;
    }

    public static bool TryParseDecimal(string? value, out decimal parsedValue)
    {
        parsedValue = 0m;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeInput(value);
        if (normalized is null)
        {
            return false;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out parsedValue);
    }

    private static string? NormalizeInput(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        trimmed = trimmed.Replace(" ", string.Empty, StringComparison.Ordinal)
                         .Replace("'", string.Empty, StringComparison.Ordinal);

        var commaCount = trimmed.Count(character => character == ',');
        var dotCount = trimmed.Count(character => character == '.');
        if (commaCount > 1 || dotCount > 1 || (commaCount > 0 && dotCount > 0))
        {
            return null;
        }

        return trimmed.Replace(',', '.');
    }

    private static CultureInfo CreateCulture(string separator)
    {
        var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = separator;
        culture.NumberFormat.CurrencyDecimalSeparator = separator;
        culture.NumberFormat.NumberGroupSeparator = separator == "," ? "." : ",";
        culture.NumberFormat.CurrencyGroupSeparator = culture.NumberFormat.NumberGroupSeparator;
        return culture;
    }
}
