using Payroll.Domain.Employees;

namespace Payroll.Domain.Payroll;

public sealed record PayrollDerivationContext(
    EmployeeWageType WageType,
    string? DepartmentName,
    bool IsDepartmentGavMandatory,
    bool IsSubjectToWithholdingTax = false,
    string? WithholdingTaxStatus = null,
    decimal WithholdingTaxRatePercent = 0m,
    decimal WithholdingTaxCorrectionAmountChf = 0m,
    string? WithholdingTaxCorrectionText = null)
{
    public static PayrollDerivationContext ForHourlyWithoutDepartment { get; } =
        new(EmployeeWageType.Hourly, null, false);
}
