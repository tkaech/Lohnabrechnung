namespace Payroll.Application.SalaryCertificate;

public interface ISalaryCertificatePdfDocumentWriter
{
    Task WriteAsync(
        string templatePath,
        string outputPath,
        IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields,
        CancellationToken cancellationToken = default);
}
