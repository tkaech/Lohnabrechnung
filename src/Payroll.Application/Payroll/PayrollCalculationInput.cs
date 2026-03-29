using Payroll.Domain.Employees;

namespace Payroll.Application.Payroll;

public sealed record PayrollCalculationInput(
    Employee Employee,
    decimal WorkedHours,
    decimal ExpenseTotal,
    decimal AhvRate,
    decimal AlvRate);
