using Payroll.Application.Employees;

namespace Payroll.Application.Tests;

public sealed class EmployeeServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsEmployeesFromRepository()
    {
        var repository = new InMemoryEmployeeRepository();
        var employee = await repository.SaveAsync(
            CreateCommand(personnelNumber: "1000", firstName: "Max", lastName: "Muster", city: "Bern", email: "max@example.ch"),
            CancellationToken.None);

        var service = new EmployeeService(repository);
        var result = await service.ListAsync();

        var item = Assert.Single(result);
        Assert.Equal(employee.EmployeeId, item.EmployeeId);
        Assert.Equal("1000", item.PersonnelNumber);
        Assert.True(item.IsActive);
        Assert.Equal("Bern", item.City);
    }

    [Fact]
    public async Task SaveAsync_CreatesEmployeeWhenPersonnelNumberIsUnique()
    {
        var repository = new InMemoryEmployeeRepository();
        var service = new EmployeeService(repository);

        var employee = await service.SaveAsync(
            CreateCommand(
                personnelNumber: "1001",
                firstName: "Mia",
                lastName: "Muster",
                city: "Zuerich",
                nationality: "DE",
                isSubjectToWithholdingTax: true,
                hourlyRateChf: 34m,
                monthlyBvgDeductionChf: 300m,
                nightSupplementRate: 0.3m,
                sundaySupplementRate: 0.5m));

        Assert.Equal("1001", employee.PersonnelNumber);
        Assert.Equal("Mia", employee.FirstName);
        Assert.Equal("Zuerich", employee.City);
        Assert.Equal("DE", employee.Nationality);
        Assert.True(employee.IsSubjectToWithholdingTax);
        Assert.Equal(34m, employee.HourlyRateChf);
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicatePersonnelNumber()
    {
        var repository = new InMemoryEmployeeRepository();
        await repository.SaveAsync(
            CreateCommand(personnelNumber: "1002", firstName: "Max", lastName: "Muster"),
            CancellationToken.None);

        var service = new EmployeeService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(CreateCommand(personnelNumber: "1002", firstName: "Mia", lastName: "Muster")));
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingEmployee()
    {
        var repository = new InMemoryEmployeeRepository();
        var created = await repository.SaveAsync(
            CreateCommand(personnelNumber: "1003", firstName: "Max", lastName: "Muster"),
            CancellationToken.None);

        var service = new EmployeeService(repository);
        var updated = await service.SaveAsync(
            CreateCommand(
                employeeId: created.EmployeeId,
                personnelNumber: "1003A",
                firstName: "Maxim",
                lastName: "Muster",
                city: "Basel",
                isActive: false,
                exitDate: new DateOnly(2026, 8, 31),
                hourlyRateChf: 36m,
                monthlyBvgDeductionChf: 310m,
                nightSupplementRate: 0.25m));

        Assert.Equal(created.EmployeeId, updated.EmployeeId);
        Assert.Equal("1003A", updated.PersonnelNumber);
        Assert.Equal("Maxim", updated.FirstName);
        Assert.Equal("Basel", updated.City);
        Assert.False(updated.IsActive);
        Assert.Equal(36m, updated.HourlyRateChf);
        Assert.Equal(0.25m, updated.NightSupplementRate);
    }

    [Fact]
    public async Task ListAsync_FiltersBySearchTextAndActivity()
    {
        var repository = new InMemoryEmployeeRepository();
        await repository.SaveAsync(CreateCommand(personnelNumber: "2000", firstName: "Anna", lastName: "Aktiv", city: "Luzern", isActive: true), CancellationToken.None);
        await repository.SaveAsync(CreateCommand(personnelNumber: "2001", firstName: "Ines", lastName: "Inaktiv", city: "Bern", isActive: false), CancellationToken.None);

        var service = new EmployeeService(repository);
        var result = await service.ListAsync(new EmployeeListQuery("Bern", false));

        var item = Assert.Single(result);
        Assert.Equal("2001", item.PersonnelNumber);
        Assert.False(item.IsActive);
    }

    [Fact]
    public async Task ArchiveAsync_MarksEmployeeInactive()
    {
        var repository = new InMemoryEmployeeRepository();
        var created = await repository.SaveAsync(CreateCommand(personnelNumber: "3000", firstName: "Lina", lastName: "Test"), CancellationToken.None);

        var service = new EmployeeService(repository);
        await service.ArchiveAsync(created.EmployeeId);

        var archived = await repository.GetByIdAsync(created.EmployeeId, CancellationToken.None);
        Assert.NotNull(archived);
        Assert.False(archived!.IsActive);
        Assert.NotNull(archived.ExitDate);
    }

    private sealed class InMemoryEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<Guid, EmployeeDetailsDto> _employees = [];

        public Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<EmployeeDetailsDto> items = _employees.Values;

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                items = items.Where(item =>
                    item.PersonnelNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.City.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || (item.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (query.IsActive.HasValue)
            {
                items = items.Where(item => item.IsActive == query.IsActive.Value);
            }

            var result = items
                .OrderBy(item => item.LastName)
                .ThenBy(item => item.FirstName)
                .Select(item => new EmployeeListItemDto(
                    item.EmployeeId,
                    item.PersonnelNumber,
                    $"{item.FirstName} {item.LastName}",
                    item.IsActive,
                    item.City,
                    item.Country,
                    item.Email,
                    item.HourlyRateChf,
                    item.MonthlyBvgDeductionChf,
                    item.ContractValidFrom,
                    item.ContractValidTo))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EmployeeListItemDto>>(result);
        }

        public Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            _employees.TryGetValue(employeeId, out var result);
            return Task.FromResult(result);
        }

        public Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken)
        {
            var trimmedPersonnelNumber = personnelNumber.Trim();
            var result = _employees.Values.Any(item =>
                item.PersonnelNumber == trimmedPersonnelNumber
                && (!excludingEmployeeId.HasValue || item.EmployeeId != excludingEmployeeId.Value));

            return Task.FromResult(result);
        }

        public Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            if (_employees.TryGetValue(employeeId, out var existing))
            {
                _employees[employeeId] = existing with
                {
                    IsActive = false,
                    ExitDate = existing.ExitDate ?? DateOnly.FromDateTime(DateTime.Today)
                };
            }

            return Task.CompletedTask;
        }

        public Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken)
        {
            var employeeId = command.EmployeeId ?? Guid.NewGuid();
            var employee = new EmployeeDetailsDto(
                employeeId,
                command.PersonnelNumber.Trim(),
                command.FirstName.Trim(),
                command.LastName.Trim(),
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                command.Street.Trim(),
                command.HouseNumber?.Trim(),
                command.AddressLine2?.Trim(),
                command.PostalCode.Trim(),
                command.City.Trim(),
                command.Country.Trim(),
                command.ResidenceCountry?.Trim(),
                command.Nationality?.Trim(),
                command.PermitCode?.Trim(),
                command.TaxStatus?.Trim(),
                command.IsSubjectToWithholdingTax,
                command.AhvNumber?.Trim(),
                command.Iban?.Trim(),
                command.PhoneNumber?.Trim(),
                command.Email?.Trim(),
                command.ContractValidFrom,
                command.ContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf,
                command.NightSupplementRate,
                command.SundaySupplementRate,
                command.HolidaySupplementRate);

            _employees[employeeId] = employee;
            return Task.FromResult(employee);
        }
    }

    private static SaveEmployeeCommand CreateCommand(
        Guid? employeeId = null,
        string personnelNumber = "1000",
        string firstName = "Max",
        string lastName = "Muster",
        string city = "Zuerich",
        string country = "Schweiz",
        string? nationality = "CH",
        bool isActive = true,
        bool? isSubjectToWithholdingTax = null,
        string? email = null,
        DateOnly? exitDate = null,
        decimal hourlyRateChf = 32.5m,
        decimal monthlyBvgDeductionChf = 280m,
        decimal? nightSupplementRate = 0.25m,
        decimal? sundaySupplementRate = null,
        decimal? holidaySupplementRate = null)
    {
        return new SaveEmployeeCommand(
            employeeId,
            personnelNumber,
            firstName,
            lastName,
            new DateOnly(1990, 5, 12),
            new DateOnly(2026, 1, 1),
            exitDate,
            isActive,
            "Musterstrasse",
            "10a",
            null,
            "8000",
            city,
            country,
            country,
            nationality,
            null,
            null,
            isSubjectToWithholdingTax,
            "756.1111.2222.33",
            "CH9300762011623852957",
            "+41 79 123 45 67",
            email,
            new DateOnly(2026, 1, 1),
            null,
            hourlyRateChf,
            monthlyBvgDeductionChf,
            nightSupplementRate,
            sundaySupplementRate,
            holidaySupplementRate);
    }
}
