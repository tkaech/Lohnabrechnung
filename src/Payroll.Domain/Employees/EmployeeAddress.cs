using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class EmployeeAddress
{
    internal EmployeeAddress()
    {
        Street = string.Empty;
        PostalCode = string.Empty;
        City = string.Empty;
        Country = string.Empty;
    }

    public string Street { get; private set; }
    public string? HouseNumber { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }

    public EmployeeAddress(
        string street,
        string? houseNumber,
        string? addressLine2,
        string postalCode,
        string city,
        string country)
    {
        Street = Guard.AgainstNullOrWhiteSpace(street, nameof(street));
        HouseNumber = NormalizeOptional(houseNumber);
        AddressLine2 = NormalizeOptional(addressLine2);
        PostalCode = Guard.AgainstNullOrWhiteSpace(postalCode, nameof(postalCode));
        City = Guard.AgainstNullOrWhiteSpace(city, nameof(city));
        Country = Guard.AgainstNullOrWhiteSpace(country, nameof(country));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
