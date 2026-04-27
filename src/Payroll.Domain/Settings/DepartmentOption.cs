using Payroll.Domain.Common;

namespace Payroll.Domain.Settings;

public sealed class DepartmentOption : AuditableEntity
{
    private DepartmentOption()
    {
        Name = string.Empty;
    }

    public DepartmentOption(string name)
    {
        Name = NormalizeName(name);
        IsGavMandatory = false;
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsGavMandatory { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    public void UpdateGavMandatory(bool isGavMandatory)
    {
        if (IsGavMandatory == isGavMandatory)
        {
            return;
        }

        IsGavMandatory = isGavMandatory;
        Touch();
    }

    private static string NormalizeName(string name)
    {
        return Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }
}
