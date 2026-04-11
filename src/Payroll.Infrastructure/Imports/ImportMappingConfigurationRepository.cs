using Microsoft.EntityFrameworkCore;
using Payroll.Application.Imports;
using Payroll.Domain.Imports;
using Payroll.Infrastructure.Persistence;
using System.Text.Json;

namespace Payroll.Infrastructure.Imports;

public sealed class ImportMappingConfigurationRepository : IImportMappingConfigurationRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly PayrollDbContext _dbContext;

    public ImportMappingConfigurationRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ImportConfigurationListItemDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
    {
        return await _dbContext.ImportMappingConfigurations
            .AsNoTracking()
            .Where(item => item.Type == type)
            .OrderBy(item => item.Name)
            .Select(item => new ImportConfigurationListItemDto(item.Id, item.Name))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ImportConfigurationDto?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ImportMappingConfigurations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == configurationId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<ImportConfigurationDto> SaveAsync(SaveImportConfigurationCommand command, CancellationToken cancellationToken)
    {
        var normalizedName = command.Name.Trim();
        var entity = command.ConfigurationId.HasValue
            ? await _dbContext.ImportMappingConfigurations.SingleOrDefaultAsync(item => item.Id == command.ConfigurationId.Value, cancellationToken)
            : await _dbContext.ImportMappingConfigurations.SingleOrDefaultAsync(item => item.Type == command.Type && item.Name == normalizedName, cancellationToken);

        var mappingsJson = JsonSerializer.Serialize(
            command.Mappings
                .Where(item => !string.IsNullOrWhiteSpace(item.FieldKey))
                .Select(item => new ImportFieldMappingDto(item.FieldKey.Trim(), item.CsvColumnName?.Trim() ?? string.Empty, item.AllowEmpty))
                .ToArray(),
            JsonSerializerOptions);

        if (entity is null)
        {
            entity = new ImportMappingConfiguration(
                command.Type,
                normalizedName,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier,
                mappingsJson);
            _dbContext.ImportMappingConfigurations.Add(entity);
        }
        else
        {
            entity.Update(
                normalizedName,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier,
                mappingsJson);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static ImportConfigurationDto ToDto(ImportMappingConfiguration entity)
    {
        IReadOnlyCollection<ImportFieldMappingDto> mappings;
        try
        {
            mappings = JsonSerializer.Deserialize<ImportFieldMappingDto[]>(entity.FieldMappingsJson, JsonSerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            mappings = [];
        }

        return new ImportConfigurationDto(
            entity.Id,
            entity.Type,
            entity.Name,
            entity.Delimiter,
            entity.FieldsEnclosed,
            entity.TextQualifier,
            mappings);
    }
}
