namespace Payroll.Desktop.ViewModels;

public enum SettingsSection
{
    SystemDb,
    Layout,
    Lists,
    Calculation,
    Sql,
    Print,
    BackupRestore,
    Import
}

public sealed class SettingsNavigationItemViewModel : ViewModelBase
{
    public SettingsNavigationItemViewModel(SettingsSection section, string label)
    {
        Section = section;
        Label = label;
    }

    public SettingsSection Section { get; }

    public string Label { get; }
}
