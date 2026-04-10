using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.MonthlyRecords;

public sealed record MonthlyRecordQuery(
    Guid EmployeeId,
    int Year,
    int Month);

public sealed record MonthlyTimeCaptureOverviewQuery(
    int Year,
    int Month);

public sealed record MonthlyTimeCaptureOverviewRowDto(
    Guid EmployeeId,
    string PersonnelNumber,
    string FirstName,
    string LastName,
    bool IsActive,
    bool HasMonthCapture,
    decimal HoursWorked,
    decimal NightHours,
    decimal SundayHours,
    decimal HolidayHours,
    decimal VehiclePauschalzone1,
    decimal VehiclePauschalzone2,
    decimal VehicleRegiezone1,
    int TimeEntryCount)
{
    public string StatusDisplay => IsActive ? "Aktiv" : "Inaktiv";
    public string CaptureDisplay => HasMonthCapture ? "Ja" : "Nein";
}

public sealed record MonthlyRecordHeaderDto(
    Guid MonthlyRecordId,
    Guid EmployeeId,
    string EmployeeFullName,
    string EmployeeFirstName,
    string EmployeeLastName,
    string PersonnelNumber,
    int Year,
    int Month,
    EmployeeMonthlyRecordStatus Status,
    DateOnly? ContractValidFrom,
    DateOnly? ContractValidTo,
    decimal? HourlyRateChf,
    decimal? MonthlyBvgDeductionChf,
    decimal TotalWorkedHours,
    decimal TotalSpecialHours,
    decimal TotalExpensesChf,
    decimal TotalVehicleCompensationChf);

public sealed record MonthlyTimeEntryDto(
    Guid TimeEntryId,
    DateOnly WorkDate,
    decimal HoursWorked,
    decimal NightHours,
    decimal SundayHours,
    decimal HolidayHours,
    decimal VehiclePauschalzone1Chf,
    decimal VehiclePauschalzone2Chf,
    decimal VehicleRegiezone1Chf,
    string? Note);

public sealed record MonthlyExpenseEntryDto(
    Guid ExpenseEntryId,
    decimal ExpensesTotalChf);

public sealed record HistoricalMonthlyExpenseEntryDto(
    Guid ExpenseEntryId,
    int Year,
    int Month,
    decimal ExpensesTotalChf);

public sealed record MonthlyPreviewRowDto(
    int Year,
    int Month,
    DateOnly? EntryDate,
    string EntryType,
    string QuantityOrAmount,
    string Details);

public sealed record MonthlyRecordPreviewDto(
    IReadOnlyCollection<MonthlyPreviewRowDto> Rows,
    IReadOnlyCollection<string> Notes);

public sealed record MonthlyPayrollPreviewLineDto(
    string Code,
    string Label,
    string QuantityDisplay,
    string RateDisplay,
    string AmountDisplay,
    string? Detail,
    bool IsEmphasized)
{
    public bool IsRegular => !IsEmphasized;
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
}

public sealed record MonthlyPayrollPreviewDto(
    IReadOnlyCollection<MonthlyPayrollPreviewLineDto> Lines,
    IReadOnlyCollection<string> Notes);

public sealed record MonthlyRecordDetailsDto(
    MonthlyRecordHeaderDto Header,
    IReadOnlyCollection<MonthlyTimeEntryDto> TimeEntries,
    IReadOnlyCollection<MonthlyTimeEntryDto> TimeEntryHistory,
    MonthlyExpenseEntryDto? ExpenseEntry,
    IReadOnlyCollection<HistoricalMonthlyExpenseEntryDto> ExpenseEntryHistory,
    MonthlyRecordPreviewDto Preview,
    MonthlyPayrollPreviewDto PayrollPreview);

public sealed record SaveMonthlyTimeEntryCommand(
    Guid MonthlyRecordId,
    Guid? TimeEntryId,
    DateOnly WorkDate,
    decimal HoursWorked,
    decimal NightHours,
    decimal SundayHours,
    decimal HolidayHours,
    decimal VehiclePauschalzone1Chf,
    decimal VehiclePauschalzone2Chf,
    decimal VehicleRegiezone1Chf,
    string? Note);

public sealed record SaveMonthlyExpenseEntryCommand(
    Guid MonthlyRecordId,
    decimal ExpensesTotalChf);
