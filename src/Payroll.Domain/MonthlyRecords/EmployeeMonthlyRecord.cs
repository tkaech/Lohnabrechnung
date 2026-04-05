using Payroll.Domain.Common;
using Payroll.Domain.Expenses;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.MonthlyRecords;

public sealed class EmployeeMonthlyRecord : AuditableEntity
{
    private readonly List<TimeEntry> _timeEntries = [];
    private readonly List<ExpenseEntry> _expenseEntries = [];
    private readonly List<VehicleCompensation> _vehicleCompensations = [];

    private EmployeeMonthlyRecord()
    {
        Status = EmployeeMonthlyRecordStatus.Draft;
    }

    public EmployeeMonthlyRecord(Guid employeeId, int year, int month)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(employeeId));
        }

        EmployeeId = employeeId;
        ValidateMonth(year, month);
        Year = year;
        Month = month;
        Status = EmployeeMonthlyRecordStatus.Draft;
    }

    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public EmployeeMonthlyRecordStatus Status { get; private set; }
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<ExpenseEntry> ExpenseEntries => _expenseEntries.AsReadOnly();
    public IReadOnlyCollection<VehicleCompensation> VehicleCompensations => _vehicleCompensations.AsReadOnly();
    public DateOnly PeriodStart => new(Year, Month, 1);
    public DateOnly PeriodEnd => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public bool ContainsDate(DateOnly date)
    {
        return date.Year == Year && date.Month == Month;
    }

    public void SetStatus(EmployeeMonthlyRecordStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        Touch();
    }

    public TimeEntry SaveTimeEntry(
        Guid? timeEntryId,
        DateOnly workDate,
        decimal hoursWorked,
        decimal nightHours,
        decimal sundayHours,
        decimal holidayHours,
        string? note)
    {
        EnsureDateInMonth(workDate, nameof(workDate));

        var existingEntry = ResolveTimeEntry(timeEntryId, workDate);
        if (existingEntry is null)
        {
            var createdEntry = new TimeEntry(Id, EmployeeId, workDate, hoursWorked, nightHours, sundayHours, holidayHours, note);
            _timeEntries.Add(createdEntry);
            return createdEntry;
        }

        EnsureNoOtherTimeEntryUsesDate(existingEntry.Id, workDate);
        existingEntry.Update(workDate, hoursWorked, nightHours, sundayHours, holidayHours, note);
        return existingEntry;
    }

    public void RemoveTimeEntry(Guid timeEntryId)
    {
        var timeEntry = _timeEntries.SingleOrDefault(item => item.Id == timeEntryId)
            ?? throw new InvalidOperationException("Time entry was not found.");

        _timeEntries.Remove(timeEntry);
    }

    public ExpenseEntry SaveExpenseEntry(
        Guid? expenseEntryId,
        DateOnly expenseDate,
        decimal amountChf)
    {
        EnsureDateInMonth(expenseDate, nameof(expenseDate));

        var existingEntry = ResolveExpenseEntry(expenseEntryId);

        if (existingEntry is null)
        {
            var createdEntry = new ExpenseEntry(Id, EmployeeId, expenseDate, amountChf);
            _expenseEntries.Add(createdEntry);
            return createdEntry;
        }

        existingEntry.Update(expenseDate, amountChf);
        return existingEntry;
    }

    public void RemoveExpenseEntry(Guid expenseEntryId)
    {
        var expenseEntry = _expenseEntries.SingleOrDefault(item => item.Id == expenseEntryId)
            ?? throw new InvalidOperationException("Expense entry was not found.");

        _expenseEntries.Remove(expenseEntry);
    }

    public VehicleCompensation SaveVehicleCompensation(
        Guid? vehicleCompensationId,
        DateOnly compensationDate,
        decimal amountChf,
        string description)
    {
        EnsureDateInMonth(compensationDate, nameof(compensationDate));

        var existingEntry = vehicleCompensationId.HasValue
            ? _vehicleCompensations.SingleOrDefault(item => item.Id == vehicleCompensationId.Value)
                ?? throw new InvalidOperationException("Vehicle compensation entry was not found.")
            : null;

        if (existingEntry is null)
        {
            var createdEntry = new VehicleCompensation(Id, EmployeeId, compensationDate, amountChf, description);
            _vehicleCompensations.Add(createdEntry);
            return createdEntry;
        }

        existingEntry.Update(compensationDate, amountChf, description);
        return existingEntry;
    }

    public void RemoveVehicleCompensation(Guid vehicleCompensationId)
    {
        var vehicleCompensation = _vehicleCompensations.SingleOrDefault(item => item.Id == vehicleCompensationId)
            ?? throw new InvalidOperationException("Vehicle compensation entry was not found.");

        _vehicleCompensations.Remove(vehicleCompensation);
    }

    private TimeEntry? ResolveTimeEntry(Guid? timeEntryId, DateOnly workDate)
    {
        if (timeEntryId.HasValue)
        {
            return _timeEntries.SingleOrDefault(item => item.Id == timeEntryId.Value)
                ?? throw new InvalidOperationException("Time entry was not found.");
        }

        return _timeEntries.SingleOrDefault(item => item.WorkDate == workDate);
    }

    private ExpenseEntry? ResolveExpenseEntry(Guid? expenseEntryId)
    {
        if (expenseEntryId.HasValue)
        {
            return _expenseEntries.SingleOrDefault(item => item.Id == expenseEntryId.Value)
                ?? throw new InvalidOperationException("Expense entry was not found.");
        }

        return _expenseEntries.SingleOrDefault();
    }

    private void EnsureNoOtherTimeEntryUsesDate(Guid currentTimeEntryId, DateOnly workDate)
    {
        if (_timeEntries.Any(item => item.Id != currentTimeEntryId && item.WorkDate == workDate))
        {
            throw new InvalidOperationException("Only one time entry per employee, month and date is allowed.");
        }
    }

    private void EnsureDateInMonth(DateOnly date, string paramName)
    {
        if (!ContainsDate(date))
        {
            throw new ArgumentOutOfRangeException(paramName, "Date must be within the selected payroll month.");
        }
    }

    private static void ValidateMonth(int year, int month)
    {
        _ = new DateOnly(year, month, 1);
    }
}
