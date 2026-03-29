using Payroll.Domain.Common;

namespace Payroll.Domain.TimeTracking;

public sealed class ImportedWorkTime : Entity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public decimal Hours { get; private set; }
    public string SourceFileName { get; private set; } = string.Empty;

    private ImportedWorkTime()
    {
    }

    public ImportedWorkTime(Guid employeeId, DateOnly workDate, decimal hours, string sourceFileName)
    {
        EmployeeId = employeeId;
        WorkDate = workDate;
        Hours = hours;
        SourceFileName = sourceFileName;
    }
}
