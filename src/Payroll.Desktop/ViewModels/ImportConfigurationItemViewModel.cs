namespace Payroll.Desktop.ViewModels;

public sealed class ImportConfigurationItemViewModel : ViewModelBase
{
    private Guid _configurationId;
    private string _name = string.Empty;

    public Guid ConfigurationId
    {
        get => _configurationId;
        set => SetProperty(ref _configurationId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
