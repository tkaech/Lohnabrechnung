namespace Payroll.Application.Imports;

public interface ICsvImportFileReader
{
    Task<CsvImportDocumentDto> ReadAsync(ReadCsvImportDocumentCommand command, CancellationToken cancellationToken);
}
