using Microsoft.EntityFrameworkCore;
using Payroll.Application.Abstractions;

namespace Payroll.Application.Employees;

public sealed class EmployeeQueries
{
    private readonly IAppDbContext _dbContext;

    public EmployeeQueries(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EmployeeSummaryDto>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.IsActive)
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new EmployeeSummaryDto(
                employee.Id,
                employee.EmployeeNumber,
                employee.FullName,
                employee.EmploymentType.ToString(),
                employee.MonthlySalary,
                employee.HourlyRate))
            .ToListAsync(cancellationToken);
    }
}
