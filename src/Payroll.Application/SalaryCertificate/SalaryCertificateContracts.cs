namespace Payroll.Application.SalaryCertificate;

public sealed record SalaryCertificateQuery(
    Guid EmployeeId,
    int Year);

public sealed record SalaryCertificatePdfExportCommand(
    Guid EmployeeId,
    int Year,
    string OutputPath);

public sealed record SalaryCertificateRecordQuery(
    Guid EmployeeId,
    int Year);

public sealed record SalaryCertificateRecordDto(
    Guid Id,
    Guid EmployeeId,
    int Year,
    DateTimeOffset CreatedAtUtc,
    string? OutputFilePath,
    string? FileHash);

public sealed record SalaryCertificateDto(
    Guid EmployeeId,
    string PersonnelNumber,
    string FirstName,
    string LastName,
    int Year,
    IReadOnlyCollection<SalaryCertificateFieldValueDto> Fields);

public sealed record SalaryCertificateFieldValueDto(
    string Code,
    string Label,
    decimal? AmountChf = null,
    string? TextValue = null,
    DateOnly? DateValue = null);

public sealed record SalaryCertificatePdfFieldWriteDto(
    string PdfFieldName,
    string Value);

public enum SalaryCertificatePdfFieldFormat
{
    Text,
    Date,
    ChfAmount
}

public sealed record SalaryCertificatePdfFieldMappingDto(
    string SalaryCertificateFieldCode,
    string PdfFieldName,
    SalaryCertificatePdfFieldFormat Format,
    bool IsRequired);

public sealed record SalaryCertificatePdfFieldMappingIssueDto(
    string SalaryCertificateFieldCode,
    string PdfFieldName,
    string Message);

public sealed record SalaryCertificatePdfFieldMappingValidationDto(
    IReadOnlyCollection<SalaryCertificatePdfFieldMappingIssueDto> Issues)
{
    public bool HasMissingRequiredFields => Issues.Count > 0;
}
