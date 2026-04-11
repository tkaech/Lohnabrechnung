namespace Payroll.Desktop.ViewModels;

public sealed class ImportedMonthStatusItemViewModel : ViewModelBase
{
    private int _year;
    private int _month;
    private DateTimeOffset _importedAtUtc;

    public int Year
    {
        get => _year;
        set => SetProperty(ref _year, value);
    }

    public int Month
    {
        get => _month;
        set => SetProperty(ref _month, value);
    }

    public DateTimeOffset ImportedAtUtc
    {
        get => _importedAtUtc;
        set => SetProperty(ref _importedAtUtc, value);
    }

    public string DisplayName => $"{Month:D2}/{Year:D4}";
}
