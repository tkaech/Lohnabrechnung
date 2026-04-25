namespace Payroll.Desktop.ViewModels;

public enum HelpSection
{
    Overview,
    LayoutParameters
}

public sealed class HelpNavigationItemViewModel : ViewModelBase
{
    public HelpNavigationItemViewModel(HelpSection section, string label)
    {
        Section = section;
        Label = label;
    }

    public HelpSection Section { get; }

    public string Label { get; }
}
