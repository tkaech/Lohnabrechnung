namespace Payroll.Application.SalaryCertificate;

public interface ISalaryCertificateRecordRepository
{
    void Add(global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord record);

    Task<SalaryCertificateRecordDto?> GetLatestAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
