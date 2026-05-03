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
    int TimeEntryCount,
    decimal ExpensesTotalChf = 0m,
    decimal SalaryAdvanceNetAdjustmentChf = 0m)
{
    public string StatusDisplay => IsActive ? "Aktiv" : "Inaktiv";
    public string CaptureDisplay => HasMonthCapture ? "Ja" : "Nein";
    public bool HasPayrollRelevantData =>
        HoursWorked > 0m
        || NightHours > 0m
        || SundayHours > 0m
        || HolidayHours > 0m
        || VehiclePauschalzone1 > 0m
        || VehiclePauschalzone2 > 0m
        || VehicleRegiezone1 > 0m
        || ExpensesTotalChf > 0m
        || SalaryAdvanceNetAdjustmentChf != 0m;
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
    string? TaxStatus,
    bool IsSubjectToWithholdingTax,
    decimal WithholdingTaxRatePercent,
    decimal WithholdingTaxCorrectionAmountChf,
    string? WithholdingTaxCorrectionText,
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

public sealed record MonthlySalaryAdvanceDto(
    Guid SalaryAdvanceId,
    int Year,
    int Month,
    decimal AmountChf,
    string? Note,
    decimal OpenAmountChf,
    decimal SettledAmountChf = 0m,
    bool IsSettled = false,
    string? ReferenceDisplay = null);

public sealed record MonthlySalaryAdvanceSettlementDto(
    Guid SalaryAdvanceSettlementId,
    Guid SalaryAdvanceId,
    int Year,
    int Month,
    decimal AmountChf,
    string? Note,
    string? ReferenceDisplay = null);

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
    bool IsEmphasized,
    string? LinkKey = null,
    string? DisplayTag = null,
    string? ColorHint = null)
{
    public bool IsRegular => !IsEmphasized;
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    public bool HasDisplayTag => !string.IsNullOrWhiteSpace(DisplayTag);
}

public sealed record MonthlyPayrollPreviewDerivationItemDto(
    string StepId,
    string KindLabel,
    string Label,
    string ValueDisplay,
    string? FormulaDisplay,
    string? Detail,
    string? LinkKey,
    string? DisplayTag,
    string? ColorHint)
{
    public bool HasFormula => !string.IsNullOrWhiteSpace(FormulaDisplay);
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    public bool HasDisplayTag => !string.IsNullOrWhiteSpace(DisplayTag);
}

public sealed record MonthlyPayrollPreviewDerivationGroupDto(
    string Title,
    IReadOnlyCollection<MonthlyPayrollPreviewDerivationItemDto> Items)
{
    public bool HasItems => Items.Count > 0;
}

public sealed record MonthlyPayrollPreviewDto(
    IReadOnlyCollection<MonthlyPayrollPreviewLineDto> Lines,
    IReadOnlyCollection<MonthlyPayrollPreviewDerivationGroupDto> DerivationGroups,
    IReadOnlyCollection<string> Notes);

public sealed record MonthlyRecordDetailsDto(
    MonthlyRecordHeaderDto Header,
    IReadOnlyCollection<MonthlyTimeEntryDto> TimeEntries,
    IReadOnlyCollection<MonthlyTimeEntryDto> TimeEntryHistory,
    MonthlyExpenseEntryDto? ExpenseEntry,
    IReadOnlyCollection<HistoricalMonthlyExpenseEntryDto> ExpenseEntryHistory,
    MonthlySalaryAdvanceDto? CurrentSalaryAdvance,
    MonthlySalaryAdvanceSettlementDto? CurrentSalaryAdvanceSettlement,
    MonthlySalaryAdvanceDto? OpenSalaryAdvance,
    MonthlyRecordPreviewDto Preview,
    MonthlyPayrollPreviewDto PayrollPreview,
    IReadOnlyCollection<MonthlySalaryAdvanceDto>? SalaryAdvances = null,
    IReadOnlyCollection<MonthlySalaryAdvanceSettlementDto>? SalaryAdvanceSettlements = null);

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

public sealed record SaveMonthlySalaryAdvanceCommand(
    Guid MonthlyRecordId,
    Guid? SalaryAdvanceId,
    decimal AmountChf,
    string? Note);

public sealed record DeleteMonthlySalaryAdvanceCommand(
    Guid MonthlyRecordId,
    Guid SalaryAdvanceId);

public sealed record SaveMonthlySalaryAdvanceSettlementCommand(
    Guid MonthlyRecordId,
    Guid SalaryAdvanceId,
    Guid? SalaryAdvanceSettlementId,
    decimal AmountChf,
    string? Note);

public sealed record SaveMonthlyWithholdingTaxCommand(
    Guid MonthlyRecordId,
    decimal WithholdingTaxRatePercent,
    decimal WithholdingTaxCorrectionAmountChf,
    string? WithholdingTaxCorrectionText);
