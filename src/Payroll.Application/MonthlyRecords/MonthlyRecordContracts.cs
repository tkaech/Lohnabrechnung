using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.MonthlyRecords;

public sealed record MonthlyRecordQuery(
    Guid EmployeeId,
    int Year,
    int Month);

public sealed record MonthlyRecordHeaderDto(
    Guid MonthlyRecordId,
    Guid EmployeeId,
    string EmployeeFullName,
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
    string? Note);

public sealed record MonthlyExpenseEntryDto(
    Guid ExpenseEntryId,
    DateOnly ExpenseDate,
    decimal AmountChf);

public sealed record MonthlyVehicleCompensationDto(
    Guid VehicleCompensationId,
    DateOnly CompensationDate,
    decimal AmountChf,
    string Description);

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

public sealed record MonthlyRecordDetailsDto(
    MonthlyRecordHeaderDto Header,
    IReadOnlyCollection<MonthlyTimeEntryDto> TimeEntries,
    IReadOnlyCollection<MonthlyExpenseEntryDto> ExpenseEntries,
    IReadOnlyCollection<MonthlyVehicleCompensationDto> VehicleCompensations,
    MonthlyRecordPreviewDto Preview);

public sealed record SaveMonthlyTimeEntryCommand(
    Guid MonthlyRecordId,
    Guid? TimeEntryId,
    DateOnly WorkDate,
    decimal HoursWorked,
    decimal NightHours,
    decimal SundayHours,
    decimal HolidayHours,
    string? Note);

public sealed record SaveMonthlyExpenseEntryCommand(
    Guid MonthlyRecordId,
    Guid? ExpenseEntryId,
    DateOnly ExpenseDate,
    decimal AmountChf);

public sealed record SaveMonthlyVehicleCompensationCommand(
    Guid MonthlyRecordId,
    Guid? VehicleCompensationId,
    DateOnly CompensationDate,
    decimal AmountChf,
    string Description);
