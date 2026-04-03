using Payroll.Domain.Common;

namespace Payroll.Domain.Reporting;

public sealed class AuditLog : AuditableEntity
{
    public string EntityName { get; private set; }
    public string Action { get; private set; }
    public string PerformedBy { get; private set; }

    public AuditLog(string entityName, string action, string performedBy)
    {
        EntityName = entityName;
        Action = action;
        PerformedBy = performedBy;
    }
}
