using Payroll.Domain.Common;

namespace Payroll.Domain.Expenses;

public sealed class ExpenseClaim : Entity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public bool IsManualAdjustment { get; private set; }

    private ExpenseClaim()
    {
    }

    public ExpenseClaim(Guid employeeId, DateOnly expenseDate, string description, decimal amount, bool isManualAdjustment)
    {
        EmployeeId = employeeId;
        ExpenseDate = expenseDate;
        Description = description;
        Amount = amount;
        IsManualAdjustment = isManualAdjustment;
    }
}
