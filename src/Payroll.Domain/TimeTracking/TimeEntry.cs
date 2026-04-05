using Payroll.Domain.Common;

namespace Payroll.Domain.TimeTracking;

public sealed class TimeEntry : AuditableEntity
{
    private TimeEntry()
    {
    }

    public Guid EmployeeMonthlyRecordId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public decimal HoursWorked { get; private set; }
    public decimal NightHours { get; private set; }
    public decimal SundayHours { get; private set; }
    public decimal HolidayHours { get; private set; }
    public string? Note { get; private set; }
    public decimal SupplementHours => NightHours + SundayHours + HolidayHours;
    public decimal TotalHours => HoursWorked;

    public TimeEntry(
        Guid employeeId,
        DateOnly workDate,
        decimal hoursWorked,
        decimal nightHours = 0m,
        decimal sundayHours = 0m,
        decimal holidayHours = 0m,
        string? note = null)
        : this(Guid.Empty, employeeId, workDate, hoursWorked, nightHours, sundayHours, holidayHours, note)
    {
    }

    public TimeEntry(
        Guid employeeMonthlyRecordId,
        Guid employeeId,
        DateOnly workDate,
        decimal hoursWorked,
        decimal nightHours = 0m,
        decimal sundayHours = 0m,
        decimal holidayHours = 0m,
        string? note = null)
    {
        EmployeeMonthlyRecordId = employeeMonthlyRecordId;
        EmployeeId = employeeId;
        Update(workDate, hoursWorked, nightHours, sundayHours, holidayHours, note);
    }

    public void Update(
        DateOnly workDate,
        decimal hoursWorked,
        decimal nightHours = 0m,
        decimal sundayHours = 0m,
        decimal holidayHours = 0m,
        string? note = null)
    {
        WorkDate = workDate;
        HoursWorked = Guard.AgainstNegative(hoursWorked, nameof(hoursWorked));
        NightHours = Guard.AgainstNegative(nightHours, nameof(nightHours));
        SundayHours = Guard.AgainstNegative(sundayHours, nameof(sundayHours));
        HolidayHours = Guard.AgainstNegative(holidayHours, nameof(holidayHours));
        Note = NormalizeOptional(note);

        if (NightHours > HoursWorked)
        {
            throw new ArgumentOutOfRangeException(nameof(nightHours), "Night hours cannot exceed total work hours.");
        }

        if (SundayHours > HoursWorked)
        {
            throw new ArgumentOutOfRangeException(nameof(sundayHours), "Sunday hours cannot exceed total work hours.");
        }

        if (HolidayHours > HoursWorked)
        {
            throw new ArgumentOutOfRangeException(nameof(holidayHours), "Holiday hours cannot exceed total work hours.");
        }

        Touch();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
