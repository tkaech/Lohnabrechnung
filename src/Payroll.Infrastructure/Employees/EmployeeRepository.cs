using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Domain.Employees;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Employees;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly PayrollDbContext _dbContext;

    public EmployeeRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var employeesQuery = _dbContext.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchText = query.SearchText.Trim();
            employeesQuery = employeesQuery.Where(employee =>
                employee.PersonnelNumber.Contains(searchText)
                || employee.FirstName.Contains(searchText)
                || employee.LastName.Contains(searchText)
                || (employee.Email != null && employee.Email.Contains(searchText))
                || employee.Address.City.Contains(searchText)
                || employee.Address.PostalCode.Contains(searchText));
        }

        if (query.IsActive.HasValue)
        {
            employeesQuery = employeesQuery.Where(employee => employee.IsActive == query.IsActive.Value);
        }

        var employees = await employeesQuery
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync(cancellationToken);

        var employeeIds = employees
            .Select(employee => employee.Id)
            .ToArray();

        var contractsByEmployeeId = await LoadLatestContractsByEmployeeIdAsync(employeeIds, cancellationToken);
        var departmentsById = await _dbContext.DepartmentOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var categoriesById = await _dbContext.EmploymentCategoryOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
        var locationsById = await _dbContext.EmploymentLocationOptions.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);

        return employees
            .Select(employee =>
            {
                contractsByEmployeeId.TryGetValue(employee.Id, out var currentContract);
                var departmentName = employee.DepartmentOptionId.HasValue && departmentsById.TryGetValue(employee.DepartmentOptionId.Value, out var department)
                    ? department.Name
                    : null;
                var categoryName = employee.EmploymentCategoryOptionId.HasValue && categoriesById.TryGetValue(employee.EmploymentCategoryOptionId.Value, out var category)
                    ? category.Name
                    : null;
                var locationName = employee.EmploymentLocationOptionId.HasValue && locationsById.TryGetValue(employee.EmploymentLocationOptionId.Value, out var location)
                    ? location.Name
                    : null;

                return new EmployeeListItemDto(
                    employee.Id,
                    employee.PersonnelNumber,
                    employee.FirstName + " " + employee.LastName,
                    employee.IsActive,
                    employee.Address.City,
                    employee.Address.Country,
                    employee.Email,
                    departmentName,
                    categoryName,
                    locationName,
                    currentContract?.HourlyRateChf ?? 0m,
                    currentContract?.MonthlyBvgDeductionChf ?? 0m,
                    currentContract?.ValidFrom ?? default,
                    currentContract?.ValidTo);
            })
            .ToArray();
    }

    public async Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var contract = await LoadLatestContractAsync(employeeId, asNoTracking: true, cancellationToken);
        var contractHistory = await LoadContractHistoryAsync(employeeId, cancellationToken);
        var departmentName = await LoadOptionNameAsync(_dbContext.DepartmentOptions, employee.DepartmentOptionId, cancellationToken);
        var categoryName = await LoadOptionNameAsync(_dbContext.EmploymentCategoryOptions, employee.EmploymentCategoryOptionId, cancellationToken);
        var locationName = await LoadOptionNameAsync(_dbContext.EmploymentLocationOptions, employee.EmploymentLocationOptionId, cancellationToken);

        return new EmployeeDetailsDto(
            employee.Id,
            employee.PersonnelNumber,
            employee.FirstName,
            employee.LastName,
            employee.BirthDate,
            employee.EntryDate,
            employee.ExitDate,
            employee.IsActive,
            employee.Address.Street,
            employee.Address.HouseNumber,
            employee.Address.AddressLine2,
            employee.Address.PostalCode,
            employee.Address.City,
            employee.Address.Country,
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
            departmentName,
            employee.EmploymentCategoryOptionId,
            categoryName,
            employee.EmploymentLocationOptionId,
            locationName,
            employee.WageType,
            contract?.ValidFrom ?? default,
            contract?.ValidTo,
            contract?.HourlyRateChf ?? 0m,
            contract?.MonthlyBvgDeductionChf ?? 0m,
            contract?.SpecialSupplementRateChf ?? 0m,
            contractHistory);
    }

    public async Task<EmployeeDetailsDto?> GetByPersonnelNumberAsync(string personnelNumber, CancellationToken cancellationToken)
    {
        var trimmedPersonnelNumber = personnelNumber.Trim();
        var employeeId = await _dbContext.Employees
            .AsNoTracking()
            .Where(item => item.PersonnelNumber == trimmedPersonnelNumber)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return employeeId.HasValue
            ? await GetByIdAsync(employeeId.Value, cancellationToken)
            : null;
    }

    public Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken)
    {
        var trimmedPersonnelNumber = personnelNumber.Trim();

        return _dbContext.Employees.AnyAsync(
            employee => employee.PersonnelNumber == trimmedPersonnelNumber
                && (!excludingEmployeeId.HasValue || employee.Id != excludingEmployeeId.Value),
            cancellationToken);
    }

    public async Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees.SingleAsync(item => item.Id == employeeId, cancellationToken);
        employee.Archive(DateOnly.FromDateTime(DateTime.Today));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken)
    {
        Employee employee;
        EmploymentContract contract;
        var normalizedContractValidFrom = NormalizeToMonthStart(command.ContractValidFrom);
        DateOnly? normalizedContractValidTo = command.ContractValidTo.HasValue
            ? NormalizeToMonthEnd(command.ContractValidTo.Value)
            : null;

        if (command.EmployeeId.HasValue)
        {
            employee = await _dbContext.Employees.SingleAsync(item => item.Id == command.EmployeeId.Value, cancellationToken);
            employee.UpdateCoreData(
                command.PersonnelNumber,
                command.FirstName,
                command.LastName,
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                CreateAddress(command),
                command.ResidenceCountry,
                command.Nationality,
                command.PermitCode,
                command.TaxStatus,
                command.IsSubjectToWithholdingTax,
                command.AhvNumber,
                command.Iban,
                command.PhoneNumber,
                command.Email,
                command.DepartmentOptionId,
                command.EmploymentCategoryOptionId,
                command.EmploymentLocationOptionId,
                command.WageType);

            contract = await SaveContractVersionAsync(
                employee.Id,
                command.EditingContractId,
                normalizedContractValidFrom,
                normalizedContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf,
                command.SpecialSupplementRateChf,
                cancellationToken);
        }
        else
        {
            employee = new Employee(
                command.PersonnelNumber,
                command.FirstName,
                command.LastName,
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                CreateAddress(command),
                command.ResidenceCountry,
                command.Nationality,
                command.PermitCode,
                command.TaxStatus,
                command.IsSubjectToWithholdingTax,
                command.AhvNumber,
                command.Iban,
                command.PhoneNumber,
                command.Email,
                command.DepartmentOptionId,
                command.EmploymentCategoryOptionId,
                command.EmploymentLocationOptionId,
                command.WageType);
            contract = new EmploymentContract(
                employee.Id,
                normalizedContractValidFrom,
                normalizedContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf,
                command.SpecialSupplementRateChf);

            _dbContext.Employees.Add(employee);
            _dbContext.EmploymentContracts.Add(contract);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken)
            ?? throw new InvalidOperationException("Saved employee could not be loaded.");
    }

    public async Task<EmployeeDetailsDto> DeleteContractVersionAsync(Guid employeeId, Guid contractId, CancellationToken cancellationToken)
    {
        var contracts = await _dbContext.EmploymentContracts
            .Where(item => item.EmployeeId == employeeId)
            .OrderBy(item => item.ValidFrom)
            .ToListAsync(cancellationToken);

        var contract = contracts.SingleOrDefault(item => item.Id == contractId)
            ?? throw new InvalidOperationException("Vertragsstand wurde nicht gefunden.");

        var currentContractId = contracts
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .First()
            .Id;

        if (contract.Id == currentContractId)
        {
            throw new InvalidOperationException("Der aktive Vertragsstand kann nicht geloescht werden.");
        }

        var previousContract = contracts
            .Where(item => item.ValidFrom < contract.ValidFrom)
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefault();
        var nextContract = contracts
            .Where(item => item.ValidFrom > contract.ValidFrom)
            .OrderBy(item => item.ValidFrom)
            .FirstOrDefault();

        _dbContext.EmploymentContracts.Remove(contract);

        if (previousContract is not null)
        {
            var newValidTo = nextContract is not null
                ? nextContract.ValidFrom.AddDays(-1)
                : (DateOnly?)null;

            previousContract.UpdateTerms(
                previousContract.ValidFrom,
                newValidTo,
                previousContract.HourlyRateChf,
                previousContract.MonthlyBvgDeductionChf,
                previousContract.SpecialSupplementRateChf);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Mitarbeitender wurde nicht gefunden.");
    }

    private static EmployeeAddress CreateAddress(SaveEmployeeCommand command)
    {
        return new EmployeeAddress(
            command.Street,
            command.HouseNumber,
            command.AddressLine2,
            command.PostalCode,
            command.City,
            command.Country);
    }

    private async Task<EmploymentContract?> LoadLatestContractAsync(
        Guid employeeId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var contractsQuery = _dbContext.EmploymentContracts
            .Where(item => item.EmployeeId == employeeId);

        if (asNoTracking)
        {
            contractsQuery = contractsQuery.AsNoTracking();
        }

        var contracts = await contractsQuery.ToListAsync(cancellationToken);
        return contracts
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefault();
    }

    private async Task<Dictionary<Guid, EmploymentContract>> LoadLatestContractsByEmployeeIdAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        var contracts = await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract => employeeIds.Contains(contract.EmployeeId))
            .ToListAsync(cancellationToken);

        return contracts
            .GroupBy(contract => contract.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(contract => contract.ValidFrom)
                    .First());
    }

    private async Task<IReadOnlyCollection<EmploymentContractVersionDto>> LoadContractHistoryAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var contracts = await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract => contract.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        contracts = contracts
            .OrderByDescending(contract => contract.ValidFrom)
            .ThenByDescending(contract => contract.CreatedAtUtc)
            .ToList();

        var currentContractId = contracts
            .OrderByDescending(contract => contract.ValidFrom)
            .FirstOrDefault()
            ?.Id;

        return contracts
            .Select(contract => new EmploymentContractVersionDto(
                contract.Id,
                contract.ValidFrom,
                contract.ValidTo,
                contract.HourlyRateChf,
                contract.MonthlyBvgDeductionChf,
                contract.SpecialSupplementRateChf,
                contract.Id == currentContractId))
            .ToArray();
    }

    private async Task<EmploymentContract> SaveContractVersionAsync(
        Guid employeeId,
        Guid? editingContractId,
        DateOnly validFrom,
        DateOnly? validTo,
        decimal hourlyRateChf,
        decimal monthlyBvgDeductionChf,
        decimal specialSupplementRateChf,
        CancellationToken cancellationToken)
    {
        var existingContracts = await _dbContext.EmploymentContracts
            .Where(item => item.EmployeeId == employeeId)
            .OrderBy(item => item.ValidFrom)
            .ToListAsync(cancellationToken);

        var contract = editingContractId.HasValue
            ? existingContracts.SingleOrDefault(item => item.Id == editingContractId.Value)
            : existingContracts.SingleOrDefault(item => item.ValidFrom == validFrom);
        if (editingContractId.HasValue && contract is null)
        {
            throw new InvalidOperationException("Der zu bearbeitende Vertragsstand wurde nicht gefunden.");
        }

        if (existingContracts.Any(item => item.Id != contract?.Id && item.ValidFrom == validFrom))
        {
            throw new InvalidOperationException("Es existiert bereits ein anderer Vertragsstand mit demselben Gueltig-ab-Datum.");
        }

        var nextContract = existingContracts
            .Where(item => item.Id != contract?.Id && item.ValidFrom > validFrom)
            .OrderBy(item => item.ValidFrom)
            .FirstOrDefault();

        if (!validTo.HasValue && nextContract is not null)
        {
            validTo = nextContract.ValidFrom.AddDays(-1);
        }

        if (validTo.HasValue && nextContract is not null && nextContract.ValidFrom <= validTo.Value)
        {
            throw new InvalidOperationException("Der Gueltigkeitsbereich des Vertragsstands ueberlappt mit einem spaeteren Vertragsstand.");
        }

        var previousContract = existingContracts
            .Where(item => item.Id != contract?.Id && item.ValidFrom < validFrom)
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefault();

        if (previousContract is not null && (!previousContract.ValidTo.HasValue || previousContract.ValidTo.Value >= validFrom))
        {
            previousContract.UpdateTerms(
                previousContract.ValidFrom,
                validFrom.AddDays(-1),
                previousContract.HourlyRateChf,
                previousContract.MonthlyBvgDeductionChf,
                previousContract.SpecialSupplementRateChf);
        }

        if (contract is null)
        {
            contract = new EmploymentContract(
                employeeId,
                validFrom,
                validTo,
                hourlyRateChf,
                monthlyBvgDeductionChf,
                specialSupplementRateChf);
            _dbContext.EmploymentContracts.Add(contract);
            return contract;
        }

        contract.UpdateTerms(
            validFrom,
            validTo,
            hourlyRateChf,
            monthlyBvgDeductionChf,
            specialSupplementRateChf);
        return contract;
    }

    private static DateOnly NormalizeToMonthStart(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static DateOnly NormalizeToMonthEnd(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    private static async Task<string?> LoadOptionNameAsync<TOption>(
        IQueryable<TOption> query,
        Guid? optionId,
        CancellationToken cancellationToken)
        where TOption : class
    {
        if (!optionId.HasValue)
        {
            return null;
        }

        return await query
            .AsNoTracking()
            .Where(item => EF.Property<Guid>(item, "Id") == optionId.Value)
            .Select(item => EF.Property<string>(item, "Name"))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
