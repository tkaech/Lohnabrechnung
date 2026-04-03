using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public string PersonnelNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    public Employee(string personnelNumber, string firstName, string lastName)
    {
        PersonnelNumber = Guard.AgainstNullOrWhiteSpace(personnelNumber, nameof(personnelNumber));
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
    }

    public void Rename(string firstName, string lastName)
    {
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Touch();
    }
}
