using Payroll.Domain.Common;

namespace Payroll.Domain.Payroll;

public sealed class PayrollEntry : Entity
{
    public Guid PayrollRunId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal GrossSalary { get; private set; }
    public decimal AhvDeduction { get; private set; }
    public decimal AlvDeduction { get; private set; }
    public decimal ExpenseReimbursement { get; private set; }
    public decimal NetSalary { get; private set; }

    private PayrollEntry()
    {
    }

    public PayrollEntry(
        Guid payrollRunId,
        Guid employeeId,
        decimal grossSalary,
        decimal ahvDeduction,
        decimal alvDeduction,
        decimal expenseReimbursement)
    {
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        GrossSalary = grossSalary;
        AhvDeduction = ahvDeduction;
        AlvDeduction = alvDeduction;
        ExpenseReimbursement = expenseReimbursement;
        NetSalary = grossSalary - ahvDeduction - alvDeduction + expenseReimbursement;
    }
}
