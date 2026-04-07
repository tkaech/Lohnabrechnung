namespace Payroll.Desktop.ViewModels;

public sealed class EditableSettingOptionViewModel : ViewModelBase
{
    private string _name = string.Empty;

    public Guid OptionId { get; init; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
