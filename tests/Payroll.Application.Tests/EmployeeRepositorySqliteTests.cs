using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Domain.Employees;
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

        var department = new global::Payroll.Domain.Settings.DepartmentOption("Sicherheit");
        var category = new global::Payroll.Domain.Settings.EmploymentCategoryOption("A");
        var location = new global::Payroll.Domain.Settings.EmploymentLocationOption("Schachenstr. 7, Emmenbruecke");
        dbContext.DepartmentOptions.Add(department);
        dbContext.EmploymentCategoryOptions.Add(category);
        dbContext.EmploymentLocationOptions.Add(location);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeRepository(dbContext);
        var saved = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null,
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
                department.Id,
                category.Id,
                location.Id,
                EmployeeWageType.Monthly,
                new DateOnly(2026, 1, 1),
                null,
                32.5m,
                280m,
                3.00m),
            CancellationToken.None);

        var listedEmployees = await repository.ListAsync(
            new EmployeeListQuery("Demo", true),
            CancellationToken.None);

        var listedEmployee = Assert.Single(listedEmployees);
        Assert.Equal(saved.EmployeeId, listedEmployee.EmployeeId);
        Assert.Equal("2000", listedEmployee.PersonnelNumber);
        Assert.Equal(32.5m, listedEmployee.HourlyRateChf);
        Assert.Equal(280m, listedEmployee.MonthlyBvgDeductionChf);
        Assert.Equal("Sicherheit", saved.DepartmentName);
        Assert.Equal("A", saved.EmploymentCategoryName);
        Assert.Equal("Schachenstr. 7, Emmenbruecke", saved.EmploymentLocationName);
        Assert.Equal(EmployeeWageType.Monthly, saved.WageType);
        Assert.Equal(3.00m, saved.SpecialSupplementRateChf);
    }

    [Fact]
    public async Task SaveAsync_RejectsContractPeriodsThatOverlapWithLaterVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EmployeeRepository(dbContext);
        var employee = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null,
                null,
                "3000",
                "Nina",
                "Vertrag",
                null,
                new DateOnly(2026, 1, 1),
                null,
                true,
                "Teststrasse",
                "1",
                null,
                "6000",
                "Luzern",
                "Schweiz",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                EmployeeWageType.Hourly,
                new DateOnly(2026, 1, 1),
                null,
                30m,
                250m,
                2.50m),
            CancellationToken.None);

        await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId,
                null,
                employee.PersonnelNumber,
                employee.FirstName,
                employee.LastName,
                employee.BirthDate,
                employee.EntryDate,
                employee.ExitDate,
                employee.IsActive,
                employee.Street,
                employee.HouseNumber,
                employee.AddressLine2,
                employee.PostalCode,
                employee.City,
                employee.Country,
                employee.ResidenceCountry,
                employee.Nationality,
                employee.PermitCode,
                employee.TaxStatus,
                employee.IsSubjectToWithholdingTax,
                employee.AhvNumber,
                employee.Iban,
                employee.PhoneNumber,
                employee.Email,
                employee.DepartmentOptionId,
                employee.EmploymentCategoryOptionId,
                employee.EmploymentLocationOptionId,
                employee.WageType,
                new DateOnly(2026, 3, 1),
                null,
                31m,
                255m,
                2.75m),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveAsync(
                new SaveEmployeeCommand(
                    employee.EmployeeId,
                    null,
                    employee.PersonnelNumber,
                    employee.FirstName,
                    employee.LastName,
                    employee.BirthDate,
                    employee.EntryDate,
                    employee.ExitDate,
                    employee.IsActive,
                    employee.Street,
                    employee.HouseNumber,
                    employee.AddressLine2,
                    employee.PostalCode,
                    employee.City,
                    employee.Country,
                    employee.ResidenceCountry,
                    employee.Nationality,
                    employee.PermitCode,
                    employee.TaxStatus,
                    employee.IsSubjectToWithholdingTax,
                    employee.AhvNumber,
                    employee.Iban,
                    employee.PhoneNumber,
                    employee.Email,
                    employee.DepartmentOptionId,
                    employee.EmploymentCategoryOptionId,
                    employee.EmploymentLocationOptionId,
                    employee.WageType,
                    new DateOnly(2026, 2, 1),
                    new DateOnly(2026, 3, 31),
                    32m,
                    260m,
                    3.00m),
                CancellationToken.None));

        Assert.Contains("ueberlappt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteContractVersionAsync_RejectsActiveVersion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EmployeeRepository(dbContext);
        var employee = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null, null, "4000", "Mia", "Test", null, new DateOnly(2026, 1, 1), null, true,
                "Teststrasse", "1", null, "6000", "Luzern", "Schweiz",
                null, null, null, null, null, null, null, null, null, null, null, null,
                EmployeeWageType.Hourly, new DateOnly(2026, 1, 1), null, 30m, 250m, 2.5m),
            CancellationToken.None);

        var activeVersion = Assert.Single(employee.ContractHistory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.DeleteContractVersionAsync(employee.EmployeeId, activeVersion.ContractId, CancellationToken.None));

        Assert.Contains("aktive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithSingleVersion_AllowsBackwardAdjustmentOfCurrentContract()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EmployeeRepository(dbContext);
        var employee = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null, null, "5000", "Ella", "Rueck", null, new DateOnly(2026, 1, 1), null, true,
                "Teststrasse", "1", null, "6000", "Luzern", "Schweiz",
                null, null, null, null, null, null, null, null, null, null, null, null,
                EmployeeWageType.Hourly, new DateOnly(2026, 4, 1), null, 30m, 250m, 2.5m),
            CancellationToken.None);

        var currentContract = Assert.Single(employee.ContractHistory);

        var updated = await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId, currentContract.ContractId, employee.PersonnelNumber, employee.FirstName, employee.LastName,
                employee.BirthDate, employee.EntryDate, employee.ExitDate, employee.IsActive,
                employee.Street, employee.HouseNumber, employee.AddressLine2, employee.PostalCode, employee.City, employee.Country,
                employee.ResidenceCountry, employee.Nationality, employee.PermitCode, employee.TaxStatus, employee.IsSubjectToWithholdingTax,
                employee.AhvNumber, employee.Iban, employee.PhoneNumber, employee.Email, employee.DepartmentOptionId,
                employee.EmploymentCategoryOptionId, employee.EmploymentLocationOptionId, employee.WageType,
                new DateOnly(2026, 2, 1), null, 30m, 250m, 2.5m),
            CancellationToken.None);

        var history = Assert.Single(updated.ContractHistory);
        Assert.Equal(currentContract.ContractId, history.ContractId);
        Assert.Equal(new DateOnly(2026, 2, 1), history.ValidFrom);
    }

    [Fact]
    public async Task SaveAsync_WithNullEditingContractId_CreatesSecondVersionWithoutOverwritingFirst()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EmployeeRepository(dbContext);
        var employee = await repository.SaveAsync(
            new SaveEmployeeCommand(
                null, null, "6000", "Nora", "Neu", null, new DateOnly(2026, 1, 1), null, true,
                "Teststrasse", "1", null, "6000", "Luzern", "Schweiz",
                null, null, null, null, null, null, null, null, null, null, null, null,
                EmployeeWageType.Hourly, new DateOnly(2026, 4, 1), null, 30m, 250m, 2.5m),
            CancellationToken.None);

        var firstContract = Assert.Single(employee.ContractHistory);

        var updated = await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId, null, employee.PersonnelNumber, employee.FirstName, employee.LastName,
                employee.BirthDate, employee.EntryDate, employee.ExitDate, employee.IsActive,
                employee.Street, employee.HouseNumber, employee.AddressLine2, employee.PostalCode, employee.City, employee.Country,
                employee.ResidenceCountry, employee.Nationality, employee.PermitCode, employee.TaxStatus, employee.IsSubjectToWithholdingTax,
                employee.AhvNumber, employee.Iban, employee.PhoneNumber, employee.Email, employee.DepartmentOptionId,
                employee.EmploymentCategoryOptionId, employee.EmploymentLocationOptionId, employee.WageType,
                new DateOnly(2026, 5, 1), null, 31m, 255m, 2.75m),
            CancellationToken.None);

        Assert.Equal(2, updated.ContractHistory.Count);
        Assert.Contains(updated.ContractHistory, item => item.ContractId == firstContract.ContractId && item.ValidFrom == new DateOnly(2026, 4, 1) && item.ValidTo == new DateOnly(2026, 4, 30));
        Assert.Contains(updated.ContractHistory, item => item.ContractId != firstContract.ContractId && item.ValidFrom == new DateOnly(2026, 5, 1) && item.ValidTo is null);
    }
}
