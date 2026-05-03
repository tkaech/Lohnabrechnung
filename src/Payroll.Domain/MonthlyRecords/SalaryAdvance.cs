using Payroll.Domain.Common;

namespace Payroll.Domain.MonthlyRecords;

public sealed class SalaryAdvance : AuditableEntity
{
    private readonly List<SalaryAdvanceSettlement> _settlements = [];

    private SalaryAdvance()
    {
    }

    public Guid EmployeeMonthlyRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal AmountChf { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyCollection<SalaryAdvanceSettlement> Settlements => _settlements.AsReadOnly();
    public decimal SettledAmountChf => _settlements.Sum(item => item.AmountChf);
    public decimal OpenAmountChf => AmountChf - SettledAmountChf;
    public bool IsSettled => OpenAmountChf == 0m;

    public SalaryAdvance(
        Guid employeeMonthlyRecordId,
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

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(employeeId));
        }

        _ = new DateOnly(year, month, 1);

        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        EmployeeId = employeeId;
        Year = year;
        Month = month;
        Update(amountChf, note);
    }

    public void Update(decimal amountChf, string? note)
    {
        var safeAmount = Guard.AgainstZeroOrNegative(amountChf, nameof(amountChf));
        if (safeAmount < SettledAmountChf)
        {
            throw new InvalidOperationException("Advance amount cannot be lower than already settled amounts.");
        }

        AmountChf = safeAmount;
        Note = NormalizeOptional(note);
        Touch();
    }

    public SalaryAdvanceSettlement SaveSettlement(
        Guid? settlementId,
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
        EnsureSettlementMonthIsAfterAdvance(year, month);

        var existingSettlement = ResolveSettlement(settlementId);
        var safeAmount = Guard.AgainstZeroOrNegative(amountChf, nameof(amountChf));
        var settledWithoutCurrent = SettledAmountChf - (existingSettlement?.AmountChf ?? 0m);
        if (settledWithoutCurrent + safeAmount > AmountChf)
        {
            throw new InvalidOperationException("Settlement amount exceeds the open salary advance balance.");
        }

        if (existingSettlement is null)
        {
            var createdSettlement = new SalaryAdvanceSettlement(
                employeeMonthlyRecordId,
                Id,
                EmployeeId,
                year,
                month,
                safeAmount,
                note);
            _settlements.Add(createdSettlement);
            Touch();
            return createdSettlement;
        }

        existingSettlement.Update(employeeMonthlyRecordId, year, month, safeAmount, note);
        Touch();
        return existingSettlement;
    }

    private SalaryAdvanceSettlement? ResolveSettlement(Guid? settlementId)
    {
        if (!settlementId.HasValue)
        {
            return null;
        }

        return _settlements.SingleOrDefault(item => item.Id == settlementId.Value)
            ?? throw new InvalidOperationException("Salary advance settlement was not found.");
    }

    private void EnsureSettlementMonthIsAfterAdvance(int year, int month)
    {
        if (year < Year || (year == Year && month <= Month))
        {
            throw new InvalidOperationException("Salary advance settlements can only be recorded in later months.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
