using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.MonthlyRecords;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.MonthlyRecords;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class MonthlyRecordRepositorySqliteTests
{
    [Fact]
    public async Task GetOrCreateAsync_EnforcesSingleMonthlyRecordPerEmployeeAndMonth()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var first = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var second = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task SaveAndLoadDetailsAsync_PersistsTimeAndExpenseEntriesWithinMonthlyContext()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, "Fruehdienst");
        record.SaveExpenseEntry(null, new DateOnly(2026, 4, 10), 18.50m);
        record.SaveVehicleCompensation(null, new DateOnly(2026, 4, 30), 120m, "Auto April");
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Single(details!.TimeEntries);
        Assert.Single(details.ExpenseEntries);
        Assert.Single(details.VehicleCompensations);
        Assert.Equal(8m, details.Header.TotalWorkedHours);
        Assert.Equal(18.50m, details.Header.TotalExpensesChf);
        Assert.Equal(120m, details.Header.TotalVehicleCompensationChf);
    }

    [Fact]
    public async Task ExpenseEntries_AllowOnlySingleMonthlyTotalPerRecord()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        var monthlyRecord = new EmployeeMonthlyRecord(employee.Id, 2026, 4);
        dbContext.EmployeeMonthlyRecords.Add(monthlyRecord);
        await dbContext.SaveChangesAsync();

        dbContext.ExpenseEntries.Add(new global::Payroll.Domain.Expenses.ExpenseEntry(monthlyRecord.Id, employee.Id, new DateOnly(2026, 4, 10), 18.50m));
        dbContext.ExpenseEntries.Add(new global::Payroll.Domain.Expenses.ExpenseEntry(monthlyRecord.Id, employee.Id, new DateOnly(2026, 4, 30), 80m));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateMonthlyRecordPerEmployeeAndMonth()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        dbContext.EmployeeMonthlyRecords.Add(new EmployeeMonthlyRecord(employee.Id, 2026, 4));
        dbContext.EmployeeMonthlyRecords.Add(new EmployeeMonthlyRecord(employee.Id, 2026, 4));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task MonthlyRecordViewModel_SaveTimeEntry_PersistsAgainstSqliteRepository()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var service = new MonthlyRecordService(repository);
        var viewModel = new MonthlyRecordViewModel(service)
        {
            SelectedMonth = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employee.Id, "Anna Aktiv");
        viewModel.TimeDate = "2026-04-05";
        viewModel.HoursWorked = "8";
        viewModel.NightHours = "1";
        viewModel.SundayHours = "0";
        viewModel.HolidayHours = "0";
        viewModel.TimeNote = "Fruehdienst";

        viewModel.SaveTimeEntryCommand.Execute(null);

        var startedAt = DateTime.UtcNow;
        while (viewModel.TimeEntries.Count == 0 && (DateTime.UtcNow - startedAt).TotalSeconds < 3)
        {
            await Task.Delay(20);
        }

        Assert.True(viewModel.TimeEntries.Count == 1, viewModel.ActionMessage);
        Assert.Equal("Zeiteintrag gespeichert.", viewModel.ActionMessage);
    }

    private static global::Payroll.Domain.Employees.Employee CreateEmployee()
    {
        return new global::Payroll.Domain.Employees.Employee(
            "9000",
            "Anna",
            "Aktiv",
            new DateOnly(1990, 2, 1),
            new DateOnly(2025, 1, 1),
            null,
            true,
            new global::Payroll.Domain.Employees.EmployeeAddress("Teststrasse", "1", null, "8000", "Zuerich", "Schweiz"),
            "Schweiz",
            "CH",
            "B",
            "Ordentlich",
            false,
            "756.1234.5678.90",
            "CH9300762011623852957",
            "+41 79 123 45 67",
            "anna.aktiv@example.ch");
    }
}
