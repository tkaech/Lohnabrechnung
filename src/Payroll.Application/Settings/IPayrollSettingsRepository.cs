using Payroll.Domain.Employees;

namespace Payroll.Application.Settings;

public interface IPayrollSettingsRepository
{
    Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken);
    Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken);
    Task<PayrollSettingsDto> DeleteCalculationVersionAsync(Guid versionId, CancellationToken cancellationToken);
}
