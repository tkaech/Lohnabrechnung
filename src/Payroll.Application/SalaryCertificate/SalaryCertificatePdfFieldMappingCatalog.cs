namespace Payroll.Application.SalaryCertificate;

public static class SalaryCertificatePdfFieldMappingCatalog
{
    public static IReadOnlyCollection<SalaryCertificatePdfFieldMappingDto> CreateInitialMapping()
    {
        return
        [
            Required(SalaryCertificateFieldCodes.CertificateYear, "TextLinks_D", SalaryCertificatePdfFieldFormat.Text),
            Required(SalaryCertificateFieldCodes.EmployeeAhvNumber, "AHVLinks_C", SalaryCertificatePdfFieldFormat.Text),
            Required(SalaryCertificateFieldCodes.EmployeeBirthDate, "TextLinks_C-GebDatum", SalaryCertificatePdfFieldFormat.Date),
            Required(SalaryCertificateFieldCodes.SalaryWageCode1, "DezZahlNull_1", SalaryCertificatePdfFieldFormat.ChfAmount),
            Required(SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8, "DezZahlNull_8", SalaryCertificatePdfFieldFormat.ChfAmount),
            Required(SalaryCertificateFieldCodes.DeductionsSocialSecurityCode9, "DezZahlNull_9", SalaryCertificatePdfFieldFormat.ChfAmount),
            Required(SalaryCertificateFieldCodes.DeductionsPensionFundCode10, "DezZahlNull_10_1", SalaryCertificatePdfFieldFormat.ChfAmount),
            Required(SalaryCertificateFieldCodes.SalaryNetWageCode11, "DezZahlNull_11", SalaryCertificatePdfFieldFormat.ChfAmount),
            Optional(SalaryCertificateFieldCodes.TaxSourceTaxCode12, "DezZahlNull_12", SalaryCertificatePdfFieldFormat.ChfAmount),
            Optional(SalaryCertificateFieldCodes.ExpensesCode13, "DezZahlNull_13_1_2", SalaryCertificatePdfFieldFormat.ChfAmount)
        ];
    }

    public static SalaryCertificatePdfFieldMappingValidationDto ValidateRequiredFields(
        IReadOnlyCollection<SalaryCertificatePdfFieldMappingDto> mappings,
        IReadOnlyCollection<string> availablePdfFieldNames)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(availablePdfFieldNames);

        var availableFieldNames = availablePdfFieldNames.ToHashSet(StringComparer.Ordinal);
        var issues = mappings
            .Where(mapping => mapping.IsRequired)
            .Where(mapping => string.IsNullOrWhiteSpace(mapping.PdfFieldName)
                || !availableFieldNames.Contains(mapping.PdfFieldName))
            .Select(mapping => new SalaryCertificatePdfFieldMappingIssueDto(
                mapping.SalaryCertificateFieldCode,
                mapping.PdfFieldName,
                string.IsNullOrWhiteSpace(mapping.PdfFieldName)
                    ? "PDF-Feldname fehlt."
                    : "PDF-Feldname wurde in der Vorlage nicht gefunden."))
            .ToArray();

        return new SalaryCertificatePdfFieldMappingValidationDto(issues);
    }

    private static SalaryCertificatePdfFieldMappingDto Required(
        string fieldCode,
        string pdfFieldName,
        SalaryCertificatePdfFieldFormat format)
    {
        return new SalaryCertificatePdfFieldMappingDto(fieldCode, pdfFieldName, format, true);
    }

    private static SalaryCertificatePdfFieldMappingDto Optional(
        string fieldCode,
        string pdfFieldName,
        SalaryCertificatePdfFieldFormat format)
    {
        return new SalaryCertificatePdfFieldMappingDto(fieldCode, pdfFieldName, format, false);
    }
}
