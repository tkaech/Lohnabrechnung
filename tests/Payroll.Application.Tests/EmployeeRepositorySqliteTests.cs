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
                null,
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
    public async Task SaveAsync_WithSingleContractVersion_AllowsAdjustingRangeBackward()
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
                "2001",
                "Demo",
                "Person",
                null,
                new DateOnly(2026, 1, 1),
                null,
                true,
                "Teststrasse",
                "1",
                null,
                "8000",
                "Zuerich",
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                EmployeeWageType.Hourly,
                null,
                new DateOnly(2026, 1, 1),
                null,
                32.5m,
                280m,
                3.0m),
            CancellationToken.None);

        var saved = await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId,
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
                employee.CurrentContractId,
                new DateOnly(2025, 12, 1),
                null,
                employee.HourlyRateChf,
                employee.MonthlyBvgDeductionChf,
                employee.SpecialSupplementRateChf),
            CancellationToken.None);

        var history = Assert.Single(saved.ContractHistory);
        Assert.Equal(new DateOnly(2025, 12, 1), history.ValidFrom);
    }

    [Fact]
    public async Task SaveAsync_WithNullEditingContractId_CreatesSeparateNewContractVersion()
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
                "2002",
                "Demo",
                "Person",
                null,
                new DateOnly(2026, 1, 1),
                null,
                true,
                "Teststrasse",
                "1",
                null,
                "8000",
                "Zuerich",
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                EmployeeWageType.Hourly,
                null,
                new DateOnly(2026, 1, 1),
                null,
                32.5m,
                280m,
                3.0m),
            CancellationToken.None);

        var saved = await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId,
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
                null,
                new DateOnly(2026, 2, 1),
                null,
                35m,
                employee.MonthlyBvgDeductionChf,
                employee.SpecialSupplementRateChf),
            CancellationToken.None);

        Assert.Equal(2, saved.ContractHistory.Count);
        Assert.Contains(saved.ContractHistory, item => item.ContractId == employee.CurrentContractId && item.ValidTo == new DateOnly(2026, 1, 31));
        Assert.Contains(saved.ContractHistory, item => item.ValidFrom == new DateOnly(2026, 2, 1) && item.HourlyRateChf == 35m);
    }

    [Fact]
    public async Task SaveAsync_PreventsOverlappingContractVersions()
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
                "2003",
                "Demo",
                "Person",
                null,
                new DateOnly(2026, 1, 1),
                null,
                true,
                "Teststrasse",
                "1",
                null,
                "8000",
                "Zuerich",
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                EmployeeWageType.Hourly,
                null,
                new DateOnly(2026, 1, 1),
                null,
                32.5m,
                280m,
                3.0m),
            CancellationToken.None);

        await repository.SaveAsync(
            new SaveEmployeeCommand(
                employee.EmployeeId,
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
                null,
                new DateOnly(2026, 3, 1),
                null,
                35m,
                employee.MonthlyBvgDeductionChf,
                employee.SpecialSupplementRateChf),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveAsync(
                new SaveEmployeeCommand(
                    employee.EmployeeId,
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
                    employee.CurrentContractId,
                    new DateOnly(2026, 2, 1),
                    null,
                    employee.HourlyRateChf,
                    employee.MonthlyBvgDeductionChf,
                    employee.SpecialSupplementRateChf),
                CancellationToken.None));

        Assert.Contains("ueberschneiden", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
