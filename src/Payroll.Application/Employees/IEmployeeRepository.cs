namespace Payroll.Application.Employees;

public interface IEmployeeRepository
{
    Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken);
    Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<EmployeeDetailsDto?> GetByPersonnelNumberAsync(string personnelNumber, CancellationToken cancellationToken);
    Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken);
}
