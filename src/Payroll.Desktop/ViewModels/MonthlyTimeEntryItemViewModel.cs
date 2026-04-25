using System.Collections.ObjectModel;

namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyTimeEntryItemViewModel
{
    public required Guid TimeEntryId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required DateOnly WorkDate { get; init; }
    public required decimal HoursWorked { get; init; }
    public required decimal NightHours { get; init; }
    public required decimal SundayHours { get; init; }
    public required decimal HolidayHours { get; init; }
    public required decimal VehiclePauschalzone1Chf { get; init; }
    public required decimal VehiclePauschalzone2Chf { get; init; }
    public required decimal VehicleRegiezone1Chf { get; init; }
    public required bool IsCurrentMonth { get; init; }
    public string? Note { get; init; }
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
    public required string Summary { get; init; }
    public ObservableCollection<TimeEntryCellViewModel> Cells { get; } = [];

    public void ApplyColumnOrder(IEnumerable<TimeEntryColumnViewModel> columns)
    {
        Cells.Clear();
        foreach (var column in columns)
        {
            Cells.Add(new TimeEntryCellViewModel(column.Key, column.Width, GetCellValue(column.Key)));
        }
    }

    private string GetCellValue(string key)
    {
        return key switch
        {
            TimeEntryColumnViewModel.WorkDateKey => $"{WorkDate:dd.MM.yyyy}",
            TimeEntryColumnViewModel.MonthKey => $"{WorkDate:MM.yyyy}",
            TimeEntryColumnViewModel.HoursWorkedKey => $"{HoursWorked:0.##} h",
            TimeEntryColumnViewModel.NightHoursKey => $"{NightHours:0.##}",
            TimeEntryColumnViewModel.SundayHoursKey => $"{SundayHours:0.##}",
            TimeEntryColumnViewModel.HolidayHoursKey => $"{HolidayHours:0.##}",
            TimeEntryColumnViewModel.VehiclePauschalzone1Key => $"{VehiclePauschalzone1Chf:0.00}",
            TimeEntryColumnViewModel.VehiclePauschalzone2Key => $"{VehiclePauschalzone2Chf:0.00}",
            TimeEntryColumnViewModel.VehicleRegiezone1Key => $"{VehicleRegiezone1Chf:0.00}",
            _ => string.Empty
        };
    }
}

public sealed record TimeEntryColumnViewModel(string Key, string Header, double Width)
{
    public const string WorkDateKey = "WorkDate";
    public const string MonthKey = "Month";
    public const string HoursWorkedKey = "HoursWorked";
    public const string NightHoursKey = "NightHours";
    public const string SundayHoursKey = "SundayHours";
    public const string HolidayHoursKey = "HolidayHours";
    public const string VehiclePauschalzone1Key = "VehiclePauschalzone1";
    public const string VehiclePauschalzone2Key = "VehiclePauschalzone2";
    public const string VehicleRegiezone1Key = "VehicleRegiezone1";
}

public sealed record TimeEntryCellViewModel(string Key, double Width, string Value);
