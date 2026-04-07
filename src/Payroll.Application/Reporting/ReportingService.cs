using System.Globalization;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;

namespace Payroll.Application.Reporting;

public sealed class ReportingService
{
    private static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

    private readonly EmployeeService _employeeService;
    private readonly MonthlyRecordService _monthlyRecordService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly IPdfExportService _pdfExportService;

    public ReportingService(
        EmployeeService employeeService,
        MonthlyRecordService monthlyRecordService,
        PayrollSettingsService payrollSettingsService,
        IPdfExportService pdfExportService)
    {
        _employeeService = employeeService;
        _monthlyRecordService = monthlyRecordService;
        _payrollSettingsService = payrollSettingsService;
        _pdfExportService = pdfExportService;
    }

    public async Task<string> CreatePayrollStatementPdfAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeService.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Mitarbeitender fuer PDF-Export nicht gefunden.");
        var monthlyRecord = await _monthlyRecordService.GetOrCreateAsync(
            new MonthlyRecordQuery(employeeId, year, month),
            cancellationToken);
        var settings = await _payrollSettingsService.GetAsync(cancellationToken);

        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var monthLabel = ToAsciiLabel(SwissCulture.TextInfo.ToTitleCase(periodStart.ToString("MMMM yyyy", SwissCulture)));
        var serviceYears = CalculateServiceYears(employee.EntryDate, periodEnd);
        var fileNameWithoutExtension = $"Lohnblatt_{employee.PersonnelNumber}_{year}_{month:00}";
        var placeholders = BuildTemplatePlaceholders(employee, monthlyRecord, settings, monthLabel, serviceYears);

        var document = new PayrollStatementPdfDocument(
            fileNameWithoutExtension,
            monthLabel,
            settings.CompanyAddress,
            string.IsNullOrWhiteSpace(settings.PrintTemplate) ? PayrollStatementTemplateProvider.LoadDefaultTemplate() : settings.PrintTemplate,
            placeholders,
            settings.PrintFontFamily,
            settings.PrintFontSize,
            settings.PrintTextColorHex,
            settings.PrintMutedTextColorHex,
            settings.PrintAccentColorHex,
            settings.PrintLogoText,
            settings.PrintLogoPath,
            employee.FirstName + " " + employee.LastName,
            BuildStreetLine(employee),
            employee.AddressLine2,
            $"{employee.PostalCode} {employee.City}",
            employee.AhvNumber,
            employee.DepartmentName,
            employee.EmploymentCategoryName,
            employee.EmploymentLocationName,
            DateOnly.FromDateTime(DateTime.Today).ToString("dd.MM.yyyy", SwissCulture),
            $"{monthlyRecord.Header.TotalWorkedHours:0.##}",
            $"{serviceYears}",
            monthlyRecord.PayrollPreview.Lines
                .Select(line => new PayrollStatementPdfLineDto(
                    line.Label,
                    line.Detail,
                    line.QuantityDisplay,
                    line.RateDisplay,
                    line.AmountDisplay,
                    line.IsEmphasized))
                .ToArray(),
            monthlyRecord.PayrollPreview.Notes);

