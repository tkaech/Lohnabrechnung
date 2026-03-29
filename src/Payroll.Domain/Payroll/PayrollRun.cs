using Payroll.Domain.Common;

namespace Payroll.Domain.Payroll;

public sealed class PayrollRun : Entity
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateOnly CreatedOn { get; private set; }
    public PayrollRunStatus Status { get; private set; } = PayrollRunStatus.Draft;
    public List<PayrollEntry> Entries { get; private set; } = new();

    private PayrollRun()
    {
    }

    public PayrollRun(int year, int month, DateOnly createdOn)
    {
        Year = year;
        Month = month;
        CreatedOn = createdOn;
    }

    public void MarkCalculated()
    {
        Status = PayrollRunStatus.Calculated;
    }
}
