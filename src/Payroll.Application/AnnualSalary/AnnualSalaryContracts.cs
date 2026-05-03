using Payroll.Application.Formatting;

namespace Payroll.Application.AnnualSalary;

public enum AnnualSalaryMonthStatus
{
    Finalized,
    Cancelled,
    OpenWithRecordedData,
    Open
}

public sealed record AnnualSalaryOverviewQuery(
    Guid EmployeeId,
    int Year);

public sealed record AnnualSalaryOverviewDto(
    Guid EmployeeId,
    string PersonnelNumber,
    string FirstName,
    string LastName,
    string? AhvNumber,
    DateOnly? BirthDate,
    int Year,
    IReadOnlyCollection<AnnualSalaryMonthDto> Months,
    AnnualSalaryTotalsDto Totals);

public sealed record AnnualSalaryMonthDto(
    int Month,
    string MonthLabel,
    bool IsFinalized,
    bool HasCancelledRun,
    bool HasRecordedMonthData,
    decimal GrossSalaryChf,
    decimal AhvIvEoDeductionChf,
    decimal AlvDeductionChf,
    decimal SicknessDailyAllowanceDeductionChf,
    decimal TrainingAndEducationDeductionChf,
    decimal TotalSocialDeductionChf,
    decimal BvgDeductionChf,
    decimal WithholdingTaxChf,
    decimal ExpensesChf,
    decimal NetSalaryChf)
{
    public AnnualSalaryMonthStatus Status => IsFinalized
        ? AnnualSalaryMonthStatus.Finalized
        : HasCancelledRun
            ? AnnualSalaryMonthStatus.Cancelled
            : HasRecordedMonthData
                ? AnnualSalaryMonthStatus.OpenWithRecordedData
                : AnnualSalaryMonthStatus.Open;
    public string StatusDisplay => Status switch
    {
        AnnualSalaryMonthStatus.Finalized => "abgeschlossen",
        AnnualSalaryMonthStatus.Cancelled => "storniert",
        AnnualSalaryMonthStatus.OpenWithRecordedData => "offen / Daten vorhanden",
        _ => "offen"
    };
    public bool IsOpen => !IsFinalized;
    public bool IsStatusFinalized => Status == AnnualSalaryMonthStatus.Finalized;
    public bool IsStatusCancelled => Status == AnnualSalaryMonthStatus.Cancelled;
    public bool IsStatusOpenWithRecordedData => Status == AnnualSalaryMonthStatus.OpenWithRecordedData;
    public bool IsStatusOpen => Status == AnnualSalaryMonthStatus.Open;
    public decimal AhvIvEoAlvDeductionChf => AhvIvEoDeductionChf + AlvDeductionChf;
    public decimal NbuDeductionChf => SicknessDailyAllowanceDeductionChf;
    public string GrossSalaryDisplay => PayrollAmountFormatter.FormatChf(GrossSalaryChf);
    public string AhvIvEoDeductionDisplay => PayrollAmountFormatter.FormatChf(AhvIvEoDeductionChf);
    public string AlvDeductionDisplay => PayrollAmountFormatter.FormatChf(AlvDeductionChf);
    public string SicknessDailyAllowanceDeductionDisplay => PayrollAmountFormatter.FormatChf(SicknessDailyAllowanceDeductionChf);
    public string TrainingAndEducationDeductionDisplay => PayrollAmountFormatter.FormatChf(TrainingAndEducationDeductionChf);
    public string TotalSocialDeductionDisplay => PayrollAmountFormatter.FormatChf(TotalSocialDeductionChf);
    public string BvgDeductionDisplay => PayrollAmountFormatter.FormatChf(BvgDeductionChf);
    public string WithholdingTaxDisplay => PayrollAmountFormatter.FormatChf(WithholdingTaxChf);
    public string ExpensesDisplay => PayrollAmountFormatter.FormatChf(ExpensesChf);
    public string NetSalaryDisplay => PayrollAmountFormatter.FormatChf(NetSalaryChf);
}

public sealed record AnnualSalaryTotalsDto(
    decimal GrossSalaryChf,
    decimal AhvIvEoDeductionChf,
    decimal AlvDeductionChf,
    decimal SicknessDailyAllowanceDeductionChf,
    decimal TrainingAndEducationDeductionChf,
    decimal SocialInsuranceDeductionChf,
    decimal BvgDeductionChf,
    decimal WithholdingTaxChf,
    decimal ExpensesChf,
    decimal NetSalaryChf);
