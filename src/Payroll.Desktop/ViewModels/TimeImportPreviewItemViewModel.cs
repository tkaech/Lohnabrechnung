namespace Payroll.Desktop.ViewModels;

public sealed class TimeImportPreviewItemViewModel : ViewModelBase
{
    private int _rowNumber;
    private string _personnelNumber = string.Empty;
    private string _fullName = string.Empty;
    private string _status = string.Empty;
    private bool _employeeMatched;
    private bool _monthlyDataExists;
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

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool EmployeeMatched
    {
        get => _employeeMatched;
        set => SetProperty(ref _employeeMatched, value);
    }

    public bool MonthlyDataExists
    {
        get => _monthlyDataExists;
        set => SetProperty(ref _monthlyDataExists, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsImportable => EmployeeMatched;
}
