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
    bool HasMonthData,
    decimal GrossSalaryChf,
    decimal AhvIvEoAlvDeductionChf,
    decimal NbuDeductionChf,
    decimal BvgDeductionChf,
    decimal WithholdingTaxChf,
    decimal ExpensesChf,
    decimal NetSalaryChf)
{
    public string StatusDisplay => HasMonthData ? "erfasst" : "-";
}

public sealed record AnnualSalaryTotalsDto(
    decimal GrossSalaryChf,
    decimal SocialInsuranceDeductionChf,
    decimal BvgDeductionChf,
    decimal WithholdingTaxChf,
    decimal ExpensesChf,
    decimal NetSalaryChf);
