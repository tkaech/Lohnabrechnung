using Payroll.Domain.Employees;

namespace Payroll.Domain.Tests;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_TrimsAndExposesExtendedProfile()
    {
        var employee = CreateEmployee(
            personnelNumber: " 1000 ",
            firstName: " Max ",
            lastName: " Muster ",
            residenceCountry: " Schweiz ",
            email: " max@example.ch ");

        Assert.Equal("1000", employee.PersonnelNumber);
        Assert.Equal("Max Muster", employee.FullName);
        Assert.Equal("Musterstrasse", employee.Address.Street);
        Assert.Equal("8000", employee.Address.PostalCode);
        Assert.Equal("Schweiz", employee.ResidenceCountry);
        Assert.Equal("max@example.ch", employee.Email);
    }

    [Fact]
    public void Rename_UpdatesNameAndAuditTimestamp()
    {
        var employee = CreateEmployee();

        employee.Rename("Mia", "Muster");

        Assert.Equal("Mia", employee.FirstName);
        Assert.NotNull(employee.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyPersonnelNumber()
    {
        Assert.Throws<ArgumentException>(() => CreateEmployee(personnelNumber: " "));
    }

    [Fact]
    public void UpdateCoreData_ChangesPersonnelNumberNameAndAddress()
    {
        var employee = CreateEmployee();

        employee.UpdateCoreData(
            "1001",
            "Mia",
            "Muster",
            new DateOnly(1991, 6, 5),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 8, 31),
            false,
            new EmployeeAddress("Seestrasse", "7", "2. OG", "9000", "St. Gallen", "Schweiz"),
            "Deutschland",
            "DE",
            "B",
            "Quellensteuer B",
            true,
            "756.1111.2222.33",
            "CH9300762011623852957",
            "+41 79 555 00 11",
            "mia@example.ch");

        Assert.Equal("1001", employee.PersonnelNumber);
        Assert.Equal("Mia Muster", employee.FullName);
        Assert.Equal(new DateOnly(2026, 8, 31), employee.ExitDate);
        Assert.False(employee.IsActive);
        Assert.Equal("Seestrasse", employee.Address.Street);
        Assert.Equal("Deutschland", employee.ResidenceCountry);
        Assert.True(employee.IsSubjectToWithholdingTax);
        Assert.NotNull(employee.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateCoreData_RejectsBirthDateAfterEntryDate()
    {
        var employee = CreateEmployee();

        Assert.Throws<ArgumentException>(() => employee.UpdateCoreData(
            "1000",
            "Max",
            "Muster",
            new DateOnly(2027, 1, 1),
            new DateOnly(2026, 1, 1),
            null,
            true,
            new EmployeeAddress("Musterstrasse", "10a", null, "8000", "Zuerich", "Schweiz"),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));
    }

    [Fact]
    public void Archive_MarksEmployeeInactiveAndSetsExitDate()
    {
        var employee = CreateEmployee();

        employee.Archive(new DateOnly(2026, 3, 31));

        Assert.False(employee.IsActive);
        Assert.Equal(new DateOnly(2026, 3, 31), employee.ExitDate);
        Assert.NotNull(employee.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateCoreData_ReactivatingEmployeeClearsExitDate()
    {
        var employee = CreateEmployee();
        employee.Archive(new DateOnly(2026, 3, 31));

        employee.UpdateCoreData(
            "1000",
            "Max",
            "Muster",
            new DateOnly(1990, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            true,
            new EmployeeAddress("Musterstrasse", "10a", null, "8000", "Zuerich", "Schweiz"),
            null,
            "CH",
            null,
            null,
            null,
            null,
            null,
            "+41 79 123 45 67",
            "max@example.ch");

        Assert.True(employee.IsActive);
        Assert.Null(employee.ExitDate);
    }

    private static Employee CreateEmployee(
        string personnelNumber = "1000",
        string firstName = "Max",
        string lastName = "Muster",
        string? residenceCountry = null,
        string? email = null)
    {
        return new Employee(
            personnelNumber,
            firstName,
            lastName,
            new DateOnly(1990, 1, 1),
            new DateOnly(2026, 1, 1),
            null,
            true,
            new EmployeeAddress("Musterstrasse", "10a", null, "8000", "Zuerich", "Schweiz"),
            residenceCountry,
            "CH",
            null,
            null,
            null,
            null,
            null,
            "+41 79 123 45 67",
            email);
    }
}