        return await _pdfExportService.ExportPayrollStatementAsync(document, cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> BuildTemplatePlaceholders(
        EmployeeDetailsDto employee,
        MonthlyRecordDetailsDto monthlyRecord,
        PayrollSettingsDto settings,
        string monthLabel,
        int serviceYears)
    {
        var lineByLabel = monthlyRecord.PayrollPreview.Lines.ToDictionary(line => line.Label, StringComparer.OrdinalIgnoreCase);
        var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Firmenname"] = FirstNonEmptyLine(settings.CompanyAddress, "PayrollApp"),
            ["Firmenadresse"] = NormalizeMultiline(settings.CompanyAddress, "-"),
            ["Logo"] = ResolveLogo(settings),
            ["MitarbeiterName"] = employee.FirstName + " " + employee.LastName,
            ["MitarbeiterAdresse"] = NormalizeMultiline(
                string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        BuildStreetLine(employee),
                        employee.AddressLine2,
                        $"{employee.PostalCode} {employee.City}"
                    }.Where(line => !string.IsNullOrWhiteSpace(line))),
                "-"),
            ["MitarbeiterBlock"] = NormalizeMultiline(
                string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        employee.FirstName + " " + employee.LastName,
                        BuildStreetLine(employee),
                        employee.AddressLine2,
                        $"{employee.PostalCode} {employee.City}"
                    }.Where(line => !string.IsNullOrWhiteSpace(line))),
                "-"),
            ["AhvNummer"] = Fallback(employee.AhvNumber),
            ["Monat"] = monthLabel,
            ["Abteilung"] = Fallback(employee.DepartmentName),
            ["Anstellungskategorie"] = Fallback(employee.EmploymentCategoryName),
            ["Anstellungsort"] = Fallback(employee.EmploymentLocationName),
            ["Abrechnungsdatum"] = DateOnly.FromDateTime(DateTime.Today).ToString("dd.MM.yyyy", SwissCulture),
            ["Gesamtstunden"] = $"{monthlyRecord.Header.TotalWorkedHours:0.##}",
            ["Dienstjahre"] = $"{serviceYears}",
            ["Hinweise"] = NormalizeMultiline(string.Join(Environment.NewLine, monthlyRecord.PayrollPreview.Notes), "-")
        };

        AddLinePlaceholders(placeholders, lineByLabel, "Basislohn", "Basislohn");
        AddLinePlaceholders(placeholders, lineByLabel, "Stunden mit Zeitzuschlag", "Zeitzuschlag");
        AddLinePlaceholders(placeholders, lineByLabel, "Spezialzuschlag gemaess Vertrag", "Spezialzuschlag");
        AddLinePlaceholders(placeholders, lineByLabel, "Fahrzeitentschaedigung Pauschalzone 1", "Pauschalzone1");
        AddLinePlaceholders(placeholders, lineByLabel, "Fahrzeitentschaedigung Pauschalzone 2", "Pauschalzone2");
        AddLinePlaceholders(placeholders, lineByLabel, "Fahrzeitentschaedigung Regiezone", "Regiezone1");
        AddLinePlaceholders(placeholders, lineByLabel, "Zwischentotal", "Zwischentotal");
        AddLinePlaceholders(placeholders, lineByLabel, "Ferienentschaedigung", "Ferienentschaedigung");
        AddLinePlaceholders(placeholders, lineByLabel, "AHV-pflichtiger Bruttolohn", "AhvPflichtigerBruttolohn");
        AddLinePlaceholders(placeholders, lineByLabel, "AHV/IV/EO", "AhvIveo");
        AddLinePlaceholders(placeholders, lineByLabel, "ALV", "Alv");
        AddLinePlaceholders(placeholders, lineByLabel, "Krankentaggeld/UVG", "KtgUvg");
        AddLinePlaceholders(placeholders, lineByLabel, "BVG", "Bvg");
        AddLinePlaceholders(placeholders, lineByLabel, "Aus- und Weiterbildungskosten inkl. Ferienentschaedigung", "AusUndWeiterbildung");
        AddLinePlaceholders(placeholders, lineByLabel, "Spesen gemaess Nachweis", "Spesen");
        AddLinePlaceholders(placeholders, lineByLabel, "Total", "Total");
        AddLinePlaceholders(placeholders, lineByLabel, "Total Auszahlung", "TotalAuszahlung");

        return placeholders;
    }

    private static void AddLinePlaceholders(
        IDictionary<string, string> placeholders,
        IReadOnlyDictionary<string, MonthlyPayrollPreviewLineDto> lines,
        string label,
        string token)
    {
        if (!lines.TryGetValue(label, out var line))
        {
            placeholders[token] = "-";
            placeholders[token + "Menge"] = "-";
            placeholders[token + "Ansatz"] = "-";
            placeholders[token + "Detail"] = string.Empty;
            return;
        }

        placeholders[token] = Fallback(line.AmountDisplay);
        placeholders[token + "Menge"] = Fallback(line.QuantityDisplay);
        placeholders[token + "Ansatz"] = Fallback(line.RateDisplay);
        placeholders[token + "Detail"] = line.Detail?.Trim() ?? string.Empty;
    }

    private static string BuildStreetLine(EmployeeDetailsDto employee)
    {
        return string.IsNullOrWhiteSpace(employee.HouseNumber)
            ? employee.Street
            : $"{employee.Street} {employee.HouseNumber}";
    }

    private static int CalculateServiceYears(DateOnly entryDate, DateOnly periodEnd)
    {
        var years = periodEnd.Year - entryDate.Year;
        if (entryDate.AddYears(years) > periodEnd)
        {
            years--;
        }

        return Math.Max(0, years);
    }

    private static string ToAsciiLabel(string value)
    {
        return value
            .Replace("ä", "ae", StringComparison.Ordinal)
            .Replace("ö", "oe", StringComparison.Ordinal)
            .Replace("ü", "ue", StringComparison.Ordinal)
            .Replace("Ä", "Ae", StringComparison.Ordinal)
            .Replace("Ö", "Oe", StringComparison.Ordinal)
            .Replace("Ü", "Ue", StringComparison.Ordinal);
    }

    private static string ResolveLogo(PayrollSettingsDto settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PrintLogoText))
        {
            return settings.PrintLogoText.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.PrintLogoPath))
        {
            return Path.GetFileNameWithoutExtension(settings.PrintLogoPath.Trim());
        }

        return "PA";
    }

    private static string NormalizeMultiline(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return string.Join(
            Environment.NewLine,
            value.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static string FirstNonEmptyLine(string? value, string fallback)
    {
        return NormalizeMultiline(value, fallback)
            .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? fallback;
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
