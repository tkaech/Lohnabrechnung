using Microsoft.EntityFrameworkCore;
using Payroll.Application.Imports;
using Payroll.Domain.Imports;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Imports;

public sealed class ImportExecutionStatusRepository : IImportExecutionStatusRepository
{
    private readonly PayrollDbContext _dbContext;

    public ImportExecutionStatusRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
    {
        return _dbContext.ImportExecutionStatuses.AnyAsync(
            item => item.Type == type && item.Year == year && item.Month == month,
            cancellationToken);
    }

    public async Task MarkImportedAsync(ImportConfigurationType type, int year, int month, DateTimeOffset importedAtUtc, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ImportExecutionStatuses
            .SingleOrDefaultAsync(item => item.Type == type && item.Year == year && item.Month == month, cancellationToken);

        if (existing is null)
        {
            _dbContext.ImportExecutionStatuses.Add(new ImportExecutionStatus(type, year, month, importedAtUtc));
        }
        else
        {
            _dbContext.Entry(existing).Property(nameof(ImportExecutionStatus.ImportedAtUtc)).CurrentValue = importedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ImportExecutionStatuses
            .Where(item => item.Type == type && item.Year == year && item.Month == month)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        _dbContext.ImportExecutionStatuses.RemoveRange(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
    {
        return await _dbContext.ImportExecutionStatuses
            .AsNoTracking()
            .Where(item => item.Type == type)
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .Select(item => new ImportedMonthStatusDto(item.Year, item.Month, item.ImportedAtUtc))
            .ToArrayAsync(cancellationToken);
    }
}
