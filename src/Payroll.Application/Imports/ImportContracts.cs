using Payroll.Domain.Imports;

namespace Payroll.Application.Imports;

public sealed record ImportFieldDefinitionDto(
    string FieldKey,
    string Label,
    bool IsRequired);

public sealed record ImportFieldMappingDto(
    string FieldKey,
    string CsvColumnName,
    bool AllowEmpty);

public sealed record ImportConfigurationListItemDto(
    Guid ConfigurationId,
    string Name);

public sealed record ImportConfigurationDto(
    Guid ConfigurationId,
    ImportConfigurationType Type,
    string Name,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings);

public sealed record SaveImportConfigurationCommand(
    Guid? ConfigurationId,
    ImportConfigurationType Type,
    string Name,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings);

public sealed record ReadCsvImportDocumentCommand(
    string FilePath,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier);

public sealed record CsvImportDocumentDto(
    IReadOnlyCollection<string> Headers,
    IReadOnlyCollection<IReadOnlyDictionary<string, string>> Rows);

public sealed record ImportValidationResultDto(
    bool IsValid,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings);

public sealed record ImportPersonDataCommand(
    string FilePath,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings,
    IReadOnlyCollection<int>? SelectedRowNumbers = null);

public sealed record PreviewPersonDataCommand(
    string FilePath,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings);

public sealed record PersonImportPreviewItemDto(
    int RowNumber,
    string PersonnelNumber,
    string FullName,
    bool AlreadyExists);

public sealed record PersonDataImportResultDto(
    int CreatedCount,
    int UpdatedCount,
    int ErrorCount,
    IReadOnlyCollection<string> Messages);

public sealed record ImportTimeDataCommand(
    string FilePath,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    int Year,
    int Month,
    bool OverwriteExistingMonth,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings,
    IReadOnlyCollection<int>? SelectedRowNumbers = null);

public sealed record PreviewTimeDataCommand(
    string FilePath,
    string Delimiter,
    bool FieldsEnclosed,
    string TextQualifier,
    int Year,
    int Month,
    IReadOnlyCollection<ImportFieldMappingDto> Mappings);

public sealed record TimeImportPreviewItemDto(
    int RowNumber,
    string PersonnelNumber,
    string FullName,
    bool EmployeeMatched,
    bool MonthlyDataExists,
    string Status);

public sealed record TimeDataImportResultDto(
    int ImportedCount,
    int ErrorCount,
    IReadOnlyCollection<string> Messages);

public sealed record ImportedMonthStatusDto(
    int Year,
    int Month,
    DateTimeOffset ImportedAtUtc)
{
    public string MonthKey => $"{Year:D4}-{Month:D2}";
}
