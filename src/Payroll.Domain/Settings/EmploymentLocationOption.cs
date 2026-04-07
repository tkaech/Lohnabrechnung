using Payroll.Domain.Common;

namespace Payroll.Domain.Settings;

public sealed class EmploymentLocationOption : AuditableEntity
{
    private EmploymentLocationOption()
    {
        Name = string.Empty;
    }

    public EmploymentLocationOption(string name)
    {
        Name = NormalizeName(name);
    }

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    private static string NormalizeName(string name)
    {
        return Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }
}
