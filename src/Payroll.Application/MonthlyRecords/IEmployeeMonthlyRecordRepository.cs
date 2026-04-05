using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.MonthlyRecords;

public interface IEmployeeMonthlyRecordRepository
{
    Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken);
    Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken);
    Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken);
    void ClearTracking();
    void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class;
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
