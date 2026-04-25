using Avalonia.Media;

namespace Payroll.Desktop.ViewModels;

public sealed class LayoutParameterColorPresetViewModel : ViewModelBase
{
    public required string HexValue { get; init; }

    public required IBrush Brush { get; init; }

    public required DelegateCommand SelectCommand { get; init; }
}
