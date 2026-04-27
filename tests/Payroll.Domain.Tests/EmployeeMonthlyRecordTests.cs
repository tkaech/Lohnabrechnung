using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Settings;

namespace Payroll.Domain.Tests;

public sealed class EmployeeMonthlyRecordTests
{
    [Fact]
    public void Constructor_CreatesMonthlyContextForEmployeeAndMonth()
    {
        var employeeId = Guid.NewGuid();

        var record = new EmployeeMonthlyRecord(employeeId, 2026, 4);

        Assert.Equal(employeeId, record.EmployeeId);
        Assert.Equal(2026, record.Year);
        Assert.Equal(4, record.Month);
        Assert.Equal(EmployeeMonthlyRecordStatus.Draft, record.Status);
        Assert.False(record.PayrollParameterSnapshot.IsInitialized);
        Assert.Equal(new DateOnly(2026, 4, 1), record.PeriodStart);
        Assert.Equal(new DateOnly(2026, 4, 30), record.PeriodEnd);
    }

    [Fact]
    public void SaveTimeEntry_RejectsDatesOutsidePayrollMonth()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            record.SaveTimeEntry(null, new DateOnly(2026, 5, 1), 8m, 0m, 0m, 0m, 0m, 0m, 0m, null));
    }

    [Fact]
    public void SaveTimeEntry_UpdatesExistingMonthlyEntryInsteadOfCreatingDuplicate()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);

        var created = record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, 10m, 11m, 12m, "Erster Stand");
        var updated = record.SaveTimeEntry(null, new DateOnly(2026, 4, 18), 7.5m, 0.5m, 0m, 0m, 20m, 21m, 22m, "Aktualisiert");

        Assert.Single(record.TimeEntries);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(new DateOnly(2026, 4, 18), updated.WorkDate);
        Assert.Equal(7.5m, updated.HoursWorked);
        Assert.Equal(20m, updated.VehiclePauschalzone1Chf);
        Assert.Equal("Aktualisiert", updated.Note);
    }

    [Fact]
    public void SaveExpenseEntry_StoresExactlyOneMonthlyExpenseBlock()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);

        var created = record.SaveExpenseEntry(24.50m);
        var updated = record.SaveExpenseEntry(80m);

        Assert.NotNull(record.ExpenseEntry);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(80m, updated.ExpensesTotalChf);
    }

    [Fact]
    public void InitializePayrollParameterSnapshot_CapturesRatesOnlyOnce()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);
        var originalSettings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m);
        var laterSettings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.40m, 0.80m, 1.20m),
            ahvIvEoRate: 0.06m,
            alvRate: 0.02m,
            sicknessAccidentInsuranceRate: 0.01m,
            trainingAndHolidayRate: 0.0003m,
            vacationCompensationRate: 0.12m,
            vacationCompensationRateAge50Plus: 0.14m,
            vehiclePauschalzone1RateChf: 8m,
            vehiclePauschalzone2RateChf: 18m,
            vehicleRegiezone1RateChf: 1m);

        record.InitializePayrollParameterSnapshot(originalSettings);
        var firstCapturedAt = record.PayrollParameterSnapshot.CapturedAtUtc;

        record.InitializePayrollParameterSnapshot(laterSettings);

        Assert.True(record.PayrollParameterSnapshot.IsInitialized);
        Assert.Equal(firstCapturedAt, record.PayrollParameterSnapshot.CapturedAtUtc);
        Assert.Equal(0.25m, record.PayrollParameterSnapshot.NightSupplementRate);
        Assert.Equal(0.1064m, record.PayrollParameterSnapshot.VacationCompensationRate);
        Assert.Equal(0.1264m, record.PayrollParameterSnapshot.VacationCompensationRateAge50Plus);
        Assert.Equal(5.6m, record.PayrollParameterSnapshot.VehiclePauschalzone1RateChf);
    }

    [Fact]
    public void InitializeEmploymentContractSnapshot_CapturesContractOnlyOnce()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);
        var originalContract = new EmploymentContract(record.EmployeeId, new DateOnly(2026, 1, 1), null, 33.5m, 310m, 3m, monthlySalaryAmountChf: 4800m);
        var laterContract = new EmploymentContract(record.EmployeeId, new DateOnly(2026, 6, 1), null, 39m, 450m, 5m);

        record.InitializeEmploymentContractSnapshot(originalContract);
        var firstCapturedAt = record.EmploymentContractSnapshot.CapturedAtUtc;

        record.InitializeEmploymentContractSnapshot(laterContract);

        Assert.True(record.EmploymentContractSnapshot.IsInitialized);
        Assert.Equal(firstCapturedAt, record.EmploymentContractSnapshot.CapturedAtUtc);
        Assert.Equal(new DateOnly(2026, 1, 1), record.EmploymentContractSnapshot.ValidFrom);
        Assert.Equal(33.5m, record.EmploymentContractSnapshot.HourlyRateChf);
        Assert.Equal(4800m, record.EmploymentContractSnapshot.MonthlySalaryAmountChf);
        Assert.Equal(310m, record.EmploymentContractSnapshot.MonthlyBvgDeductionChf);
        Assert.Equal(3m, record.EmploymentContractSnapshot.SpecialSupplementRateChf);
    }
}
