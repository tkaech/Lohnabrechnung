namespace Payroll.Application.AnnualSalary;

public sealed record AnnualSalaryOverviewQuery(
    Guid EmployeeId,
    int Year);

public sealed record AnnualSalaryOverviewDto(
    Guid EmployeeId,
    string PersonnelNumber,
    string FirstName,
    string LastName,
    int Year,
    IReadOnlyCollection<AnnualSalaryMonthDto> Months,
    AnnualSalaryTotalsDto Totals);

public sealed record AnnualSalaryMonthDto(
    int Month,
    string MonthLabel,
    bool IsFinalized,
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
    public string StatusDisplay => IsFinalized ? "abgeschlossen" : "offen";
    public bool IsOpen => !IsFinalized;
    public decimal AhvIvEoAlvDeductionChf => AhvIvEoDeductionChf + AlvDeductionChf;
    public decimal NbuDeductionChf => SicknessDailyAllowanceDeductionChf;
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
