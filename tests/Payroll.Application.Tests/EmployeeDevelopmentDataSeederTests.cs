using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class EmployeeDevelopmentDataSeederTests
{
    [Fact]
    public void Seed_CreatesTenDemoEmployeesOnlyOnce()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        using var dbContext = new PayrollDbContext(options);
        dbContext.Database.EnsureCreated();

        EmployeeDevelopmentDataSeeder.Seed(dbContext);
        EmployeeDevelopmentDataSeeder.Seed(dbContext);

        Assert.Equal(10, dbContext.Employees.Count());
        Assert.Equal(10, dbContext.EmploymentContracts.Count());
        Assert.Equal(2, dbContext.Employees.Count(employee => !employee.IsActive));
        Assert.Contains(dbContext.Employees, employee => employee.PermitCode == "B");
        Assert.Contains(dbContext.Employees, employee => employee.IsSubjectToWithholdingTax == true);
    }
}
