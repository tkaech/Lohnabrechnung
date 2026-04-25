namespace Payroll.Application.AnnualSalary;

public sealed class AnnualSalaryService
{
    private readonly IAnnualSalaryRepository _repository;

    public AnnualSalaryService(IAnnualSalaryRepository repository)
    {
        _repository = repository;
    }

    public Task<AnnualSalaryOverviewDto> GetOverviewAsync(
        AnnualSalaryOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        _ = new DateOnly(query.Year, 1, 1);

        return _repository.GetOverviewAsync(query, cancellationToken);
    }
}
