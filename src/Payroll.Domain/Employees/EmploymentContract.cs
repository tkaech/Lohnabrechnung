using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class EmploymentContract : AuditableEntity
{
    private EmploymentContract()
    {
    }

    public Guid EmployeeId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal HourlyRateChf { get; private set; }
    public decimal MonthlySalaryAmountChf { get; private set; }
    public decimal MonthlyBvgDeductionChf { get; private set; }
    public decimal SpecialSupplementRateChf { get; private set; }
    public EmployeeWageType WageType { get; private set; }

    public EmploymentContract(
        Guid employeeId,
        DateOnly validFrom,
        DateOnly? validTo,
        decimal hourlyRateChf,
        decimal monthlyBvgDeductionChf,
        decimal specialSupplementRateChf,
        EmployeeWageType wageType = EmployeeWageType.Hourly,
        decimal monthlySalaryAmountChf = 0m)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));

        EmployeeId = employeeId;
        ValidFrom = validFrom;
        ValidTo = validTo;
        HourlyRateChf = Guard.AgainstZeroOrNegative(hourlyRateChf, nameof(hourlyRateChf));
        MonthlySalaryAmountChf = Guard.AgainstNegative(monthlySalaryAmountChf, nameof(monthlySalaryAmountChf));
        MonthlyBvgDeductionChf = Guard.AgainstNegative(monthlyBvgDeductionChf, nameof(monthlyBvgDeductionChf));
        SpecialSupplementRateChf = Guard.AgainstNegative(specialSupplementRateChf, nameof(specialSupplementRateChf));
        WageType = wageType;
    }

    public bool IsActiveOn(DateOnly date)
    {
        return date >= ValidFrom && (!ValidTo.HasValue || date <= ValidTo.Value);
    }

    public decimal CalculateGrossPay(decimal hours)
    {
        return Guard.AgainstNegative(hours, nameof(hours)) * HourlyRateChf;
    }

    public decimal CalculateSpecialSupplement(decimal hours)
    {
        return Guard.AgainstNegative(hours, nameof(hours)) * SpecialSupplementRateChf;
    }

    public void UpdateTerms(
        DateOnly validFrom,
        DateOnly? validTo,
        decimal hourlyRateChf,
        decimal monthlyBvgDeductionChf,
        decimal specialSupplementRateChf,
        EmployeeWageType wageType = EmployeeWageType.Hourly,
        decimal monthlySalaryAmountChf = 0m)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));

        ValidFrom = validFrom;
        ValidTo = validTo;
        HourlyRateChf = Guard.AgainstZeroOrNegative(hourlyRateChf, nameof(hourlyRateChf));
        MonthlySalaryAmountChf = Guard.AgainstNegative(monthlySalaryAmountChf, nameof(monthlySalaryAmountChf));
        MonthlyBvgDeductionChf = Guard.AgainstNegative(monthlyBvgDeductionChf, nameof(monthlyBvgDeductionChf));
        SpecialSupplementRateChf = Guard.AgainstNegative(specialSupplementRateChf, nameof(specialSupplementRateChf));
        WageType = wageType;
        Touch();
    }
}
