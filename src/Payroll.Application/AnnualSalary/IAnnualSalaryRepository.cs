namespace Payroll.Application.AnnualSalary;

public interface IAnnualSalaryRepository
{
    Task<AnnualSalaryOverviewDto> GetOverviewAsync(
        AnnualSalaryOverviewQuery query,
        CancellationToken cancellationToken);
}
