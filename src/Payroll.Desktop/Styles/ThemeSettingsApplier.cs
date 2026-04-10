using Avalonia;
using Avalonia.Media;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;

namespace Payroll.Desktop.Styles;

public static class ThemeSettingsApplier
{
    public static void Apply(PayrollSettingsDto settings)
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        var resources = Avalonia.Application.Current.Resources;

        NumericFormatManager.ApplyDecimalSeparator(settings.DecimalSeparator);

        resources["Theme.FontFamily"] = CreateFontFamily(settings.AppFontFamily, Payroll.Domain.Settings.PayrollSettings.DefaultAppFontFamily);
        resources["Theme.FontSize.Body"] = (double)settings.AppFontSize;
        resources["Theme.FontSize.Caption"] = Math.Max(10d, (double)settings.AppFontSize - 1d);
        resources["Theme.FontSize.Section"] = (double)settings.AppFontSize + 3d;
        resources["Theme.FontSize.Page"] = (double)settings.AppFontSize + 5d;
        resources["Theme.FontSize.AppTitle"] = (double)settings.AppFontSize + 11d;
        resources["Theme.Color.TextPrimary"] = CreateBrush(settings.AppTextColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultAppTextColorHex);
        resources["Theme.Color.TextMuted"] = CreateBrush(settings.AppMutedTextColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultAppMutedTextColorHex);
        resources["Theme.Color.AppBackground"] = CreateBrush(settings.AppBackgroundColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultAppBackgroundColorHex);
        resources["Theme.Color.Brand"] = CreateBrush(settings.AppAccentColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultAppAccentColorHex);

        resources["Print.FontFamily"] = CreateFontFamily(settings.PrintFontFamily, Payroll.Domain.Settings.PayrollSettings.DefaultPrintFontFamily);
        resources["Print.FontSize.Body"] = (double)settings.PrintFontSize;
        resources["Print.FontSize.Caption"] = Math.Max(7d, (double)settings.PrintFontSize - 1d);
        resources["Print.FontSize.Title"] = (double)settings.PrintFontSize + 2d;
        resources["Print.Color.TextPrimary"] = CreateBrush(settings.PrintTextColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultPrintTextColorHex);
        resources["Print.Color.TextMuted"] = CreateBrush(settings.PrintMutedTextColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultPrintMutedTextColorHex);
        resources["Print.Color.Accent"] = CreateBrush(settings.PrintAccentColorHex, Payroll.Domain.Settings.PayrollSettings.DefaultPrintAccentColorHex);
    }

    private static FontFamily CreateFontFamily(string? value, string fallback)
    {
        try
        {
            return new FontFamily(string.IsNullOrWhiteSpace(value) ? fallback : value);
        }
        catch
        {
            return new FontFamily(fallback);
        }
    }

    private static IBrush CreateBrush(string? value, string fallback)
    {
        try
        {
            return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(value) ? fallback : value));
        }
        catch
        {
            return new SolidColorBrush(Color.Parse(fallback));
        }
    }
}
