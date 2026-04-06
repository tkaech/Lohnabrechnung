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
    public decimal ExpensesTotalChf { get; private set; }
    public string ExpenseTypeCode { get; private set; } = PayrollCode;
    public string Description { get; private set; } = DisplayName;
    public string Currency => "CHF";

    public ExpenseEntry(Guid employeeId, decimal expensesTotalChf)
        : this(Guid.Empty, employeeId, expensesTotalChf)
    {
    }

    public ExpenseEntry(Guid employeeMonthlyRecordId, Guid employeeId, decimal expensesTotalChf)
    {
        EmployeeId = employeeId;
        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        Update(expensesTotalChf);
    }

    public void Update(decimal expensesTotalChf)
    {
        ExpensesTotalChf = Guard.AgainstNegative(expensesTotalChf, nameof(expensesTotalChf));
        ExpenseTypeCode = PayrollCode;
        Description = DisplayName;
        Touch();
    }
}
