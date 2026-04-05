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

        return employees
            .Select(employee =>
            {
                contractsByEmployeeId.TryGetValue(employee.Id, out var currentContract);

                return new EmployeeListItemDto(
                    employee.Id,
                    employee.PersonnelNumber,
                    employee.FirstName + " " + employee.LastName,
                    employee.IsActive,
                    employee.Address.City,
                    employee.Address.Country,
                    employee.Email,
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
            contract?.ValidFrom ?? default,
            contract?.ValidTo,
            contract?.HourlyRateChf ?? 0m,
            contract?.MonthlyBvgDeductionChf ?? 0m);
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
                command.Email);

            contract = await LoadLatestContractAsync(employee.Id, asNoTracking: false, cancellationToken)
                ?? new EmploymentContract(
                    employee.Id,
                    command.ContractValidFrom,
                    command.ContractValidTo,
                    command.HourlyRateChf,
                    command.MonthlyBvgDeductionChf);

            if (contract.EmployeeId == employee.Id && _dbContext.Entry(contract).State != EntityState.Detached)
            {
                contract.UpdateTerms(
                    command.ContractValidFrom,
                    command.ContractValidTo,
                    command.HourlyRateChf,
                    command.MonthlyBvgDeductionChf);
            }
            else
            {
                _dbContext.EmploymentContracts.Add(contract);
            }
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
                command.Email);
            contract = new EmploymentContract(
                employee.Id,
                command.ContractValidFrom,
                command.ContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf);

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
}
