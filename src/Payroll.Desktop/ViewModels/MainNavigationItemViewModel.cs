namespace Payroll.Desktop.ViewModels;

public enum MainSection
{
    Employees,
    TimeAndExpenses,
    PayrollRuns,
    AhvAndDeductions,
    WithholdingTax,
    Reporting,
    Settings,
    Help
}

public sealed class MainNavigationItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public MainNavigationItemViewModel(MainSection section, string label, bool isEnabled, Action? activate)
    {
        Section = section;
        Label = label;
        IsEnabled = isEnabled;
        ActivateCommand = new DelegateCommand(
            () => activate?.Invoke(),
            () => IsEnabled);
    }

    public MainSection Section { get; }

    public string Label { get; }

    public bool IsEnabled { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                RaisePropertyChanged(nameof(IsNotSelected));
            }
        }
    }

    public bool IsNotSelected => !IsSelected;

    public DelegateCommand ActivateCommand { get; }
}
