using Microsoft.EntityFrameworkCore;
using Payroll.Application.Payroll;
using Payroll.Domain.Employees;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Infrastructure.Payroll;

public sealed class PayrollRunRepository : IPayrollRunRepository
{
    private readonly PayrollDbContext _dbContext;

    public PayrollRunRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PayrollRun>> ListFinalizedRunsAsync(
        int year,
        int fromMonth,
        int toMonth,
        CancellationToken cancellationToken)
    {
        _ = new DateOnly(year, fromMonth, 1);
        _ = new DateOnly(year, toMonth, 1);

        var runs = await _dbContext.PayrollRuns
            .AsNoTracking()
            .Include(run => run.Lines)
            .Where(run => run.Status == PayrollRunStatus.Finalized && EF.Functions.Like(run.PeriodKey, $"{year:D4}-%"))
            .ToListAsync(cancellationToken);

        return runs
            .Where(run => TryGetMonthFromPeriodKey(run.PeriodKey, out var month) && month >= fromMonth && month <= toMonth)
            .OrderBy(run => run.PeriodKey, StringComparer.Ordinal)
            .ToArray();
    }

    public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodAsync(
        Guid employeeId,
        string periodKey,
        CancellationToken cancellationToken)
    {
        return _dbContext.PayrollRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                run => run.PeriodKey == periodKey
                    && run.Status == PayrollRunStatus.Finalized
                    && run.Lines.Any(line => line.EmployeeId == employeeId),
                cancellationToken);
    }

    public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodForUpdateAsync(
        Guid employeeId,
        string periodKey,
        CancellationToken cancellationToken)
    {
        return _dbContext.PayrollRuns
            .SingleOrDefaultAsync(
                run => run.PeriodKey == periodKey
                    && run.Status == PayrollRunStatus.Finalized
                    && run.Lines.Any(line => line.EmployeeId == employeeId),
                cancellationToken);
    }

    public async Task<PayrollRun?> GetLatestRunForEmployeePeriodAsync(
        Guid employeeId,
        string periodKey,
        CancellationToken cancellationToken)
    {
        var runs = await _dbContext.PayrollRuns
            .AsNoTracking()
            .Where(run => run.PeriodKey == periodKey
                && run.Lines.Any(line => line.EmployeeId == employeeId))
            .ToListAsync(cancellationToken);

        return runs
            .OrderByDescending(run => run.Status == PayrollRunStatus.Finalized)
            .ThenByDescending(run => run.CreatedAtUtc)
            .FirstOrDefault();
    }

    public Task<bool> HasCancelledRunForEmployeePeriodAsync(
        Guid employeeId,
        string periodKey,
        CancellationToken cancellationToken)
    {
        return _dbContext.PayrollRuns
            .AsNoTracking()
            .AnyAsync(
                run => run.PeriodKey == periodKey
                    && run.Status == PayrollRunStatus.Cancelled
                    && run.Lines.Any(line => line.EmployeeId == employeeId),
                cancellationToken);
    }

    public async Task<PayrollRunMonthlyInputDto?> LoadMonthlyInputAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var record = await _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(
                record => record.EmployeeId == employeeId && record.Year == year && record.Month == month,
                cancellationToken);

        if (record is null)
        {
            return null;
        }

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);
        DepartmentOption? department = null;
        if (employee?.DepartmentOptionId is { } departmentId)
        {
            department = await _dbContext.DepartmentOptions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);
        }

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var contracts = await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract =>
                contract.EmployeeId == employeeId
                && contract.ValidFrom <= periodEnd
                && (!contract.ValidTo.HasValue || contract.ValidTo.Value >= periodStart))
            .ToListAsync(cancellationToken);

        return new PayrollRunMonthlyInputDto(
            record.EmployeeId,
            employee?.BirthDate,
            department?.Name,
            department?.IsGavMandatory ?? false,
            employee?.TaxStatus,
            employee?.IsSubjectToWithholdingTax == true,
            record,
            contracts.OrderByDescending(contract => contract.ValidFrom).FirstOrDefault());
    }

    public async Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollSettings.SingleOrDefaultAsync(cancellationToken)
            ?? new PayrollSettings();
    }

    public async Task<PayrollSettings> LoadPayrollSettingsForPeriodAsync(int year, int month, CancellationToken cancellationToken)
    {
        var fallbackSettings = await LoadCurrentPayrollSettingsAsync(cancellationToken);
        var referenceDate = new DateOnly(year, month, 1);
        var generalVersions = await _dbContext.PayrollGeneralSettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var hourlyVersions = await _dbContext.PayrollHourlySettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return PayrollSettingsVersionResolver.ResolveForDate(
            fallbackSettings,
            generalVersions,
            hourlyVersions,
            referenceDate);
    }

    public void Add(PayrollRun payrollRun)
    {
        _dbContext.PayrollRuns.Add(payrollRun);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool TryGetMonthFromPeriodKey(string periodKey, out int month)
    {
        month = 0;
        if (string.IsNullOrWhiteSpace(periodKey) || periodKey.Length < 7)
        {
            return false;
        }

        return int.TryParse(periodKey.AsSpan(5, 2), out month);
    }
}
