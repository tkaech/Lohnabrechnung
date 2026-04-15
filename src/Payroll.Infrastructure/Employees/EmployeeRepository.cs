using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Domain.Employees;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Employees;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly PayrollDbContext _dbContext;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

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

        var contractsByEmployeeId = await LoadCurrentContractsByEmployeeIdAsync(employeeIds, cancellationToken);
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

        var contractHistory = await LoadContractsAsync(employeeId, asNoTracking: true, cancellationToken);
        var contract = DetermineCurrentContract(contractHistory);
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
            contract?.Id,
            contract?.ValidFrom ?? default,
            contract?.ValidTo,
            contract?.HourlyRateChf ?? 0m,
            contract?.MonthlyBvgDeductionChf ?? 0m,
            contract?.SpecialSupplementRateChf ?? 0m,
            BuildContractHistory(contractHistory, contract?.Id));
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
        var normalizedValidFrom = NormalizeToMonthStart(command.ContractValidFrom);
        DateOnly? normalizedValidTo = command.ContractValidTo.HasValue
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

            var contracts = await LoadContractsAsync(employee.Id, asNoTracking: false, cancellationToken);
            var contract = command.EditingContractId.HasValue
                ? contracts.SingleOrDefault(item => item.Id == command.EditingContractId.Value)
                : null;

            if (command.EditingContractId.HasValue && contract is null)
            {
                throw new InvalidOperationException("Der zu bearbeitende Vertragsstand wurde nicht gefunden.");
            }

            if (contract is not null)
            {
                contract.UpdateTerms(
                    normalizedValidFrom,
                    normalizedValidTo,
                    command.HourlyRateChf,
                    command.MonthlyBvgDeductionChf,
                    command.SpecialSupplementRateChf);
            }
            else
            {
                contract = new EmploymentContract(
                    employee.Id,
                    normalizedValidFrom,
                    normalizedValidTo,
                    command.HourlyRateChf,
                    command.MonthlyBvgDeductionChf,
                    command.SpecialSupplementRateChf);
                _dbContext.EmploymentContracts.Add(contract);
                contracts.Add(contract);
            }

            ApplyContractVersionRules(employee.Id, contracts, contract, normalizedValidFrom, normalizedValidTo, isNewVersion: !command.EditingContractId.HasValue);
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
            var contract = new EmploymentContract(
                employee.Id,
                normalizedValidFrom,
                normalizedValidTo,
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

    private static IReadOnlyCollection<EmploymentContractVersionDto> BuildContractHistory(
        IReadOnlyCollection<EmploymentContract> contracts,
        Guid? currentContractId)
    {
        return contracts
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new EmploymentContractVersionDto(
                item.Id,
                item.ValidFrom,
                item.ValidTo,
                item.HourlyRateChf,
                item.MonthlyBvgDeductionChf,
                item.SpecialSupplementRateChf,
                item.Id == currentContractId))
            .ToArray();
    }

    private static void ApplyContractVersionRules(
        Guid employeeId,
        List<EmploymentContract> contracts,
        EmploymentContract targetContract,
        DateOnly validFrom,
        DateOnly? validTo,
        bool isNewVersion)
    {
        if (contracts.Count == 1)
        {
            return;
        }

        var orderedContracts = contracts
            .OrderBy(item => item.ValidFrom)
            .ThenBy(item => item.CreatedAtUtc)
            .ToList();

        var previousContract = orderedContracts
            .Where(item => item.Id != targetContract.Id && item.ValidFrom < validFrom)
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault();
        var nextContract = orderedContracts
            .Where(item => item.Id != targetContract.Id && item.ValidFrom > validFrom)
            .OrderBy(item => item.ValidFrom)
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefault();

        var effectiveValidTo = validTo;
        if (isNewVersion && !effectiveValidTo.HasValue && nextContract is not null)
        {
            effectiveValidTo = nextContract.ValidFrom.AddDays(-1);
            targetContract.UpdateTerms(validFrom, effectiveValidTo, targetContract.HourlyRateChf, targetContract.MonthlyBvgDeductionChf, targetContract.SpecialSupplementRateChf);
        }

        if (previousContract is not null)
        {
            var previousValidTo = validFrom.AddDays(-1);
            if (previousValidTo < previousContract.ValidFrom)
            {
                throw new InvalidOperationException("Der neue Vertragsstand beginnt vor dem vorherigen Vertragsstand.");
            }

            if (isNewVersion)
            {
                previousContract.UpdateTerms(
                    previousContract.ValidFrom,
                    previousValidTo,
                    previousContract.HourlyRateChf,
                    previousContract.MonthlyBvgDeductionChf,
                    previousContract.SpecialSupplementRateChf);
            }
        }

        if (nextContract is not null && (!effectiveValidTo.HasValue || effectiveValidTo.Value >= nextContract.ValidFrom))
        {
            throw new InvalidOperationException("Vertragsstaende duerfen sich nicht ueberschneiden.");
        }

        if (previousContract is not null && previousContract.ValidTo.HasValue && previousContract.ValidTo.Value >= validFrom)
        {
            throw new InvalidOperationException("Vertragsstaende duerfen sich nicht ueberschneiden.");
        }

        var overlaps = contracts.Any(item =>
            item.Id != targetContract.Id
            && item.EmployeeId == employeeId
            && item.ValidFrom <= (effectiveValidTo ?? DateOnly.MaxValue)
            && validFrom <= (item.ValidTo ?? DateOnly.MaxValue));

        if (overlaps)
        {
            throw new InvalidOperationException("Vertragsstaende duerfen sich nicht ueberschneiden.");
        }
    }

    private async Task<EmploymentContract?> LoadCurrentContractAsync(
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
            .Where(item => item.IsActiveOn(Today))
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault()
            ?? contracts
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault();
    }

    private async Task<List<EmploymentContract>> LoadContractsAsync(
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

        return await contractsQuery.ToListAsync(cancellationToken);
    }

    private static EmploymentContract? DetermineCurrentContract(IReadOnlyCollection<EmploymentContract> contracts)
    {
        return contracts
            .Where(item => item.IsActiveOn(Today))
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault()
            ?? contracts
                .OrderByDescending(item => item.ValidFrom)
                .ThenByDescending(item => item.CreatedAtUtc)
                .FirstOrDefault();
    }

    private async Task<Dictionary<Guid, EmploymentContract>> LoadCurrentContractsByEmployeeIdAsync(
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
                group => DetermineCurrentContract(group.ToArray())!);
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
