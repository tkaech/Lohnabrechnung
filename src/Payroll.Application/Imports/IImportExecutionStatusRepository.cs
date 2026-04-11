using Payroll.Domain.Imports;

namespace Payroll.Application.Imports;

public interface IImportExecutionStatusRepository
{
    Task<bool> ExistsAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken);
    Task MarkImportedAsync(ImportConfigurationType type, int year, int month, DateTimeOffset importedAtUtc, CancellationToken cancellationToken);
    Task DeleteAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken);
}
