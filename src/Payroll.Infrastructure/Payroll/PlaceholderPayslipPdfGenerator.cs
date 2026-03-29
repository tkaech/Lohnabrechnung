using Payroll.Application.Abstractions;
using Payroll.Domain.Payroll;

namespace Payroll.Infrastructure.Payroll;

public sealed class PlaceholderPayslipPdfGenerator : IPayslipPdfGenerator
{
    public Task GenerateAsync(PayrollRun payrollRun, string outputDirectory, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
