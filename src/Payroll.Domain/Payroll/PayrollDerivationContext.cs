using Payroll.Domain.Employees;

namespace Payroll.Domain.Payroll;

public sealed record PayrollDerivationContext(
    EmployeeWageType WageType,
    string? DepartmentName,
    bool IsDepartmentGavMandatory)
{
    public static PayrollDerivationContext ForHourlyWithoutDepartment { get; } =
        new(EmployeeWageType.Hourly, null, false);
}
