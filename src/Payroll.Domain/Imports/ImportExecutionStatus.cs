using Payroll.Domain.Common;

namespace Payroll.Domain.Imports;

public sealed class ImportExecutionStatus : AuditableEntity
{
    private ImportExecutionStatus()
    {
    }

    public ImportExecutionStatus(
        ImportConfigurationType type,
        int year,
        int month,
        DateTimeOffset importedAtUtc)
    {
        Type = type;
        Year = year;
        Month = month;
        ImportedAtUtc = importedAtUtc;
    }

    public ImportConfigurationType Type { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateTimeOffset ImportedAtUtc { get; private set; }
}
