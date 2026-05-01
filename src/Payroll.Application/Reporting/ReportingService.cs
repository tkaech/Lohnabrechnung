using System.Globalization;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Payroll;
using Payroll.Application.Settings;
using Payroll.Domain.Payroll;

namespace Payroll.Application.Reporting;

public sealed class ReportingService
{
    private static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

    private readonly EmployeeService _employeeService;
    private readonly MonthlyRecordService _monthlyRecordService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IPayrollRunRepository? _payrollRunRepository;

    public ReportingService(
        EmployeeService employeeService,
        MonthlyRecordService monthlyRecordService,
        PayrollSettingsService payrollSettingsService,
        IPdfExportService pdfExportService)
        : this(employeeService, monthlyRecordService, payrollSettingsService, pdfExportService, null)
    {
    }

    public ReportingService(
        EmployeeService employeeService,
        MonthlyRecordService monthlyRecordService,
        PayrollSettingsService payrollSettingsService,
        IPdfExportService pdfExportService,
        IPayrollRunRepository? payrollRunRepository)
    {
        _employeeService = employeeService;
        _monthlyRecordService = monthlyRecordService;
        _payrollSettingsService = payrollSettingsService;
        _pdfExportService = pdfExportService;
        _payrollRunRepository = payrollRunRepository;
    }

