using Microsoft.EntityFrameworkCore;
using Payroll.Application.MonthlyRecords;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.MonthlyRecords;

public sealed class EmployeeMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
{
    private readonly PayrollDbContext _dbContext;
    private readonly PayrollRunLineDerivationService _payrollRunLineDerivationService = new();

    public EmployeeMonthlyRecordRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        var existingRecord = await _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(
                record => record.EmployeeId == employeeId && record.Year == year && record.Month == month,
                cancellationToken);

        if (existingRecord is not null)
        {
            return existingRecord;
        }

        var createdRecord = new EmployeeMonthlyRecord(employeeId, year, month);
        _dbContext.EmployeeMonthlyRecords.Add(createdRecord);
        return createdRecord;
    }

    public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        return _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        var monthlyRecord = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);

        if (monthlyRecord is null)
        {
            return null;
        }

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleAsync(item => item.Id == monthlyRecord.EmployeeId, cancellationToken);

        var contract = await LoadRelevantContractAsync(monthlyRecord.EmployeeId, monthlyRecord.PeriodStart, monthlyRecord.PeriodEnd, cancellationToken);
        var payrollSettings = await _dbContext.PayrollSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PayrollSettings();

        var previewNotes = BuildPreviewNotes(monthlyRecord, contract is not null);
        var previewRows = await BuildPreviewRowsAsync(monthlyRecord.EmployeeId, cancellationToken);
        var payrollPreview = BuildPayrollPreview(monthlyRecord, contract, payrollSettings);
        var employeeMonthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == monthlyRecord.EmployeeId)
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .ToListAsync(cancellationToken);

        var header = new MonthlyRecordHeaderDto(
            monthlyRecord.Id,
            monthlyRecord.EmployeeId,
            employee.FullName,
            monthlyRecord.Year,
            monthlyRecord.Month,
            monthlyRecord.Status,
            contract?.ValidFrom,
            contract?.ValidTo,
            contract?.HourlyRateChf,
            contract?.MonthlyBvgDeductionChf,
            monthlyRecord.TimeEntries.Sum(entry => entry.HoursWorked),
            monthlyRecord.TimeEntries.Sum(entry => entry.SupplementHours),
            monthlyRecord.ExpenseEntry?.ExpensesTotalChf ?? 0m,
            monthlyRecord.TimeEntries.Sum(entry => entry.VehicleCompensationTotalChf));

        var timeEntries = monthlyRecord.TimeEntries
            .OrderBy(entry => entry.WorkDate)
            .Select(entry => new MonthlyTimeEntryDto(
                entry.Id,
                entry.WorkDate,
                entry.HoursWorked,
                entry.NightHours,
                entry.SundayHours,
                entry.HolidayHours,
                entry.VehiclePauschalzone1Chf,
                entry.VehiclePauschalzone2Chf,
                entry.VehicleRegiezone1Chf,
                entry.Note))
            .ToArray();

        var timeEntryHistory = employeeMonthlyRecords
            .SelectMany(record => record.TimeEntries)
            .OrderBy(entry => entry.WorkDate)
            .ThenBy(entry => entry.Id)
            .Select(entry => new MonthlyTimeEntryDto(
                entry.Id,
                entry.WorkDate,
                entry.HoursWorked,
                entry.NightHours,
                entry.SundayHours,
                entry.HolidayHours,
                entry.VehiclePauschalzone1Chf,
                entry.VehiclePauschalzone2Chf,
                entry.VehicleRegiezone1Chf,
                entry.Note))
            .ToArray();

        var expenseEntry = monthlyRecord.ExpenseEntry is null
            ? null
            : new MonthlyExpenseEntryDto(
                monthlyRecord.ExpenseEntry.Id,
                monthlyRecord.ExpenseEntry.ExpensesTotalChf);

        var expenseEntryHistory = employeeMonthlyRecords
            .Where(record => record.ExpenseEntry is not null)
            .OrderBy(record => record.Year)
            .ThenBy(record => record.Month)
            .Select(record => new HistoricalMonthlyExpenseEntryDto(
                record.ExpenseEntry!.Id,
                record.Year,
                record.Month,
                record.ExpenseEntry.ExpensesTotalChf))
            .ToArray();

        return new MonthlyRecordDetailsDto(
            header,
            timeEntries,
            timeEntryHistory,
            expenseEntry,
            expenseEntryHistory,
            new MonthlyRecordPreviewDto(previewRows, previewNotes),
            payrollPreview);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }

    public void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class
    {
        _dbContext.Entry(entity).State = EntityState.Added;
    }

    private async Task<Domain.Employees.EmploymentContract?> LoadRelevantContractAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var contracts = await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract =>
                contract.EmployeeId == employeeId
                && contract.ValidFrom <= periodEnd
                && (!contract.ValidTo.HasValue || contract.ValidTo.Value >= periodStart))
            .ToListAsync(cancellationToken);

        return contracts
            .OrderByDescending(contract => contract.ValidFrom)
            .FirstOrDefault();
    }

    private static IReadOnlyCollection<string> BuildPreviewNotes(EmployeeMonthlyRecord monthlyRecord, bool hasContract)
    {
        var notes = new List<string>();

        if (!hasContract)
        {
            notes.Add("Kein Vertragsstand fuer den gewaelten Monat gefunden.");
        }

        var workSummary = PayrollWorkSummary.FromTimeEntries(
            monthlyRecord.EmployeeId,
            monthlyRecord.TimeEntries);

        if (workSummary.HasAmbiguousSpecialHourOverlap)
        {
            notes.Add("Spezialstunden uebersteigen die Arbeitsstunden. Offene Ueberlappungsregel bleibt sichtbar.");
        }

        if (monthlyRecord.ExpenseEntry is null)
        {
            notes.Add("Keine Spesen im aktuellen Monat erfasst.");
        }

        if (monthlyRecord.TimeEntries.All(entry => entry.VehicleCompensationTotalChf == 0m))
        {
            notes.Add("Keine Fahrzeugentschaedigung im aktuellen Monat erfasst.");
        }

        if (notes.Count == 0)
        {
            notes.Add("Monatsvorschau zeigt derzeit Verdichtung ohne automatische Payroll-Berechnung.");
        }

        return notes;
    }

    private MonthlyPayrollPreviewDto BuildPayrollPreview(
        EmployeeMonthlyRecord monthlyRecord,
        Domain.Employees.EmploymentContract? contract,
        PayrollSettings payrollSettings)
    {
        var lines = new List<MonthlyPayrollPreviewLineDto>();
        var notes = new List<string>();
        var timeEntries = monthlyRecord.TimeEntries.OrderBy(entry => entry.WorkDate).ToArray();
        var workSummary = PayrollWorkSummary.FromTimeEntries(monthlyRecord.EmployeeId, timeEntries);
        var expenses = monthlyRecord.ExpenseEntry is null
            ? Array.Empty<Domain.Expenses.ExpenseEntry>()
            : [monthlyRecord.ExpenseEntry];

        PayrollRunLineDerivationResult? derivationResult = null;
        if (contract is null)
        {
            notes.Add("Kein gueltiger Vertragsstand fuer die Lohn-Voransicht im gewaelten Monat gefunden.");
        }
        else
        {
            derivationResult = _payrollRunLineDerivationService.DeriveForEmployee(
                monthlyRecord.PeriodEnd,
                contract,
                payrollSettings,
                workSummary,
                expenses,
                timeEntries);

            foreach (var issue in derivationResult.Issues)
            {
                notes.Add(TranslateDerivationIssue(issue.Code));
            }
        }

        var derivationLines = derivationResult?.Lines ?? [];
        var baseLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BaseHours);
        var supplementLines = derivationLines
            .Where(line => line.LineType is PayrollLineType.NightSupplement or PayrollLineType.SundaySupplement or PayrollLineType.HolidaySupplement)
            .ToArray();
        var specialSupplementLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.SpecialSupplement);
        var vacationCompensationLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.VacationCompensation);
        var subtotalChf = (baseLine?.AmountChf ?? 0m)
            + supplementLines.Sum(line => line.AmountChf)
            + (specialSupplementLine?.AmountChf ?? 0m)
            + GetVehicleAmount(derivationLines, "VEHICLE_P1")
            + GetVehicleAmount(derivationLines, "VEHICLE_P2")
            + GetVehicleAmount(derivationLines, "VEHICLE_R1");
        var ahvGrossChf = derivationLines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);
        var totalChf = derivationLines
            .Where(line => line.LineType != PayrollLineType.Expense)
            .Sum(line => line.AmountChf);
        var expensesChf = monthlyRecord.ExpenseEntry?.ExpensesTotalChf ?? 0m;
        var totalPayoutChf = RoundToFiveRappen(totalChf + expensesChf);

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Basislohn",
            baseLine?.Quantity is null ? $"{workSummary.WorkHours:0.##} h" : $"{baseLine.Quantity.Value:0.##} h",
            contract is null ? "-" : $"{contract.HourlyRateChf:0.00} CHF",
            FormatAmount(baseLine?.AmountChf),
            null,
            false));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Stunden mit Zeitzuschlag",
            supplementLines.Length == 0 ? $"{workSummary.SpecialHours:0.##} h" : $"{supplementLines.Sum(line => line.Quantity ?? 0m):0.##} h",
            "gem. Settings",
            FormatAmountOrPending(
                supplementLines.Sum(line => line.AmountChf),
                contract is not null && workSummary.SpecialHours > 0m && supplementLines.Length == 0),
            supplementLines.Length == 0
                ? "Noch nicht vollstaendig ableitbar oder keine zuschlagspflichtigen Stunden im Monat."
                : BuildSupplementDetail(supplementLines),
            false));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Spezialzuschlag gemaess Vertrag",
            specialSupplementLine?.Quantity is null ? $"{workSummary.WorkHours:0.##} h" : $"{specialSupplementLine.Quantity.Value:0.##} h",
            contract is null ? "-" : $"{contract.SpecialSupplementRateChf:0.00} CHF",
            FormatAmount(contract is null ? null : specialSupplementLine?.AmountChf ?? 0m),
            contract is null
                ? "Ohne gueltigen Vertrag nicht ableitbar."
                : contract.SpecialSupplementRateChf > 0m
                    ? "Arbeitsstunden multipliziert mit dem vertraglichen Spezialzuschlag."
                    : "Im aktuellen Vertragsstand ist kein Spezialzuschlag hinterlegt.",
            false));

        lines.Add(BuildVehiclePreviewLine(
            "Fahrzeitentschaedigung Pauschalzone 1",
            timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf),
            payrollSettings.VehiclePauschalzone1RateChf,
            timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf) * payrollSettings.VehiclePauschalzone1RateChf));

        lines.Add(BuildVehiclePreviewLine(
            "Fahrzeitentschaedigung Pauschalzone 2",
            timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf),
            payrollSettings.VehiclePauschalzone2RateChf,
            timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf) * payrollSettings.VehiclePauschalzone2RateChf));

        lines.Add(BuildVehiclePreviewLine(
            "Fahrzeitentschaedigung Regiezone",
            timeEntries.Sum(entry => entry.VehicleRegiezone1Chf),
            payrollSettings.VehicleRegiezone1RateChf,
            timeEntries.Sum(entry => entry.VehicleRegiezone1Chf) * payrollSettings.VehicleRegiezone1RateChf));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Zwischentotal",
            "-",
            "-",
            FormatAmount(contract is null ? null : subtotalChf),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Summe der aktuell fachlich ableitbaren lohnrelevanten Positionen.",
            true));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Ferienentschaedigung",
            "-",
            contract is null ? "-" : $"{payrollSettings.VacationCompensationRate:0.####}",
            FormatAmount(contract is null ? null : vacationCompensationLine?.AmountChf ?? 0m),
            contract is null
                ? "Ohne gueltigen Vertrag nicht ableitbar."
                : "Rate aus Einstellungen multipliziert mit Basislohn, Zeitzuschlaegen, Spezialzuschlag und Fahrzeitentschaedigung.",
            false));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "AHV-pflichtiger Bruttolohn",
            "-",
            "-",
            FormatAmount(contract is null ? null : ahvGrossChf),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Basierend auf Basislohn, ableitbaren Zuschlaegen und Fahrzeitentschaedigung.",
            true));

        lines.Add(BuildNamedAmountLine("AHV/IV/EO", derivationLines, "AHV_IV_EO"));
        lines.Add(BuildNamedAmountLine("ALV", derivationLines, "ALV"));
        lines.Add(BuildNamedAmountLine("Krankentaggeld/UVG", derivationLines, "KTG_UVG"));
        lines.Add(BuildNamedAmountLine("Aus- und Weiterbildungskosten inkl. Ferienentschaedigung", derivationLines, "AUSBILDUNG_FERIEN"));

        var bvgLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BvgDeduction);
        if (bvgLine is not null)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                "BVG",
                "-",
                "-",
                FormatAmount(bvgLine.AmountChf),
                "Aus dem aktuellen Vertragsstand.",
                false));
        }

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Total",
            "-",
            "-",
            FormatAmount(contract is null ? null : totalChf),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Netto aus lohnrelevanten Positionen und Abzuegen ohne Spesen.",
            true));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Spesen gemaess Nachweis",
            "-",
            "-",
            $"{expensesChf:0.00} CHF",
            expensesChf > 0m ? "Direkt aus dem monatlichen Spesenblock." : null,
            false));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            "Total Auszahlung",
            "-",
            "gerundet auf 0.05",
            FormatAmount(contract is null ? null : totalPayoutChf),
            contract is null ? "Ohne gueltigen Vertrag nicht vollstaendig ableitbar." : "Summe aus Total und Spesen, analog Excel-Vorlage auf 5 Rappen gerundet.",
            true));

        if (workSummary.SpecialHours == 0m)
        {
            notes.Add("Keine zuschlagspflichtigen Stunden im aktuellen Monat erfasst.");
        }

        if (expensesChf == 0m)
        {
            notes.Add("Keine Spesen im aktuellen Monat erfasst.");
        }

        if (notes.Count == 0)
        {
            notes.Add("Lohn-Voransicht basiert auf Monatsdaten, aktuellem Vertrag und zentralen Settings.");
        }

        return new MonthlyPayrollPreviewDto(lines, notes);
    }

    private async Task<IReadOnlyCollection<MonthlyPreviewRowDto>> BuildPreviewRowsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var monthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ToListAsync(cancellationToken);

        var rows = new List<MonthlyPreviewRowDto>();

        foreach (var record in monthlyRecords)
        {
            var monthRows = record.TimeEntries
                .Select(entry => new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    entry.WorkDate,
                    "Zeit",
                    $"{entry.HoursWorked:0.##} h",
                    BuildTimePreviewDetails(entry)))
                .Concat(record.TimeEntries
                    .Where(entry => entry.VehicleCompensationTotalChf > 0m)
                    .Select(entry => new MonthlyPreviewRowDto(
                        record.Year,
                        record.Month,
                        entry.WorkDate,
                        "Fahrzeug",
                        $"{entry.VehicleCompensationTotalChf:0.00} CHF",
                        BuildVehiclePreviewDetails(entry))))
                .Concat(record.ExpenseEntry is null
                    ? []
                    : [
                        new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    null,
                    "Spesen",
                    $"{record.ExpenseEntry.ExpensesTotalChf:0.00} CHF",
                    $"Diverse Spesen {record.ExpenseEntry.ExpensesTotalChf:0.00}")]
                )
                .OrderBy(entry => entry.EntryDate)
                .ThenBy(entry => entry.EntryType)
                .ToArray();

            if (monthRows.Length == 0)
            {
                rows.Add(new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    null,
                    "Monat",
                    "-",
                    "Keine Eintraege"));
                continue;
            }

            rows.AddRange(monthRows);
        }

        return rows;
    }

    private static string BuildTimePreviewDetails(Domain.TimeTracking.TimeEntry entry)
    {
        var detailParts = new List<string>();

        if (entry.NightHours > 0)
        {
            detailParts.Add($"Nacht {entry.NightHours:0.##} h");
        }

        if (entry.SundayHours > 0)
        {
            detailParts.Add($"Sonntag {entry.SundayHours:0.##} h");
        }

        if (entry.HolidayHours > 0)
        {
            detailParts.Add($"Feiertag {entry.HolidayHours:0.##} h");
        }

        if (!string.IsNullOrWhiteSpace(entry.Note))
        {
            detailParts.Add(entry.Note);
        }

        return detailParts.Count == 0
            ? "Keine Zusatzangaben"
            : string.Join(" | ", detailParts);
    }

    private static string BuildVehiclePreviewDetails(Domain.TimeTracking.TimeEntry entry)
    {
        var detailParts = new List<string>();

        if (entry.VehiclePauschalzone1Chf > 0m)
        {
            detailParts.Add($"P1 {entry.VehiclePauschalzone1Chf:0.00}");
        }

        if (entry.VehiclePauschalzone2Chf > 0m)
        {
            detailParts.Add($"P2 {entry.VehiclePauschalzone2Chf:0.00}");
        }

        if (entry.VehicleRegiezone1Chf > 0m)
        {
            detailParts.Add($"R1 {entry.VehicleRegiezone1Chf:0.00}");
        }

        return detailParts.Count == 0
            ? "Keine Fahrzeugwerte"
            : string.Join(" | ", detailParts);
    }

    private static MonthlyPayrollPreviewLineDto BuildVehiclePreviewLine(
        string label,
        decimal quantity,
        decimal rateChf,
        decimal amountChf)
    {
        return new MonthlyPayrollPreviewLineDto(
            label,
            $"{quantity:0.##}",
            rateChf > 0m ? $"{rateChf:0.00} CHF" : "-",
            $"{amountChf:0.00} CHF",
            quantity > 0m && rateChf <= 0m
                ? "Menge vorhanden, aber kein zentraler CHF-Ansatz in den Einstellungen gepflegt."
                : null,
            false);
    }

    private static MonthlyPayrollPreviewLineDto BuildNamedAmountLine(
        string label,
        IEnumerable<PayrollRunLine> derivationLines,
        string code)
    {
        var matchingLine = derivationLines.SingleOrDefault(line => line.Code == code);
        return new MonthlyPayrollPreviewLineDto(
            label,
            "-",
            matchingLine is null ? "-" : "gem. Settings",
            FormatAmount(matchingLine?.AmountChf),
            null,
            false);
    }

    private static decimal GetVehicleAmount(IEnumerable<PayrollRunLine> derivationLines, string code)
    {
        return derivationLines
            .Where(line => line.Code == code)
            .Sum(line => line.AmountChf);
    }

    private static string FormatAmount(decimal? amountChf)
    {
        return amountChf.HasValue
            ? $"{amountChf.Value:0.00} CHF"
            : "Noch nicht ableitbar";
    }

    private static string FormatAmountOrPending(decimal amountChf, bool isPending)
    {
        return isPending
            ? "Noch nicht ableitbar"
            : $"{amountChf:0.00} CHF";
    }

    private static string BuildSupplementDetail(IEnumerable<PayrollRunLine> supplementLines)
    {
        return string.Join(" | ", supplementLines.Select(line => $"{line.Description} {line.AmountChf:0.00} CHF"));
    }

    private static string TranslateDerivationIssue(string code)
    {
        return code switch
        {
            "MISSING_NIGHT_RULE" => "Nachtzuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "MISSING_SUN_RULE" => "Sonntagszuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "MISSING_HOL_RULE" => "Feiertagszuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "AMBIGUOUS_SPECIAL_HOUR_OVERLAP" => "Spezialstunden uebersteigen die Arbeitsstunden. Zuschlagsberechnung bleibt deshalb unvollstaendig.",
            _ => code
        };
    }

    private static decimal RoundToFiveRappen(decimal amountChf)
    {
        return Math.Round(amountChf * 20m, 0, MidpointRounding.AwayFromZero) / 20m;
    }
}
