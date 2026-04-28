using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;
using Payroll.Domain.SalaryCertificate;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.SalaryCertificate;

namespace Payroll.Application.Tests;

public sealed class SalaryCertificateRecordRepositorySqliteTests
{
    [Fact]
    public async Task GetLatestAsync_ReturnsLatestRecordPerEmployeeAndYear()
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

        var first = new SalaryCertificateRecord(employee.Id, 2026, "/tmp/first.pdf");
        await Task.Delay(5);
        var second = new SalaryCertificateRecord(employee.Id, 2026, "/tmp/second.pdf");
        dbContext.SalaryCertificateRecords.Add(first);
        dbContext.SalaryCertificateRecords.Add(second);
        await dbContext.SaveChangesAsync();

        var repository = new SalaryCertificateRecordRepository(dbContext);

        var latest = await repository.GetLatestAsync(employee.Id, 2026, CancellationToken.None);

        Assert.NotNull(latest);
        Assert.Equal("/tmp/second.pdf", latest!.OutputFilePath);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            "1000",
            "Anna",
            "Aktiv",
            new DateOnly(1990, 1, 1),
            new DateOnly(2025, 1, 1),
            null,
            true,
            new EmployeeAddress("Beispielstrasse", "1", null, "8000", "Bern", "Schweiz"),
            "Schweiz",
            "CH",
            "B",
            "Ordentlich",
            false,
            "756.0000.0000.00",
            "CH9300762011623852957",
            "+41 79 000 00 00",
            "anna.aktiv@example.ch",
            null,
            null,
            null,
            EmployeeWageType.Hourly);
    }
}
