using Payroll.Domain.Payroll;

namespace Payroll.Application.Abstractions;

public interface IPayslipPdfGenerator
{
    Task GenerateAsync(PayrollRun payrollRun, string outputDirectory, CancellationToken cancellationToken = default);
}
