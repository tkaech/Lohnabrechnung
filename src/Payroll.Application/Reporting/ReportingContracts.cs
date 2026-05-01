namespace Payroll.Application.Reporting;

public sealed record PayrollStatementPdfLineDto(
    string Label,
    string? Detail,
    string QuantityDisplay,
    string RateDisplay,
    string AmountDisplay,
    bool IsEmphasized);

public sealed record PayrollStatementPdfDocument(
    string FileNameWithoutExtension,
    string MonthLabel,
    string CompanyAddress,
    string TemplateContent,
    IReadOnlyDictionary<string, string> TemplatePlaceholders,
    string PrintFontFamily,
    decimal PrintFontSize,
    string PrintTextColorHex,
    string PrintMutedTextColorHex,
    string PrintAccentColorHex,
    string PrintLogoText,
    string PrintLogoPath,
    string EmployeeFullName,
    string? EmployeeAddressLine1,
    string? EmployeeAddressLine2,
    string? EmployeeAddressLine3,
    string? AhvNumber,
    string? DepartmentName,
    string? EmploymentCategoryName,
    string? EmploymentLocationName,
    string PayrollDateLabel,
    string TotalHoursLabel,
    string ServiceYearsLabel,
    IReadOnlyCollection<PayrollStatementPdfLineDto> Lines,
    IReadOnlyCollection<string> Notes);

public sealed record PayrollTotalsQuery(
    int Year,
    int FromMonth,
    int ToMonth);

public sealed record PayrollTotalsLineDto(
    string Code,
    string Label,
    decimal AmountChf,
    bool IsEmphasized);

public sealed record PayrollTotalsReportDto(
    int Year,
    int FromMonth,
    int ToMonth,
    IReadOnlyCollection<int> IncludedMonths,
    IReadOnlyCollection<PayrollTotalsLineDto> Lines);
