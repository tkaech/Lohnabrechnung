using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class Employee : Entity
{
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public DateOnly HireDate { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public decimal HourlyRate { get; private set; }
    public decimal MonthlySalary { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Employee()
    {
    }

    public Employee(
        string employeeNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DateOnly hireDate,
        EmploymentType employmentType,
        decimal hourlyRate,
        decimal monthlySalary)
    {
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        HireDate = hireDate;
        EmploymentType = employmentType;
        HourlyRate = hourlyRate;
        MonthlySalary = monthlySalary;
    }

    public string FullName => $"{FirstName} {LastName}";
}
