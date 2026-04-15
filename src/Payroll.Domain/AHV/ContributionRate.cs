using Payroll.Domain.Common;

namespace Payroll.Domain.AHV;

public sealed class ContributionRate : AuditableEntity
{
    public string Code { get; private set; }
    public decimal EmployeeRate { get; private set; }
    public decimal EmployerRate { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    public ContributionRate(
        string code,
        decimal employeeRate,
        decimal employerRate,
        DateOnly validFrom,
        DateOnly? validTo = null)
    {
        Guard.AgainstInvalidPeriod(validFrom, validTo, nameof(validTo));

        Code = Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        EmployeeRate = Guard.AgainstRateOutOfRange(employeeRate, nameof(employeeRate));
        EmployerRate = Guard.AgainstRateOutOfRange(employerRate, nameof(employerRate));
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public bool IsValidOn(DateOnly date)
    {
        return date >= ValidFrom && (!ValidTo.HasValue || date <= ValidTo.Value);
    }

    public decimal CalculateEmployeeContribution(decimal insuredAmountChf)
    {
        return Guard.AgainstNegative(insuredAmountChf, nameof(insuredAmountChf)) * EmployeeRate;
    }

    public decimal CalculateEmployerContribution(decimal insuredAmountChf)
    {
        return Guard.AgainstNegative(insuredAmountChf, nameof(insuredAmountChf)) * EmployerRate;
    }
}
