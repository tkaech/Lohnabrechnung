using Payroll.Domain.Common;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.Payroll;

public sealed class PayrollWorkSummary
{
    public PayrollWorkSummary(
        Guid employeeId,
        decimal workHours,
        decimal nightHours,
        decimal sundayHours,
        decimal holidayHours)
    {
        EmployeeId = employeeId;
        WorkHours = Guard.AgainstNegative(workHours, nameof(workHours));
        NightHours = Guard.AgainstNegative(nightHours, nameof(nightHours));
        SundayHours = Guard.AgainstNegative(sundayHours, nameof(sundayHours));
        HolidayHours = Guard.AgainstNegative(holidayHours, nameof(holidayHours));

        if (NightHours > WorkHours)
        {
            throw new ArgumentOutOfRangeException(nameof(nightHours), "Night hours cannot exceed total work hours.");
        }

        if (SundayHours > WorkHours)
        {
            throw new ArgumentOutOfRangeException(nameof(sundayHours), "Sunday hours cannot exceed total work hours.");
        }

        if (HolidayHours > WorkHours)
        {
            throw new ArgumentOutOfRangeException(nameof(holidayHours), "Holiday hours cannot exceed total work hours.");
        }
    }

    public Guid EmployeeId { get; }
    public decimal WorkHours { get; }
    public decimal NightHours { get; }
    public decimal SundayHours { get; }
    public decimal HolidayHours { get; }
    public decimal SpecialHours => NightHours + SundayHours + HolidayHours;

    public static PayrollWorkSummary FromTimeEntries(Guid employeeId, IEnumerable<TimeEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var timeEntries = entries.ToList();
        foreach (var entry in timeEntries)
        {
            if (entry.EmployeeId != employeeId)
            {
                throw new ArgumentException("All time entries must belong to the same employee.", nameof(entries));
            }
        }

        return new PayrollWorkSummary(
            employeeId,
            timeEntries.Sum(entry => entry.HoursWorked),
            timeEntries.Sum(entry => entry.NightHours),
            timeEntries.Sum(entry => entry.SundayHours),
            timeEntries.Sum(entry => entry.HolidayHours));
    }
}
