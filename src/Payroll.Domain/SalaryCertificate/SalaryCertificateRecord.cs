using Payroll.Domain.Common;

namespace Payroll.Domain.SalaryCertificate;

public sealed class SalaryCertificateRecord : AuditableEntity
{
    private SalaryCertificateRecord()
    {
    }

    public SalaryCertificateRecord(
        Guid employeeId,
        int year,
        string? outputFilePath,
        string? fileHash = null)
    {
        EmployeeId = employeeId;
        Year = year;
        OutputFilePath = NormalizeOptional(outputFilePath);
        FileHash = NormalizeOptional(fileHash);
    }

    public Guid EmployeeId { get; private set; }
    public int Year { get; private set; }
    public string? OutputFilePath { get; private set; }
    public string? FileHash { get; private set; }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
