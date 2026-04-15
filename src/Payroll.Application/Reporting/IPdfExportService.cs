namespace Payroll.Application.Reporting;

public interface IPdfExportService
{
    Task<string> ExportPayrollStatementAsync(PayrollStatementPdfDocument document, CancellationToken cancellationToken = default);
}
