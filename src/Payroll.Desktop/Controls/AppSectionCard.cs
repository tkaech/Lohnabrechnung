using Avalonia;
using Avalonia.Controls;

namespace Payroll.Desktop.Controls;

public sealed class AppSectionCard : ContentControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AppSectionCard, string?>(nameof(Header));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}
