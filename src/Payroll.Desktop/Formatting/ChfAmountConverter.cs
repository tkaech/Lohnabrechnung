using System.Globalization;
using Avalonia.Data.Converters;
using Payroll.Application.Formatting;

namespace Payroll.Desktop.Formatting;

public sealed class ChfAmountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            decimal amount => PayrollAmountFormatter.FormatChf(amount),
            double amount => PayrollAmountFormatter.FormatChf((decimal)amount),
            float amount => PayrollAmountFormatter.FormatChf((decimal)amount),
            int amount => PayrollAmountFormatter.FormatChf(amount),
            long amount => PayrollAmountFormatter.FormatChf(amount),
            null => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
