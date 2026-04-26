using Payroll.Domain.Common;

namespace Payroll.Domain.Payroll;

public sealed class PayrollRun : AuditableEntity
{
    private readonly List<PayrollRunLine> _lines = [];

    private PayrollRun()
    {
        PeriodKey = string.Empty;
    }

    public string PeriodKey { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public IReadOnlyCollection<PayrollRunLine> Lines => _lines.AsReadOnly();

    public PayrollRun(string periodKey, DateOnly paymentDate)
    {
        PeriodKey = Guard.AgainstNullOrWhiteSpace(periodKey, nameof(periodKey));
        PaymentDate = paymentDate;
        Status = PayrollRunStatus.Draft;
    }

    public void AddLine(PayrollRunLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (Status is PayrollRunStatus.Finalized or PayrollRunStatus.Cancelled)
        {
            throw new InvalidOperationException("Finalized payroll runs cannot be modified.");
        }

        _lines.Add(line);
        Touch();
    }

    public void AddLines(IEnumerable<PayrollRunLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        foreach (var line in lines)
        {
            AddLine(line);
        }
    }

    public decimal GetTotalAmountChf()
    {
        return _lines.Sum(line => line.AmountChf);
    }

    public decimal GetNetAmountChfForEmployee(Guid employeeId)
    {
        return _lines
            .Where(line => line.EmployeeId == employeeId)
            .Sum(line => line.AmountChf);
    }

    public decimal GetTotalHoursForEmployee(Guid employeeId)
    {
        return _lines
            .Where(line => line.EmployeeId == employeeId && line.Unit == PayrollLineUnit.Hours)
            .Sum(line => line.Quantity ?? 0m);
    }

    public void FinalizeRun()
    {
        if (Status == PayrollRunStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled payroll runs cannot be finalized.");
        }

        Status = PayrollRunStatus.Finalized;
        Touch();
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (Status == PayrollRunStatus.Cancelled)
        {
            throw new InvalidOperationException("Payroll run is already cancelled.");
        }

        if (Status != PayrollRunStatus.Finalized)
        {
            throw new InvalidOperationException("Only finalized payroll runs can be cancelled.");
        }

        Status = PayrollRunStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        Touch();
    }
}
