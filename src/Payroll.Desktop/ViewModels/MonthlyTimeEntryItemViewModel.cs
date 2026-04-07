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
    public required string Summary { get; init; }
}
