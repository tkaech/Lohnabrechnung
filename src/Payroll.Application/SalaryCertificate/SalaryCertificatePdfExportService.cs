using System.Globalization;
using Payroll.Application.Settings;
using Payroll.Domain.SalaryCertificate;

namespace Payroll.Application.SalaryCertificate;

public sealed class SalaryCertificatePdfExportService
{
    private static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

    private readonly SalaryCertificateService _salaryCertificateService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly ISalaryCertificatePdfFormFieldReader _pdfFormFieldReader;
    private readonly ISalaryCertificatePdfDocumentWriter _pdfDocumentWriter;
    private readonly ISalaryCertificateRecordRepository _salaryCertificateRecordRepository;

    public SalaryCertificatePdfExportService(
        SalaryCertificateService salaryCertificateService,
        PayrollSettingsService payrollSettingsService,
        ISalaryCertificatePdfFormFieldReader pdfFormFieldReader,
        ISalaryCertificatePdfDocumentWriter pdfDocumentWriter,
        ISalaryCertificateRecordRepository salaryCertificateRecordRepository)
    {
        _salaryCertificateService = salaryCertificateService;
        _payrollSettingsService = payrollSettingsService;
        _pdfFormFieldReader = pdfFormFieldReader;
        _pdfDocumentWriter = pdfDocumentWriter;
        _salaryCertificateRecordRepository = salaryCertificateRecordRepository;
    }

    public async Task<string> ExportAsync(
        SalaryCertificatePdfExportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settings = await _payrollSettingsService.GetAsync(cancellationToken);
        var templatePath = Path.GetFullPath(settings.SalaryCertificatePdfTemplatePath);
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"Lohnausweis-Vorlage nicht gefunden: {templatePath}");
        }

        var outputPath = Path.GetFullPath(command.OutputPath);
        if (string.Equals(templatePath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Lohnausweis darf die Vorlage nicht ueberschreiben.");
        }

        var availablePdfFieldNames = await _pdfFormFieldReader.ReadFieldNamesAsync(templatePath, cancellationToken);
        var mappings = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping();
        var mappingValidation = SalaryCertificatePdfFieldMappingCatalog.ValidateRequiredFields(mappings, availablePdfFieldNames);
        if (mappingValidation.HasMissingRequiredFields)
        {
            throw new InvalidOperationException(BuildMappingValidationMessage(mappingValidation));
        }

        var certificate = await _salaryCertificateService.CreateAsync(
            new SalaryCertificateQuery(command.EmployeeId, command.Year),
            cancellationToken);
        var valuesByCode = certificate.Fields.ToDictionary(field => field.Code, StringComparer.Ordinal);

        var fields = mappings
            .Select(mapping => BuildFieldWrite(mapping, valuesByCode))
            .Where(field => field is not null)
            .Cast<SalaryCertificatePdfFieldWriteDto>()
            .ToArray();

        await _pdfDocumentWriter.WriteAsync(templatePath, outputPath, fields, cancellationToken);
        _salaryCertificateRecordRepository.Add(new SalaryCertificateRecord(command.EmployeeId, command.Year, outputPath));
        await _salaryCertificateRecordRepository.SaveChangesAsync(cancellationToken);
        return outputPath;
    }

    public Task<SalaryCertificateRecordDto?> GetLatestRecordAsync(
        SalaryCertificateRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        _ = new DateOnly(query.Year, 1, 1);

        return _salaryCertificateRecordRepository.GetLatestAsync(query.EmployeeId, query.Year, cancellationToken);
    }

    private static SalaryCertificatePdfFieldWriteDto? BuildFieldWrite(
        SalaryCertificatePdfFieldMappingDto mapping,
        IReadOnlyDictionary<string, SalaryCertificateFieldValueDto> valuesByCode)
    {
        if (!valuesByCode.TryGetValue(mapping.SalaryCertificateFieldCode, out var fieldValue))
        {
            throw new InvalidOperationException($"Lohnausweis-Feldwert fehlt: {mapping.SalaryCertificateFieldCode}");
        }

        var formattedValue = mapping.Format switch
        {
            SalaryCertificatePdfFieldFormat.Text => fieldValue.TextValue,
            SalaryCertificatePdfFieldFormat.Date => fieldValue.DateValue?.ToString("dd.MM.yyyy", SwissCulture),
            SalaryCertificatePdfFieldFormat.ChfAmount => fieldValue.AmountChf?.ToString("0.00", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Unbekanntes Lohnausweis-Format: {mapping.Format}")
        };

        if (string.IsNullOrWhiteSpace(formattedValue))
        {
            if (mapping.IsRequired)
            {
                throw new InvalidOperationException($"Pflichtfeld '{mapping.SalaryCertificateFieldCode}' hat keinen Wert.");
            }

            return null;
        }

        return new SalaryCertificatePdfFieldWriteDto(mapping.PdfFieldName, formattedValue);
    }

    private static string BuildMappingValidationMessage(SalaryCertificatePdfFieldMappingValidationDto validation)
    {
        var details = string.Join(
            "; ",
            validation.Issues.Select(issue => $"{issue.SalaryCertificateFieldCode}: {issue.Message}"));

        return $"Lohnausweis-PDF-Mapping unvollstaendig: {details}";
    }
}
