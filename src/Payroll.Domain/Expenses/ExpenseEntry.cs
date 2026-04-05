using Payroll.Domain.Common;

namespace Payroll.Domain.Expenses;

public sealed class ExpenseEntry : AuditableEntity
{
    public const string PayrollCode = "EXP";
    public const string DisplayName = "Diverse Spesen";

    private ExpenseEntry()
    {
    }

    public Guid EmployeeMonthlyRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public decimal AmountChf { get; private set; }
    public string ExpenseTypeCode { get; private set; } = PayrollCode;
    public string Description { get; private set; } = DisplayName;
    public string Currency => "CHF";

    public ExpenseEntry(Guid employeeId, DateOnly expenseDate, decimal amount)
        : this(Guid.Empty, employeeId, expenseDate, amount)
    {
    }

    public ExpenseEntry(Guid employeeMonthlyRecordId, Guid employeeId, DateOnly expenseDate, decimal amount)
    {
        EmployeeId = employeeId;
        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        Update(expenseDate, amount);
    }

    public void Update(DateOnly expenseDate, decimal amount)
    {
        ExpenseDate = expenseDate;
        AmountChf = Guard.AgainstNegative(amount, nameof(amount));
        ExpenseTypeCode = PayrollCode;
        Description = DisplayName;
        Touch();
    }
}
