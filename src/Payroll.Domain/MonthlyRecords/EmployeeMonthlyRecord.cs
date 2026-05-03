using Payroll.Domain.Common;
using Payroll.Domain.Expenses;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.MonthlyRecords;

public sealed class EmployeeMonthlyRecord : AuditableEntity
{
    private readonly List<TimeEntry> _timeEntries = [];
    private readonly List<SalaryAdvance> _salaryAdvances = [];
    private readonly List<SalaryAdvanceSettlement> _salaryAdvanceSettlements = [];

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
    public decimal WithholdingTaxRatePercent { get; private set; }
    public decimal WithholdingTaxCorrectionAmountChf { get; private set; }
    public string? WithholdingTaxCorrectionText { get; private set; }
    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<SalaryAdvance> SalaryAdvances => _salaryAdvances.AsReadOnly();
    public IReadOnlyCollection<SalaryAdvanceSettlement> SalaryAdvanceSettlements => _salaryAdvanceSettlements.AsReadOnly();
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

    public void InitializeEmploymentContractSnapshot(Employees.EmploymentContract? contract)
    {
        if (contract is null || EmploymentContractSnapshot.IsInitialized)
        {
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

    public SalaryAdvance SaveSalaryAdvance(Guid? salaryAdvanceId, decimal amountChf, string? note)
    {
        var existingAdvance = ResolveSalaryAdvance(salaryAdvanceId);
        if (existingAdvance is null)
        {
            var createdAdvance = new SalaryAdvance(Id, EmployeeId, Year, Month, amountChf, note);
            _salaryAdvances.Add(createdAdvance);
            return createdAdvance;
        }

        existingAdvance.Update(amountChf, note);
        return existingAdvance;
    }

    public void RemoveSalaryAdvance(Guid salaryAdvanceId)
    {
        var advance = ResolveSalaryAdvance(salaryAdvanceId)
            ?? throw new InvalidOperationException("Salary advance was not found.");

        if (advance.Year != Year || advance.Month != Month)
        {
            throw new InvalidOperationException("Only salary advances from the current monthly record can be removed.");
        }

        if (advance.Settlements.Count > 0)
        {
            throw new InvalidOperationException("Salary advance cannot be removed once settlements exist.");
        }

        _salaryAdvances.Remove(advance);
        Touch();
    }

    public void RegisterSalaryAdvanceSettlement(SalaryAdvanceSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        if (settlement.EmployeeMonthlyRecordId != Id)
        {
            throw new InvalidOperationException("Settlement does not belong to this monthly record.");
        }

        if (settlement.EmployeeId != EmployeeId || settlement.Year != Year || settlement.Month != Month)
        {
            throw new InvalidOperationException("Settlement metadata does not match the target monthly record.");
        }

        if (_salaryAdvanceSettlements.All(item => item.Id != settlement.Id))
        {
            _salaryAdvanceSettlements.Add(settlement);
        }
    }

    public void SaveWithholdingTaxInputs(decimal ratePercent, decimal correctionAmountChf, string? correctionText)
    {
        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent), "Rate must be between 0 and 100.");
        }

        WithholdingTaxRatePercent = ratePercent;
        WithholdingTaxCorrectionAmountChf = correctionAmountChf;
        WithholdingTaxCorrectionText = string.IsNullOrWhiteSpace(correctionText) ? null : correctionText.Trim();
        Touch();
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

    private SalaryAdvance? ResolveSalaryAdvance(Guid? salaryAdvanceId)
    {
        if (!salaryAdvanceId.HasValue)
        {
            return null;
        }

        return _salaryAdvances.SingleOrDefault(item => item.Id == salaryAdvanceId.Value)
            ?? throw new InvalidOperationException("Salary advance was not found.");
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
