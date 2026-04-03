using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class EmployeeRepositorySqliteTests
{
    [Fact]
    public async Task SaveAndListAsync_WorkWithSQLiteWithoutApplyQueries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EmployeeRepository(dbContext);
        var saved = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null,
                "2000",
                "Demo",
                "Person",
                new DateOnly(1992, 5, 10),
                new DateOnly(2026, 1, 1),
                null,
                true,
                "Teststrasse",
                "4",
                null,
                "8000",
                "Zuerich",
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                "756.2000.2000.20",
                "CH4431999123000889020",
                "+41 79 200 20 20",
                "demo.person@demo-payroll.local",
                new DateOnly(2026, 1, 1),
                null,
                32.5m,
                280m,
                0.25m,
                null,
                null),
            CancellationToken.None);

        var listedEmployees = await repository.ListAsync(
            new EmployeeListQuery("Demo", true),
            CancellationToken.None);

        var listedEmployee = Assert.Single(listedEmployees);
        Assert.Equal(saved.EmployeeId, listedEmployee.EmployeeId);
        Assert.Equal("2000", listedEmployee.PersonnelNumber);
        Assert.Equal(32.5m, listedEmployee.HourlyRateChf);
        Assert.Equal(280m, listedEmployee.MonthlyBvgDeductionChf);
    }
}
