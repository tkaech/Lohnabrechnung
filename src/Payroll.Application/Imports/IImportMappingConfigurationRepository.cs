using Payroll.Domain.Imports;

namespace Payroll.Application.Imports;

public interface IImportMappingConfigurationRepository
{
    Task<IReadOnlyCollection<ImportConfigurationListItemDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken);
    Task<ImportConfigurationDto?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken);
    Task<ImportConfigurationDto> SaveAsync(SaveImportConfigurationCommand command, CancellationToken cancellationToken);
}
