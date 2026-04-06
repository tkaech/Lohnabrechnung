using Payroll.Domain.Common;

namespace Payroll.Domain.Employees;

public sealed class Employee : AuditableEntity
{
    private Employee()
    {
        PersonnelNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Address = new EmployeeAddress();
        EntryDate = default;
        IsActive = true;
    }

    public string PersonnelNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateOnly? BirthDate { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public DateOnly? ExitDate { get; private set; }
    public bool IsActive { get; private set; }
    public EmployeeAddress Address { get; private set; } = null!;
    public string? ResidenceCountry { get; private set; }
    public string? Nationality { get; private set; }
    public string? PermitCode { get; private set; }
    public string? TaxStatus { get; private set; }
    public bool? IsSubjectToWithholdingTax { get; private set; }
    public string? AhvNumber { get; private set; }
    public string? Iban { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    public Employee(
        string personnelNumber,
        string firstName,
        string lastName,
        DateOnly? birthDate,
        DateOnly entryDate,
        DateOnly? exitDate,
        bool isActive,
        EmployeeAddress address,
        string? residenceCountry,
        string? nationality,
        string? permitCode,
        string? taxStatus,
        bool? isSubjectToWithholdingTax,
        string? ahvNumber,
        string? iban,
        string? phoneNumber,
        string? email)
    {
        ApplyCoreData(
            personnelNumber,
            firstName,
            lastName,
            birthDate,
            entryDate,
            exitDate,
            isActive,
            address,
            residenceCountry,
            nationality,
            permitCode,
            taxStatus,
            isSubjectToWithholdingTax,
            ahvNumber,
            iban,
            phoneNumber,
            email);
    }

    public void Rename(string firstName, string lastName)
    {
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Touch();
    }

    public void UpdatePersonnelNumber(string personnelNumber)
    {
        PersonnelNumber = Guard.AgainstNullOrWhiteSpace(personnelNumber, nameof(personnelNumber));
        Touch();
    }

    public void UpdateCoreData(
        string personnelNumber,
        string firstName,
        string lastName,
        DateOnly? birthDate,
        DateOnly entryDate,
        DateOnly? exitDate,
        bool isActive,
        EmployeeAddress address,
        string? residenceCountry,
        string? nationality,
        string? permitCode,
        string? taxStatus,
        bool? isSubjectToWithholdingTax,
        string? ahvNumber,
        string? iban,
        string? phoneNumber,
        string? email)
    {
        ApplyCoreData(
            personnelNumber,
            firstName,
            lastName,
            birthDate,
            entryDate,
            exitDate,
            isActive,
            address,
            residenceCountry,
            nationality,
            permitCode,
            taxStatus,
            isSubjectToWithholdingTax,
            ahvNumber,
            iban,
            phoneNumber,
            email);
        Touch();
    }

    public void Archive(DateOnly archiveDate)
    {
        if (archiveDate < EntryDate)
        {
            throw new ArgumentException("Archive date cannot be before entry date.", nameof(archiveDate));
        }

        IsActive = false;
        ExitDate = !ExitDate.HasValue || ExitDate.Value > archiveDate
            ? archiveDate
            : ExitDate;
        Touch();
    }

    private void ApplyCoreData(
        string personnelNumber,
        string firstName,
        string lastName,
        DateOnly? birthDate,
        DateOnly entryDate,
        DateOnly? exitDate,
        bool isActive,
        EmployeeAddress address,
        string? residenceCountry,
        string? nationality,
        string? permitCode,
        string? taxStatus,
        bool? isSubjectToWithholdingTax,
        string? ahvNumber,
        string? iban,
        string? phoneNumber,
        string? email)
    {
        ArgumentNullException.ThrowIfNull(address);
        var normalizedExitDate = isActive ? null : exitDate;
        ValidateDates(birthDate, entryDate, normalizedExitDate);

        PersonnelNumber = Guard.AgainstNullOrWhiteSpace(personnelNumber, nameof(personnelNumber));
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        BirthDate = birthDate;
        EntryDate = entryDate;
        ExitDate = normalizedExitDate;
        IsActive = isActive;
        Address = address;
        ResidenceCountry = NormalizeOptional(residenceCountry);
        Nationality = NormalizeOptional(nationality);
        PermitCode = NormalizeOptional(permitCode);
        TaxStatus = NormalizeOptional(taxStatus);
        IsSubjectToWithholdingTax = isSubjectToWithholdingTax;
        AhvNumber = NormalizeOptional(ahvNumber);
        Iban = NormalizeOptional(iban);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Email = NormalizeOptional(email);
    }

    private static void ValidateDates(DateOnly? birthDate, DateOnly entryDate, DateOnly? exitDate)
    {
        if (birthDate.HasValue && birthDate.Value > entryDate)
        {
            throw new ArgumentException("Birth date cannot be after entry date.", nameof(birthDate));
        }

        Guard.AgainstInvalidPeriod(entryDate, exitDate, nameof(exitDate));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
