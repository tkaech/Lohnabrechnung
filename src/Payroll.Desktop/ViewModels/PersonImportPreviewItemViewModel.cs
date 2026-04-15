namespace Payroll.Desktop.ViewModels;

public sealed class PersonImportPreviewItemViewModel : ViewModelBase
{
    private int _rowNumber;
    private string _personnelNumber = string.Empty;
    private string _fullName = string.Empty;
    private bool _alreadyExists;
    private bool _isSelected = true;

    public int RowNumber
    {
        get => _rowNumber;
        set => SetProperty(ref _rowNumber, value);
    }

    public string PersonnelNumber
    {
        get => _personnelNumber;
        set => SetProperty(ref _personnelNumber, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public bool AlreadyExists
    {
        get => _alreadyExists;
        set => SetProperty(ref _alreadyExists, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string ImportStatus => AlreadyExists ? "bereits vorhanden" : "neu";
}
