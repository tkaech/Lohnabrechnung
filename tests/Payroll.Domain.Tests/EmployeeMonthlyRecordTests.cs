using Payroll.Domain.MonthlyRecords;

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
    public void SaveTimeEntry_UpdatesExistingDayInsteadOfCreatingDuplicate()
    {
        var record = new EmployeeMonthlyRecord(Guid.NewGuid(), 2026, 4);

        var created = record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, 10m, 11m, 12m, "Erster Stand");
        var updated = record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 7.5m, 0.5m, 0m, 0m, 20m, 21m, 22m, "Aktualisiert");

        Assert.Single(record.TimeEntries);
        Assert.Equal(created.Id, updated.Id);
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
}
