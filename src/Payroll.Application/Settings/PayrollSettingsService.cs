using Payroll.Domain.Employees;

namespace Payroll.Application.Settings;

public sealed class PayrollSettingsService
{
    private readonly IPayrollSettingsRepository _repository;

    public PayrollSettingsService(IPayrollSettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAsync(cancellationToken);
    }

    public Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetWorkTimeSupplementSettingsAsync(cancellationToken);
    }

    public Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _repository.SaveAsync(command, cancellationToken);
    }

    public Task<PayrollSettingsDto> DeleteCalculationVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteCalculationVersionAsync(versionId, cancellationToken);
    }
}
