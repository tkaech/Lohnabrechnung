using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;

namespace Payroll.Application.Payroll;

public interface IPayrollRunRepository
{
    Task<IReadOnlyCollection<PayrollRun>> ListFinalizedRunsAsync(int year, int fromMonth, int toMonth, CancellationToken cancellationToken);
    Task<PayrollRun?> GetFinalizedRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken);
    Task<PayrollRun?> GetFinalizedRunForEmployeePeriodForUpdateAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken);
    Task<PayrollRun?> GetLatestRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken);
    Task<bool> HasCancelledRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken);
    Task<PayrollRunMonthlyInputDto?> LoadMonthlyInputAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken);
    Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken);
    Task<PayrollSettings> LoadPayrollSettingsForPeriodAsync(int year, int month, CancellationToken cancellationToken);
    void Add(PayrollRun payrollRun);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
