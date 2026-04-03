using Payroll.Domain.Common;

namespace Payroll.Domain.Expenses;

public sealed class ExpenseEntry : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public string ExpenseTypeCode { get; private set; }
    public decimal AmountChf { get; private set; }
    public string Description { get; private set; }
    public string Currency => "CHF";

    public ExpenseEntry(Guid employeeId, DateOnly expenseDate, string expenseTypeCode, decimal amount, string description)
    {
        EmployeeId = employeeId;
        ExpenseDate = expenseDate;
        ExpenseTypeCode = Guard.AgainstNullOrWhiteSpace(expenseTypeCode, nameof(expenseTypeCode));
        AmountChf = Guard.AgainstNegative(amount, nameof(amount));
        Description = Guard.AgainstNullOrWhiteSpace(description, nameof(description));
    }
}