    public async Task<string> CreatePayrollStatementPdfAsync(
        Guid employeeId,
        int year,
        int month,
        DateOnly paymentDate,
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
        var placeholders = BuildTemplatePlaceholders(employee, monthlyRecord, settings, monthLabel, serviceYears, paymentDate);

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
            paymentDate.ToString("dd.MM.yyyy", SwissCulture),
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

    public async Task<PayrollTotalsReportDto> GetPayrollTotalsAsync(
        PayrollTotalsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _ = new DateOnly(query.Year, query.FromMonth, 1);
        _ = new DateOnly(query.Year, query.ToMonth, 1);
        if (query.FromMonth > query.ToMonth)
        {
            throw new InvalidOperationException("Von-Monat darf nicht groesser als Bis-Monat sein.");
        }

        if (_payrollRunRepository is null)
        {
            return new PayrollTotalsReportDto(query.Year, query.FromMonth, query.ToMonth, [], []);
        }

        var runs = await _payrollRunRepository.ListFinalizedRunsAsync(query.Year, query.FromMonth, query.ToMonth, cancellationToken);
        var includedMonths = runs
            .Select(run => TryGetMonthFromPeriodKey(run.PeriodKey, out var month) ? month : 0)
            .Where(month => month is >= 1 and <= 12)
            .Distinct()
            .OrderBy(month => month)
            .ToArray();

        if (runs.Count == 0)
        {
            return new PayrollTotalsReportDto(query.Year, query.FromMonth, query.ToMonth, includedMonths, []);
        }

        var allLines = runs.SelectMany(run => run.Lines).ToArray();
        var subtotalChf = SumAmounts(allLines, line =>
            line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.MonthlySalary
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                || IsVehicleCode(line.Code));
        var ahvGrossChf = SumAmounts(allLines, line =>
            line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.MonthlySalary
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VacationCompensation
                or PayrollLineType.VehicleCompensation);
        var totalChf = SumAmounts(allLines, line => line.LineType != PayrollLineType.Expense);
        var totalPayoutChf = runs.Sum(run => RoundToFiveRappen(run.Lines.Sum(line => line.AmountChf)));

        var lines = new List<PayrollTotalsLineDto>();
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.BaseSalaryCode, "Basislohn",
            SumAmounts(allLines, line => line.LineType is PayrollLineType.BaseHours or PayrollLineType.MonthlySalary));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.TimeSupplementCode, "Stunden mit Zeitzuschlag",
            SumAmounts(allLines, line => line.LineType is PayrollLineType.NightSupplement or PayrollLineType.SundaySupplement or PayrollLineType.HolidaySupplement));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.SpecialSupplementCode, "Spezialzuschlag gemaess Vertrag",
            SumAmounts(allLines, line => line.LineType == PayrollLineType.SpecialSupplement));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.VehiclePauschalzone1Code, "Fahrzeitentschaedigung Pauschalzone 1",
            SumAmounts(allLines, line => string.Equals(line.Code, "VEHICLE_P1", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.VehiclePauschalzone2Code, "Fahrzeitentschaedigung Pauschalzone 2",
            SumAmounts(allLines, line => string.Equals(line.Code, "VEHICLE_P2", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.VehicleRegiezone1Code, "Fahrzeitentschaedigung Regiezone",
            SumAmounts(allLines, line => string.Equals(line.Code, "VEHICLE_R1", StringComparison.Ordinal)));
        lines.Add(new PayrollTotalsLineDto(PayrollPreviewHelpCatalog.SubtotalCode, "Zwischentotal", subtotalChf, true));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.VacationCompensationCode, "Ferienentschaedigung",
            SumAmounts(allLines, line => line.LineType == PayrollLineType.VacationCompensation));
        lines.Add(new PayrollTotalsLineDto(PayrollPreviewHelpCatalog.AhvGrossCode, "AHV-pflichtiger Bruttolohn", ahvGrossChf, true));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.AhvIvEoCode, "AHV/IV/EO",
            SumAmounts(allLines, line => string.Equals(line.Code, "AHV_IV_EO", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.AlvCode, "ALV",
            SumAmounts(allLines, line => string.Equals(line.Code, "ALV", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.KtgUvgCode, "Krankentaggeld/UVG",
            SumAmounts(allLines, line => string.Equals(line.Code, "KTG_UVG", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.TrainingAndHolidayCode, "Aus- und Weiterbildungskosten inkl. Ferienentschaedigung",
            SumAmounts(allLines, line => string.Equals(line.Code, "AUSBILDUNG_FERIEN", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, "WITHHOLDING_TAX", "Quellensteuer",
            SumAmounts(allLines, line => string.Equals(line.Code, "WITHHOLDING_TAX", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, "WITHHOLDING_TAX_CORRECTION", "Quellensteuer Korrektur / Rueckzahlung",
            SumAmounts(allLines, line => string.Equals(line.Code, "WITHHOLDING_TAX_CORRECTION", StringComparison.Ordinal)));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.BvgCode, "BVG",
            SumAmounts(allLines, line => line.LineType == PayrollLineType.BvgDeduction));
        lines.Add(new PayrollTotalsLineDto(PayrollPreviewHelpCatalog.TotalCode, "Total", totalChf, true));
        AddLineIfNonZero(lines, PayrollPreviewHelpCatalog.ExpensesCode, "Spesen gemaess Nachweis",
            SumAmounts(allLines, line => line.LineType == PayrollLineType.Expense));
        lines.Add(new PayrollTotalsLineDto(PayrollPreviewHelpCatalog.TotalPayoutCode, "Total Auszahlung", totalPayoutChf, true));

        return new PayrollTotalsReportDto(
            query.Year,
            query.FromMonth,
            query.ToMonth,
            includedMonths,
            lines
                .OrderBy(line => GetTotalsSortOrder(line.Code))
                .ThenBy(line => line.Label, StringComparer.Ordinal)
                .ToArray());
    }

    private static IReadOnlyDictionary<string, string> BuildTemplatePlaceholders(
        EmployeeDetailsDto employee,
        MonthlyRecordDetailsDto monthlyRecord,
        PayrollSettingsDto settings,
        string monthLabel,
        int serviceYears,
        DateOnly paymentDate)
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
            ["Abrechnungsdatum"] = paymentDate.ToString("dd.MM.yyyy", SwissCulture),
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

    private static decimal SumAmounts(IEnumerable<PayrollRunLine> lines, Func<PayrollRunLine, bool> predicate)
    {
        return lines.Where(predicate).Sum(line => line.AmountChf);
    }

    private static void AddLineIfNonZero(ICollection<PayrollTotalsLineDto> lines, string code, string label, decimal amountChf)
    {
        if (amountChf == 0m)
        {
            return;
        }

        lines.Add(new PayrollTotalsLineDto(code, label, amountChf, false));
    }

    private static bool IsVehicleCode(string code)
    {
        return string.Equals(code, "VEHICLE_P1", StringComparison.Ordinal)
            || string.Equals(code, "VEHICLE_P2", StringComparison.Ordinal)
            || string.Equals(code, "VEHICLE_R1", StringComparison.Ordinal);
    }

    private static int GetTotalsSortOrder(string code)
    {
        return code switch
        {
            PayrollPreviewHelpCatalog.BaseSalaryCode => 10,
            PayrollPreviewHelpCatalog.TimeSupplementCode => 20,
            PayrollPreviewHelpCatalog.SpecialSupplementCode => 30,
            PayrollPreviewHelpCatalog.VehiclePauschalzone1Code => 40,
            PayrollPreviewHelpCatalog.VehiclePauschalzone2Code => 50,
            PayrollPreviewHelpCatalog.VehicleRegiezone1Code => 60,
            PayrollPreviewHelpCatalog.SubtotalCode => 70,
            PayrollPreviewHelpCatalog.VacationCompensationCode => 80,
            PayrollPreviewHelpCatalog.AhvGrossCode => 90,
            PayrollPreviewHelpCatalog.AhvIvEoCode => 100,
            PayrollPreviewHelpCatalog.AlvCode => 110,
            PayrollPreviewHelpCatalog.KtgUvgCode => 120,
            PayrollPreviewHelpCatalog.TrainingAndHolidayCode => 130,
            "WITHHOLDING_TAX" => 140,
            "WITHHOLDING_TAX_CORRECTION" => 145,
            PayrollPreviewHelpCatalog.BvgCode => 150,
            PayrollPreviewHelpCatalog.TotalCode => 160,
            PayrollPreviewHelpCatalog.ExpensesCode => 170,
            PayrollPreviewHelpCatalog.TotalPayoutCode => 180,
            _ => 999
        };
    }

    private static decimal RoundToFiveRappen(decimal value)
    {
        return Math.Round(value * 20m, MidpointRounding.AwayFromZero) / 20m;
    }

    private static bool TryGetMonthFromPeriodKey(string periodKey, out int month)
    {
        month = 0;
        if (string.IsNullOrWhiteSpace(periodKey) || periodKey.Length < 7)
        {
            return false;
        }

        return int.TryParse(periodKey.AsSpan(5, 2), out month);
    }
}
