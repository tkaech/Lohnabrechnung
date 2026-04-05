using Payroll.Domain.Common;
using Payroll.Domain.Employees;

namespace Payroll.Domain.Settings;

public sealed class PayrollSettings : AuditableEntity
{
    private PayrollSettings()
    {
        WorkTimeSupplementSettings = WorkTimeSupplementSettings.Empty;
    }

    public PayrollSettings(WorkTimeSupplementSettings? workTimeSupplementSettings = null)
    {
        WorkTimeSupplementSettings = workTimeSupplementSettings ?? WorkTimeSupplementSettings.Empty;
    }

    public WorkTimeSupplementSettings WorkTimeSupplementSettings { get; private set; }

    public void UpdateWorkTimeSupplementSettings(WorkTimeSupplementSettings workTimeSupplementSettings)
    {
        ArgumentNullException.ThrowIfNull(workTimeSupplementSettings);

        WorkTimeSupplementSettings = workTimeSupplementSettings;
        Touch();
    }
}
