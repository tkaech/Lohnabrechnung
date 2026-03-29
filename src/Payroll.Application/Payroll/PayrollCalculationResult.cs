namespace Payroll.Application.Payroll;

public sealed record PayrollCalculationResult(
    decimal GrossSalary,
    decimal AhvDeduction,
    decimal AlvDeduction,
    decimal ExpenseReimbursement,
    decimal NetSalary);
