using Payroll.Domain.Employees;

namespace Payroll.Domain.Tests;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_TrimsAndExposesFullName()
    {
        var employee = new Employee(" 1000 ", " Max ", " Muster ");

        Assert.Equal("1000", employee.PersonnelNumber);
        Assert.Equal("Max Muster", employee.FullName);
    }

    [Fact]
    public void Rename_UpdatesNameAndAuditTimestamp()
    {
        var employee = new Employee("1000", "Max", "Muster");

        employee.Rename("Mia", "Muster");

        Assert.Equal("Mia", employee.FirstName);
        Assert.NotNull(employee.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyPersonnelNumber()
    {
        Assert.Throws<ArgumentException>(() => new Employee(" ", "Max", "Muster"));
    }
}
