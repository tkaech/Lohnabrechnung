namespace Payroll.Application.Employees;

public sealed class EmployeeService
{
    private readonly IEmployeeRepository _repository;

    public EmployeeService(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery? query = null, CancellationToken cancellationToken = default)
    {
        return _repository.ListAsync(query ?? new EmployeeListQuery(null, null), cancellationToken);
    }

    public Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(employeeId, cancellationToken);
    }

    public async Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await _repository.PersonnelNumberExistsAsync(command.PersonnelNumber, command.EmployeeId, cancellationToken))
        {
            throw new InvalidOperationException("Personnel number must be unique.");
        }

        return await _repository.SaveAsync(command, cancellationToken);
    }

    public Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return _repository.ArchiveAsync(employeeId, cancellationToken);
    }
}
