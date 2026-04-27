namespace Payroll.Application.SalaryCertificate;

public sealed record SalaryCertificateQuery(
    Guid EmployeeId,
    int Year);

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

public sealed record SalaryCertificatePdfFieldMappingDto(
    string FieldCode,
    string PdfFieldName);
