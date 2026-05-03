using Payroll.Domain.Common;

namespace Payroll.Domain.MonthlyRecords;

public sealed class SalaryAdvanceSettlement : AuditableEntity
{
    private SalaryAdvanceSettlement()
    {
    }

    public Guid EmployeeMonthlyRecordId { get; private set; }
    public Guid SalaryAdvanceId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal AmountChf { get; private set; }
    public string? Note { get; private set; }

    public SalaryAdvanceSettlement(
        Guid employeeMonthlyRecordId,
        Guid salaryAdvanceId,
        Guid employeeId,
        int year,
        int month,
        decimal amountChf,
        string? note)
    {
        if (employeeMonthlyRecordId == Guid.Empty)
        {
            throw new ArgumentException("Monthly record is required.", nameof(employeeMonthlyRecordId));
        }

        if (salaryAdvanceId == Guid.Empty)
        {
            throw new ArgumentException("Salary advance is required.", nameof(salaryAdvanceId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(employeeId));
        }

        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        SalaryAdvanceId = salaryAdvanceId;
        EmployeeId = employeeId;
        Update(employeeMonthlyRecordId, year, month, amountChf, note);
    }

    public void Update(
        Guid employeeMonthlyRecordId,
        int year,
        int month,
        decimal amountChf,
        string? note)
    {
        if (employeeMonthlyRecordId == Guid.Empty)
        {
            throw new ArgumentException("Monthly record is required.", nameof(employeeMonthlyRecordId));
        }

        _ = new DateOnly(year, month, 1);

        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        Year = year;
        Month = month;
        AmountChf = Guard.AgainstZeroOrNegative(amountChf, nameof(amountChf));
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }
}
