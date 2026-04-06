using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.MonthlyRecords;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Settings;
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
    public async Task SaveAndLoadDetailsAsync_PersistsTimeVehicleAndExpenseValuesWithinMonthlyContext()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            0.053m,
            0.011m,
            0.00821m,
            0.00015m,
            0.1064m,
            5.6m,
            16.8m,
            0.32m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, 120m, 80m, 60m, "Fruehdienst");
        record.SaveExpenseEntry(18.50m);
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Single(details!.TimeEntries);
        Assert.NotNull(details.ExpenseEntry);
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Basislohn" && line.AmountDisplay == "260.00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Ferienentschaedigung" && line.AmountDisplay == "247.63 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Spezialzuschlag gemaess Vertrag" && line.AmountDisplay == "24.00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Fahrzeitentschaedigung Pauschalzone 1" && line.AmountDisplay == "672.00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Spesen gemaess Nachweis" && line.AmountDisplay == "18.50 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Total Auszahlung");
        Assert.Equal(8m, details.Header.TotalWorkedHours);
        Assert.Equal(18.50m, details.Header.TotalExpensesChf);
        Assert.Equal(260m, details.Header.TotalVehicleCompensationChf);
        Assert.Equal(120m, details.TimeEntries.Single().VehiclePauschalzone1Chf);
    }

    [Fact]
    public async Task ExpenseEntries_ModelEnforcesUniqueMonthlyRecordReference()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var entityType = dbContext.Model.FindEntityType(typeof(global::Payroll.Domain.Expenses.ExpenseEntry));
        var uniqueForeignKeyIndex = entityType!
            .GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(global::Payroll.Domain.Expenses.ExpenseEntry.EmployeeMonthlyRecordId));

        Assert.True(uniqueForeignKeyIndex.IsUnique);
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
    public async Task MonthlyRecordViewModel_SaveExpenseEntry_PersistsAgainstSqliteRepository()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var service = new MonthlyRecordService(repository);
        var viewModel = new MonthlyRecordViewModel(service)
        {
            SelectedMonth = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employee.Id, "Anna Aktiv");
        viewModel.ExpensesTotal = "18.5";

        viewModel.SaveExpenseEntryCommand.Execute(null);

        var startedAt = DateTime.UtcNow;
        while (viewModel.ActionMessage != "Spesen gespeichert." && (DateTime.UtcNow - startedAt).TotalSeconds < 3)
        {
            await Task.Delay(20);
        }

        Assert.Equal("Spesen gespeichert.", viewModel.ActionMessage);
        Assert.Equal("18.50", viewModel.ExpensesTotal);
        Assert.Contains("Spesen 18.50 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
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
