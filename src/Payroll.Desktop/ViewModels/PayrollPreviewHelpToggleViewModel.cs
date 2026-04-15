namespace Payroll.Desktop.ViewModels;

public sealed class PayrollPreviewHelpToggleViewModel : ViewModelBase
{
    private bool _isEnabled;
    private string _helpText = string.Empty;

    public required string Code { get; init; }
    public required string Label { get; init; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string HelpText
    {
        get => _helpText;
        set => SetProperty(ref _helpText, value);
    }
}
