namespace Payroll.Application.SalaryCertificate;

public interface ISalaryCertificatePdfFormFieldReader
{
    Task<IReadOnlyCollection<string>> ReadFieldNamesAsync(
        string templatePath,
        CancellationToken cancellationToken = default);
}
