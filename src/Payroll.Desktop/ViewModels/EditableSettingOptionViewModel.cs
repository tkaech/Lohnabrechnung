namespace Payroll.Desktop.ViewModels;

public sealed class EditableSettingOptionViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private bool _isGavMandatory;

    public Guid OptionId { get; init; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsGavMandatory
    {
        get => _isGavMandatory;
        set => SetProperty(ref _isGavMandatory, value);
    }
}
