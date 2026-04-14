using Payroll.Domain.Common;
using Payroll.Domain.Expenses;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.MonthlyRecords;

public sealed class EmployeeMonthlyRecord : AuditableEntity
{
    private readonly List<TimeEntry> _timeEntries = [];

    private EmployeeMonthlyRecord()
    {
        Status = EmployeeMonthlyRecordStatus.Draft;
        PayrollParameterSnapshot = new PayrollParameterSnapshot();
        EmploymentContractSnapshot = new EmploymentContractSnapshot();
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
        PayrollParameterSnapshot = new PayrollParameterSnapshot();
        EmploymentContractSnapshot = new EmploymentContractSnapshot();
    }

    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public EmployeeMonthlyRecordStatus Status { get; private set; }
    public PayrollParameterSnapshot PayrollParameterSnapshot { get; private set; }
    public EmploymentContractSnapshot EmploymentContractSnapshot { get; private set; }
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public ExpenseEntry? ExpenseEntry { get; private set; }
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

    public void InitializePayrollParameterSnapshot(Settings.PayrollSettings payrollSettings)
    {
        ArgumentNullException.ThrowIfNull(payrollSettings);

        if (PayrollParameterSnapshot.IsInitialized)
        {
            return;
        }

        PayrollParameterSnapshot = PayrollParameterSnapshot.Create(payrollSettings);
        Touch();
    }

    public void RefreshPayrollParameterSnapshot(Settings.PayrollSettings payrollSettings)
    {
        ArgumentNullException.ThrowIfNull(payrollSettings);
        PayrollParameterSnapshot = PayrollParameterSnapshot.Create(payrollSettings);
        Touch();
    }

    public void InitializeEmploymentContractSnapshot(Employees.EmploymentContract? contract)
    {
        if (contract is null || EmploymentContractSnapshot.IsInitialized)
        {
            return;
        }

        EmploymentContractSnapshot = EmploymentContractSnapshot.Create(contract);
        Touch();
    }

    public void RefreshEmploymentContractSnapshot(Employees.EmploymentContract? contract)
    {
        if (contract is null)
        {
            EmploymentContractSnapshot = new EmploymentContractSnapshot();
            Touch();
            return;
        }

        EmploymentContractSnapshot = EmploymentContractSnapshot.Create(contract);
        Touch();
    }

    public TimeEntry SaveTimeEntry(
        Guid? timeEntryId,
        DateOnly workDate,
        decimal hoursWorked,
        decimal nightHours,
        decimal sundayHours,
        decimal holidayHours,
        decimal vehiclePauschalzone1Chf,
        decimal vehiclePauschalzone2Chf,
        decimal vehicleRegiezone1Chf,
        string? note)
    {
        EnsureDateInMonth(workDate, nameof(workDate));

        var existingEntry = ResolveTimeEntry(timeEntryId);
        if (existingEntry is null)
        {
            var createdEntry = new TimeEntry(
                Id,
                EmployeeId,
                workDate,
                hoursWorked,
                nightHours,
                sundayHours,
                holidayHours,
                note,
                vehiclePauschalzone1Chf,
                vehiclePauschalzone2Chf,
                vehicleRegiezone1Chf);
            _timeEntries.Add(createdEntry);
            return createdEntry;
        }

        EnsureNoOtherTimeEntryExists(existingEntry.Id);
        existingEntry.Update(
            workDate,
            hoursWorked,
            nightHours,
            sundayHours,
            holidayHours,
            note,
            vehiclePauschalzone1Chf,
            vehiclePauschalzone2Chf,
            vehicleRegiezone1Chf);
        return existingEntry;
    }

    public void RemoveTimeEntry(Guid timeEntryId)
    {
        var timeEntry = _timeEntries.SingleOrDefault(item => item.Id == timeEntryId)
            ?? throw new InvalidOperationException("Time entry was not found.");

        _timeEntries.Remove(timeEntry);
    }

    public ExpenseEntry SaveExpenseEntry(decimal expensesTotalChf)
    {
        if (ExpenseEntry is null)
        {
            var createdEntry = new ExpenseEntry(Id, EmployeeId, expensesTotalChf);
            ExpenseEntry = createdEntry;
            return createdEntry;
        }

        ExpenseEntry.Update(expensesTotalChf);
        return ExpenseEntry;
    }

    private TimeEntry? ResolveTimeEntry(Guid? timeEntryId)
    {
        if (timeEntryId.HasValue)
        {
            return _timeEntries.SingleOrDefault(item => item.Id == timeEntryId.Value)
                ?? throw new InvalidOperationException("Time entry was not found.");
        }

        return _timeEntries.Count switch
        {
            0 => null,
            1 => _timeEntries[0],
            _ => throw new InvalidOperationException("Only one time entry per employee and month is allowed.")
        };
    }

    private void EnsureNoOtherTimeEntryExists(Guid currentTimeEntryId)
    {
        if (_timeEntries.Any(item => item.Id != currentTimeEntryId))
        {
            throw new InvalidOperationException("Only one time entry per employee and month is allowed.");
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
