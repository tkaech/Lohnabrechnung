using Microsoft.EntityFrameworkCore;
using Payroll.Application.Formatting;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Payroll;
using Payroll.Infrastructure.Settings;
using System.Globalization;
using System.Text.Json;

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
        var currentPayrollSettings = await LoadCurrentPayrollSettingsAsync(cancellationToken);
        var contractForSnapshot = await LoadBestAvailableContractForMonthAsync(
            employeeId,
            new DateOnly(year, month, 1),
            new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            cancellationToken);
        var existingRecord = await _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
                .ThenInclude(advance => advance.Settlements)
            .Include(record => record.SalaryAdvanceSettlements)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(
                record => record.EmployeeId == employeeId && record.Year == year && record.Month == month,
                cancellationToken);

        if (existingRecord is not null)
        {
            existingRecord.InitializePayrollParameterSnapshot(currentPayrollSettings);
            existingRecord.InitializeEmploymentContractSnapshot(contractForSnapshot);
            return existingRecord;
        }

        var createdRecord = new EmployeeMonthlyRecord(employeeId, year, month);
        createdRecord.InitializePayrollParameterSnapshot(currentPayrollSettings);
        createdRecord.InitializeEmploymentContractSnapshot(contractForSnapshot);
        _dbContext.EmployeeMonthlyRecords.Add(createdRecord);
        return createdRecord;
    }

    public async Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        var monthlyRecord = await _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
                .ThenInclude(advance => advance.Settlements)
            .Include(record => record.SalaryAdvanceSettlements)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);

        if (monthlyRecord is null)
        {
            return null;
        }

        monthlyRecord.InitializePayrollParameterSnapshot(await LoadCurrentPayrollSettingsAsync(cancellationToken));
        monthlyRecord.InitializeEmploymentContractSnapshot(
            await LoadBestAvailableContractForMonthAsync(monthlyRecord.EmployeeId, monthlyRecord.PeriodStart, monthlyRecord.PeriodEnd, cancellationToken));
        return monthlyRecord;
    }

    public Task<SalaryAdvance?> GetSalaryAdvanceByIdAsync(Guid salaryAdvanceId, CancellationToken cancellationToken)
    {
        return _dbContext.SalaryAdvances
            .Include(advance => advance.Settlements)
            .SingleOrDefaultAsync(advance => advance.Id == salaryAdvanceId, cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        var monthlyRecord = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
                .ThenInclude(advance => advance.Settlements)
            .Include(record => record.SalaryAdvanceSettlements)
            .Include(record => record.ExpenseEntry)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);

        if (monthlyRecord is null)
        {
            return null;
        }

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleAsync(item => item.Id == monthlyRecord.EmployeeId, cancellationToken);
        DepartmentOption? department = null;
        if (employee.DepartmentOptionId is { } departmentId)
        {
            department = await _dbContext.DepartmentOptions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == departmentId, cancellationToken);
        }

        var hasFinalizedPayrollRun = await HasFinalizedPayrollRunAsync(
            monthlyRecord.EmployeeId,
            monthlyRecord.Year,
            monthlyRecord.Month,
            cancellationToken);
        var contract = ResolveContractForMonth(
            monthlyRecord,
            await LoadRelevantContractAsync(monthlyRecord.EmployeeId, monthlyRecord.PeriodStart, monthlyRecord.PeriodEnd, cancellationToken),
            hasFinalizedPayrollRun);
        var currentPayrollSettings = await LoadCurrentPayrollSettingsAsync(cancellationToken);
        var payrollSettings = ResolvePayrollSettingsForMonth(
            monthlyRecord,
            currentPayrollSettings,
            await TryLoadPayrollSettingsForMonthAsync(monthlyRecord.PeriodStart, cancellationToken),
            hasFinalizedPayrollRun);
        var salaryAdvances = await LoadEmployeeSalaryAdvancesAsync(monthlyRecord.EmployeeId, cancellationToken);

        var previewNotes = BuildPreviewNotes(monthlyRecord, contract is not null);
        var previewRows = await BuildPreviewRowsAsync(monthlyRecord.EmployeeId, salaryAdvances, cancellationToken);
        var payrollPreview = BuildPayrollPreview(
            monthlyRecord,
            employee.BirthDate,
            employee.TaxStatus,
            employee.IsSubjectToWithholdingTax == true,
            contract,
            department,
            payrollSettings,
            currentPayrollSettings,
            salaryAdvances);
        var employeeMonthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == monthlyRecord.EmployeeId)
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
            .Include(record => record.SalaryAdvanceSettlements)
            .Include(record => record.ExpenseEntry)
            .ToListAsync(cancellationToken);

        var header = new MonthlyRecordHeaderDto(
            monthlyRecord.Id,
            monthlyRecord.EmployeeId,
            employee.FullName,
            employee.FirstName,
            employee.LastName,
            employee.PersonnelNumber,
            monthlyRecord.Year,
            monthlyRecord.Month,
            monthlyRecord.Status,
            contract?.ValidFrom,
            contract?.ValidTo,
            contract?.HourlyRateChf,
            contract?.MonthlyBvgDeductionChf,
            employee.TaxStatus,
            employee.IsSubjectToWithholdingTax == true,
            monthlyRecord.WithholdingTaxRatePercent,
            monthlyRecord.WithholdingTaxCorrectionAmountChf,
            monthlyRecord.WithholdingTaxCorrectionText,
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

        var salaryAdvanceItems = salaryAdvances
            .Where(advance => (advance.Year == monthlyRecord.Year && advance.Month == monthlyRecord.Month)
                || advance.OpenAmountChf > 0m)
            .OrderBy(advance => advance.Year)
            .ThenBy(advance => advance.Month)
            .ThenBy(advance => advance.CreatedAtUtc)
            .Select(advance => new MonthlySalaryAdvanceDto(
                advance.Id,
                advance.Year,
                advance.Month,
                advance.AmountChf,
                advance.Note,
                advance.OpenAmountChf,
                advance.SettledAmountChf,
                advance.IsSettled,
                $"{advance.Month:00}/{advance.Year}"))
            .ToArray();

        var salaryAdvanceSettlementItems = salaryAdvances
            .SelectMany(advance => advance.Settlements.Select(settlement => new { advance, settlement }))
            .Where(item => item.settlement.Year == monthlyRecord.Year && item.settlement.Month == monthlyRecord.Month)
            .OrderByDescending(item => item.settlement.CreatedAtUtc)
            .Select(item => new MonthlySalaryAdvanceSettlementDto(
                item.settlement.Id,
                item.advance.Id,
                item.settlement.Year,
                item.settlement.Month,
                item.settlement.AmountChf,
                item.settlement.Note,
                $"{item.advance.Month:00}/{item.advance.Year}"))
            .ToArray();

        var currentSalaryAdvance = salaryAdvances
            .Where(advance => advance.Year == monthlyRecord.Year && advance.Month == monthlyRecord.Month)
            .OrderByDescending(advance => advance.CreatedAtUtc)
            .Select(advance => salaryAdvanceItems.First(item => item.SalaryAdvanceId == advance.Id))
            .FirstOrDefault();

        var currentSalaryAdvanceSettlement = salaryAdvanceSettlementItems.FirstOrDefault();

        var openSalaryAdvance = salaryAdvanceItems
            .Where(advance => advance.OpenAmountChf > 0m)
            .OrderBy(advance => advance.Year)
            .ThenBy(advance => advance.Month)
            .FirstOrDefault();

        return new MonthlyRecordDetailsDto(
            header,
            timeEntries,
            timeEntryHistory,
            expenseEntry,
            expenseEntryHistory,
            currentSalaryAdvance,
            currentSalaryAdvanceSettlement,
            openSalaryAdvance,
            new MonthlyRecordPreviewDto(previewRows, previewNotes),
            payrollPreview,
            salaryAdvanceItems,
            salaryAdvanceSettlementItems);
    }

    public async Task<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> ListTimeCaptureOverviewAsync(int year, int month, CancellationToken cancellationToken)
    {
        var employees = await _dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync(cancellationToken);

        var monthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.Year == year && record.Month == month)
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .ToListAsync(cancellationToken);
        var salaryAdvances = await _dbContext.SalaryAdvances
            .AsNoTracking()
            .Where(advance => advance.Year == year && advance.Month == month)
            .ToListAsync(cancellationToken);
        var salaryAdvanceSettlements = await _dbContext.SalaryAdvanceSettlements
            .AsNoTracking()
            .Where(settlement => settlement.Year == year && settlement.Month == month)
            .ToListAsync(cancellationToken);

        var recordsByEmployeeId = monthlyRecords.ToDictionary(record => record.EmployeeId);
        var advanceAmountsByEmployeeId = salaryAdvances
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.AmountChf));
        var settlementAmountsByEmployeeId = salaryAdvanceSettlements
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.AmountChf));

        return employees
            .Select(employee =>
            {
                recordsByEmployeeId.TryGetValue(employee.Id, out var monthlyRecord);
                var timeEntries = monthlyRecord?.TimeEntries ?? [];
                var netSalaryAdvanceAdjustmentChf =
                    (advanceAmountsByEmployeeId.TryGetValue(employee.Id, out var advanceAmount) ? advanceAmount : 0m)
                    - (settlementAmountsByEmployeeId.TryGetValue(employee.Id, out var settlementAmount) ? settlementAmount : 0m);

                return new MonthlyTimeCaptureOverviewRowDto(
                    employee.Id,
                    employee.PersonnelNumber,
                    employee.FirstName,
                    employee.LastName,
                    employee.IsActive,
                    timeEntries.Count > 0,
                    timeEntries.Sum(entry => entry.HoursWorked),
                    timeEntries.Sum(entry => entry.NightHours),
                    timeEntries.Sum(entry => entry.SundayHours),
                    timeEntries.Sum(entry => entry.HolidayHours),
                    timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf),
                    timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf),
                    timeEntries.Sum(entry => entry.VehicleRegiezone1Chf),
                    timeEntries.Count,
                    monthlyRecord?.ExpenseEntry?.ExpensesTotalChf ?? 0m,
                    netSalaryAdvanceAdjustmentChf);
            })
            .ToArray();
    }

    public Task<bool> HasTimeEntriesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        return _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.Year == year && record.Month == month)
            .AnyAsync(record => record.TimeEntries.Any(), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTimeEntriesForMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.TimeEntries
            .Where(entry => entry.WorkDate.Year == year && entry.WorkDate.Month == month)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return;
        }

        _dbContext.TimeEntries.RemoveRange(entries);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void ClearTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }

    public void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class
    {
        _dbContext.Entry(entity).State = EntityState.Added;
    }

    public void MarkAsDeleted<TEntity>(TEntity entity) where TEntity : class
    {
        _dbContext.Entry(entity).State = EntityState.Deleted;
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

    private async Task<Domain.Employees.EmploymentContract?> LoadBestAvailableContractForMonthAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var relevantContract = await LoadRelevantContractAsync(employeeId, periodStart, periodEnd, cancellationToken);
        if (relevantContract is not null)
        {
            return relevantContract;
        }

        return await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract => contract.EmployeeId == employeeId)
            .OrderByDescending(contract => contract.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<SalaryAdvance>> LoadEmployeeSalaryAdvancesAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SalaryAdvances
            .AsNoTracking()
            .Where(advance => advance.EmployeeId == employeeId)
            .Include(advance => advance.Settlements)
            .OrderBy(advance => advance.Year)
            .ThenBy(advance => advance.Month)
            .ToArrayAsync(cancellationToken);
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

    private static SalaryAdvanceActivityForMonth GetSalaryAdvanceActivityForMonth(
        EmployeeMonthlyRecord monthlyRecord,
        IReadOnlyCollection<SalaryAdvance> salaryAdvances)
    {
        var relevantAdvances = SalaryAdvancePayrollMonthFilter.FilterForMonth(
            salaryAdvances,
            monthlyRecord.EmployeeId,
            monthlyRecord.Year,
            monthlyRecord.Month);

        return new SalaryAdvanceActivityForMonth(
            relevantAdvances,
            relevantAdvances
                .Where(advance => advance.Year == monthlyRecord.Year && advance.Month == monthlyRecord.Month)
                .ToArray(),
            relevantAdvances
                .Where(advance => advance.Year == monthlyRecord.Year && advance.Month == monthlyRecord.Month)
                .Sum(advance => advance.AmountChf),
            relevantAdvances
                .SelectMany(advance => advance.Settlements)
                .Where(settlement => settlement.Year == monthlyRecord.Year && settlement.Month == monthlyRecord.Month)
                .Sum(settlement => settlement.AmountChf));
    }

    private MonthlyPayrollPreviewDto BuildPayrollPreview(
        EmployeeMonthlyRecord monthlyRecord,
        DateOnly? employeeBirthDate,
        string? employeeTaxStatus,
        bool isSubjectToWithholdingTax,
        Domain.Employees.EmploymentContract? contract,
        DepartmentOption? department,
        PayrollSettings payrollSettings,
        PayrollSettings currentPayrollSettings,
        IReadOnlyCollection<SalaryAdvance> salaryAdvances)
    {
        var timeEntries = monthlyRecord.TimeEntries.OrderBy(entry => entry.WorkDate).ToArray();
        var currentMonthSalaryAdvances = GetSalaryAdvanceActivityForMonth(monthlyRecord, salaryAdvances);
        if (timeEntries.Length == 0
            && currentMonthSalaryAdvances.PayoutAmountChf == 0m
            && currentMonthSalaryAdvances.SettlementAmountChf == 0m
            && contract?.WageType != Domain.Employees.EmployeeWageType.Monthly)
        {
            return new MonthlyPayrollPreviewDto([], [], ["Monat noch nicht erfasst"]);
        }

        var lines = new List<MonthlyPayrollPreviewLineDto>();
        var notes = new List<string>();
        var numberCulture = CreateNumberCulture(currentPayrollSettings.DecimalSeparator, currentPayrollSettings.ThousandsSeparator);
        var currencyCode = currentPayrollSettings.CurrencyCode;
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
                employeeBirthDate,
                contract,
                payrollSettings,
                workSummary,
                expenses,
                timeEntries,
                currentMonthSalaryAdvances.RelevantAdvances,
                new PayrollDerivationContext(
                    contract.WageType,
                    department?.Name,
                    department?.IsGavMandatory ?? false,
                    isSubjectToWithholdingTax,
                    employeeTaxStatus,
                    monthlyRecord.WithholdingTaxRatePercent,
                    monthlyRecord.WithholdingTaxCorrectionAmountChf,
                    monthlyRecord.WithholdingTaxCorrectionText));

            foreach (var issue in derivationResult.Issues)
            {
                notes.Add(TranslateDerivationIssue(issue.Code));
            }
        }

        var derivationLines = derivationResult?.Lines ?? [];
        var baseLine = derivationLines.SingleOrDefault(line => line.LineType is PayrollLineType.BaseHours or PayrollLineType.MonthlySalary);
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
                or PayrollLineType.MonthlySalary
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VacationCompensation
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);
        var salaryAdvancePayoutLines = derivationLines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvancePayout)
            .ToArray();
        var salaryAdvanceSettlementLines = derivationLines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvanceSettlement)
            .ToArray();
        var salaryAdvancePayoutAmountChf = salaryAdvancePayoutLines.Sum(line => line.AmountChf);
        var salaryAdvanceSettlementAmountChf = salaryAdvanceSettlementLines.Sum(line => line.AmountChf);
        var totalChf = derivationLines
            .Where(line => line.LineType is not PayrollLineType.Expense
                and not PayrollLineType.SalaryAdvancePayout
                and not PayrollLineType.SalaryAdvanceSettlement)
            .Sum(line => line.AmountChf);
        var expensesChf = monthlyRecord.ExpenseEntry?.ExpensesTotalChf ?? 0m;
        var totalPayoutChf = RoundToFiveRappen(totalChf + expensesChf + salaryAdvancePayoutAmountChf + salaryAdvanceSettlementAmountChf);

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.BaseSalaryCode,
            "Basislohn",
            contract?.WageType == Domain.Employees.EmployeeWageType.Monthly
                ? "Monat"
                : baseLine?.Quantity is null ? $"{FormatQuantity(workSummary.WorkHours, numberCulture)} h" : $"{FormatQuantity(baseLine.Quantity.Value, numberCulture)} h",
            contract is null
                ? "-"
                : contract.WageType == Domain.Employees.EmployeeWageType.Monthly
                    ? FormatMoney(contract.MonthlySalaryAmountChf, numberCulture, currencyCode)
                    : FormatMoney(contract.HourlyRateChf, numberCulture, currencyCode),
            FormatAmount(baseLine?.AmountChf, numberCulture, currencyCode),
            null,
            false,
            "BASE",
            "BAS",
            GetPreviewColorHint("BASE")));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.TimeSupplementCode,
            "Stunden mit Zeitzuschlag",
            supplementLines.Length == 0 ? $"{FormatQuantity(workSummary.SpecialHours, numberCulture)} h" : $"{FormatQuantity(supplementLines.Sum(line => line.Quantity ?? 0m), numberCulture)} h",
            BuildSupplementRateDisplay(workSummary, payrollSettings.WorkTimeSupplementSettings, numberCulture),
            FormatAmountOrPending(
                supplementLines.Sum(line => line.AmountChf),
                contract is not null && workSummary.SpecialHours > 0m && supplementLines.Length == 0,
                numberCulture,
                currencyCode),
            supplementLines.Length == 0
                ? "Noch nicht vollstaendig ableitbar oder keine zuschlagspflichtigen Stunden im Monat."
                : BuildSupplementDetail(supplementLines, numberCulture, currencyCode),
            false,
            "TIME_SUPPLEMENTS",
            "ZT",
            GetPreviewColorHint("TIME_SUPPLEMENTS")));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.SpecialSupplementCode,
            "Spezialzuschlag gemaess Vertrag",
            specialSupplementLine?.Quantity is null ? $"{FormatQuantity(workSummary.WorkHours, numberCulture)} h" : $"{FormatQuantity(specialSupplementLine.Quantity.Value, numberCulture)} h",
            contract is null ? "-" : FormatMoney(contract.SpecialSupplementRateChf, numberCulture, currencyCode),
            FormatAmount(contract is null ? null : specialSupplementLine?.AmountChf ?? 0m, numberCulture, currencyCode),
            contract is null
                ? "Ohne gueltigen Vertrag nicht ableitbar."
                : contract.SpecialSupplementRateChf > 0m
                    ? "Arbeitsstunden multipliziert mit dem vertraglichen Spezialzuschlag."
                    : "Im aktuellen Vertragsstand ist kein Spezialzuschlag hinterlegt.",
            false,
            "SPECIAL_CONTRACT",
            "SZ",
            GetPreviewColorHint("SPECIAL_CONTRACT")));

        lines.Add(BuildVehiclePreviewLine(
            PayrollPreviewHelpCatalog.VehiclePauschalzone1Code,
            "Fahrzeitentschaedigung Pauschalzone 1",
            timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf),
            payrollSettings.VehiclePauschalzone1RateChf,
            timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf) * payrollSettings.VehiclePauschalzone1RateChf,
            "VEHICLE_P1",
            "P1",
            numberCulture,
            currencyCode));

        lines.Add(BuildVehiclePreviewLine(
            PayrollPreviewHelpCatalog.VehiclePauschalzone2Code,
            "Fahrzeitentschaedigung Pauschalzone 2",
            timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf),
            payrollSettings.VehiclePauschalzone2RateChf,
            timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf) * payrollSettings.VehiclePauschalzone2RateChf,
            "VEHICLE_P2",
            "P2",
            numberCulture,
            currencyCode));

        lines.Add(BuildVehiclePreviewLine(
            PayrollPreviewHelpCatalog.VehicleRegiezone1Code,
            "Fahrzeitentschaedigung Regiezone",
            timeEntries.Sum(entry => entry.VehicleRegiezone1Chf),
            payrollSettings.VehicleRegiezone1RateChf,
            timeEntries.Sum(entry => entry.VehicleRegiezone1Chf) * payrollSettings.VehicleRegiezone1RateChf,
            "VEHICLE_R1",
            "R1",
            numberCulture,
            currencyCode));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.SubtotalCode,
            "Zwischentotal",
            "-",
            "-",
            FormatAmount(contract is null ? null : subtotalChf, numberCulture, currencyCode),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Summe der aktuell fachlich ableitbaren lohnrelevanten Positionen.",
            true,
            "SUBTOTAL",
            "SUB",
            GetPreviewColorHint("SUBTOTAL")));

        var effectiveVacationCompensationRate = payrollSettings.GetVacationCompensationRate(employeeBirthDate, monthlyRecord.PeriodEnd);
        var usesAge50PlusVacationCompensationRate = payrollSettings.UsesVacationCompensationRateAge50Plus(employeeBirthDate, monthlyRecord.PeriodEnd);
        var age50PlusEffectiveDate = payrollSettings.GetVacationCompensationRateAge50PlusEffectiveDate(employeeBirthDate);

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.VacationCompensationCode,
            "Ferienentschaedigung",
            "-",
            contract is null ? "-" : FormatPercentageRate(effectiveVacationCompensationRate, numberCulture),
            FormatAmount(contract is null ? null : vacationCompensationLine?.AmountChf ?? 0m, numberCulture, currencyCode),
            BuildVacationCompensationPreviewDetail(contract is not null, employeeBirthDate, usesAge50PlusVacationCompensationRate, age50PlusEffectiveDate),
            false,
            "VACATION_COMP",
            "FER",
            GetPreviewColorHint("VACATION_COMP")));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.AhvGrossCode,
            "AHV-pflichtiger Bruttolohn",
            "-",
            "-",
            FormatAmount(contract is null ? null : ahvGrossChf, numberCulture, currencyCode),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Basierend auf Basislohn, ableitbaren Zuschlaegen, Fahrzeitentschaedigung und Ferienentschaedigung.",
            true,
            "AHV_GROSS",
            "BRU",
            GetPreviewColorHint("AHV_GROSS")));

        lines.Add(BuildNamedAmountLine(PayrollPreviewHelpCatalog.AhvIvEoCode, "AHV/IV/EO", derivationLines, "AHV_IV_EO", payrollSettings.AhvIvEoRate, "AHV_IV_EO", "AHV", numberCulture, currencyCode));
        lines.Add(BuildNamedAmountLine(PayrollPreviewHelpCatalog.AlvCode, "ALV", derivationLines, "ALV", payrollSettings.AlvRate, "ALV", "ALV", numberCulture, currencyCode));
        lines.Add(BuildNamedAmountLine(PayrollPreviewHelpCatalog.KtgUvgCode, "Krankentaggeld/UVG", derivationLines, "KTG_UVG", payrollSettings.SicknessAccidentInsuranceRate, "KTG_UVG", "UVG", numberCulture, currencyCode));
        var trainingLine = BuildTrainingAndHolidayLine(
            employeeBirthDate,
            monthlyRecord.PeriodEnd,
            derivationLines,
            payrollSettings,
            numberCulture,
            currencyCode);
        lines.Add(department?.IsGavMandatory == true
            ? trainingLine with { RateDisplay = FormatPercentageRate(payrollSettings.TrainingAndHolidayRate, numberCulture), AmountDisplay = FormatMoney(0m, numberCulture, currencyCode), Detail = "Unterdrueckt, weil die Abteilung GAV-pflichtig ist." }
            : trainingLine);

        var withholdingTaxLine = derivationLines.SingleOrDefault(line => line.Code == "WITHHOLDING_TAX");
        if (withholdingTaxLine is not null)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                "WITHHOLDING_TAX",
                withholdingTaxLine.Description,
                "-",
                FormatRawPercentageRate(monthlyRecord.WithholdingTaxRatePercent, numberCulture),
                FormatAmount(withholdingTaxLine.AmountChf, numberCulture, currencyCode),
                $"Basis: {FormatMoney(ahvGrossChf, numberCulture, currencyCode)}.",
                false,
                "WITHHOLDING_TAX",
                "QST",
                GetPreviewColorHint("WITHHOLDING_TAX")));
        }

        var withholdingTaxCorrectionLine = derivationLines.SingleOrDefault(line => line.Code == "WITHHOLDING_TAX_CORRECTION");
        if (withholdingTaxCorrectionLine is not null)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                "WITHHOLDING_TAX_CORRECTION",
                "Quellensteuer Korrektur / Rueckzahlung",
                "-",
                "-",
                FormatAmount(withholdingTaxCorrectionLine.AmountChf, numberCulture, currencyCode),
                monthlyRecord.WithholdingTaxCorrectionText,
                false,
                "WITHHOLDING_TAX_CORRECTION",
                "QK",
                GetPreviewColorHint("WITHHOLDING_TAX")));
        }

        var bvgLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BvgDeduction);
        if (bvgLine is not null)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                PayrollPreviewHelpCatalog.BvgCode,
                "BVG",
                "-",
                "-",
                FormatAmount(bvgLine.AmountChf, numberCulture, currencyCode),
                "Aus dem aktuellen Vertragsstand.",
                false,
                "BVG",
                "BVG",
                GetPreviewColorHint("BVG")));
        }

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.TotalCode,
            "Total",
            "-",
            "-",
            FormatAmount(contract is null ? null : totalChf, numberCulture, currencyCode),
            contract is null ? "Ohne gueltigen Vertrag nicht ableitbar." : "Netto aus lohnrelevanten Positionen und Abzuegen ohne Spesen.",
            true,
            "TOTAL",
            "TOT",
            GetPreviewColorHint("TOTAL")));

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.ExpensesCode,
            "Spesen gemaess Nachweis",
            "-",
            "-",
            FormatMoney(expensesChf, numberCulture, currencyCode),
            expensesChf > 0m ? "Direkt aus dem monatlichen Spesenblock." : null,
            false,
            "EXPENSES",
            "SPS",
            GetPreviewColorHint("EXPENSES")));

        if (salaryAdvancePayoutLines.Length > 0)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                PayrollPreviewHelpCatalog.SalaryAdvancePayoutCode,
                "Lohnvorschuss Auszahlung",
                "-",
                "-",
                FormatAmount(salaryAdvancePayoutAmountChf, numberCulture, currencyCode),
                BuildSalaryAdvancePreviewDetail(currentMonthSalaryAdvances.PayoutAdvances, false),
                false,
                "SALARY_ADVANCE_PAYOUT",
                "VOR",
                GetPreviewColorHint("SALARY_ADVANCE_PAYOUT")));
        }

        if (salaryAdvanceSettlementLines.Length > 0)
        {
            lines.Add(new MonthlyPayrollPreviewLineDto(
                PayrollPreviewHelpCatalog.SalaryAdvanceSettlementCode,
                "Lohnvorschuss Verrechnung",
                "-",
                "-",
                FormatAmount(salaryAdvanceSettlementAmountChf, numberCulture, currencyCode),
                BuildSalaryAdvancePreviewDetail(currentMonthSalaryAdvances.RelevantAdvances, true),
                false,
                "SALARY_ADVANCE_SETTLEMENT",
                "VER",
                GetPreviewColorHint("SALARY_ADVANCE_SETTLEMENT")));
        }

        lines.Add(new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.TotalPayoutCode,
            "Total Auszahlung",
            "-",
            "gerundet auf 0.05",
            FormatAmount(contract is null ? null : totalPayoutChf, numberCulture, currencyCode),
            contract is null ? "Ohne gueltigen Vertrag nicht vollstaendig ableitbar." : "Summe aus Total und Spesen, analog Excel-Vorlage auf 5 Rappen gerundet.",
            true,
            "TOTAL_PAYOUT",
            "AUS",
            GetPreviewColorHint("TOTAL_PAYOUT")));

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
            notes.Add("Lohn-Voransicht basiert auf Monatsdaten, aktuellem Vertrag und dem gespeicherten Monatsparameter-Snapshot.");
        }

        var helpTextSettings = PayrollPreviewHelpCatalog.MergeWithDefaults(DeserializePayrollPreviewHelpVisibilities(currentPayrollSettings.PayrollPreviewHelpVisibilityJson))
            .ToDictionary(item => item.Code, item => item, StringComparer.Ordinal);
        var filteredLines = lines
            .Select(line => line with
            {
                Detail = ResolveConfiguredHelpText(line, helpTextSettings)
            })
            .ToArray();

        var derivationGroups = BuildPayrollPreviewDerivationGroups(
            monthlyRecord.PeriodEnd,
            employeeBirthDate,
            monthlyRecord,
            contract,
            department,
            payrollSettings,
            workSummary,
            timeEntries,
            derivationLines,
            derivationResult?.Issues ?? [],
            subtotalChf,
            ahvGrossChf,
            totalChf,
            expensesChf,
            salaryAdvancePayoutAmountChf,
            salaryAdvanceSettlementAmountChf,
            totalPayoutChf,
            numberCulture,
            currencyCode);

        return new MonthlyPayrollPreviewDto(filteredLines, derivationGroups, notes);
    }

    private async Task<IReadOnlyCollection<MonthlyPreviewRowDto>> BuildPreviewRowsAsync(
        Guid employeeId,
        IReadOnlyCollection<SalaryAdvance> salaryAdvances,
        CancellationToken cancellationToken)
    {
        var monthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
            .Include(record => record.SalaryAdvanceSettlements)
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
                        PayrollAmountFormatter.FormatChf(entry.VehicleCompensationTotalChf),
                        BuildVehiclePreviewDetails(entry))))
                .Concat(record.ExpenseEntry is null
                    ? []
                    : [
                        new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    null,
                    "Spesen",
                    PayrollAmountFormatter.FormatChf(record.ExpenseEntry.ExpensesTotalChf),
                    $"Diverse Spesen {PayrollAmountFormatter.FormatChf(record.ExpenseEntry.ExpensesTotalChf)}")]
                )
                .Concat(salaryAdvances
                    .Where(advance => advance.Year == record.Year && advance.Month == record.Month)
                    .Select(advance => new MonthlyPreviewRowDto(
                        record.Year,
                        record.Month,
                        null,
                        "Vorschuss",
                        PayrollAmountFormatter.FormatChf(advance.AmountChf),
                        string.IsNullOrWhiteSpace(advance.Note)
                            ? $"Lohnvorschuss {PayrollAmountFormatter.FormatChf(advance.AmountChf)}"
                            : advance.Note!)))
                .Concat(salaryAdvances
                    .SelectMany(advance => advance.Settlements.Select(settlement => new { advance, settlement }))
                    .Where(item => item.settlement.Year == record.Year && item.settlement.Month == record.Month)
                    .Select(item => new MonthlyPreviewRowDto(
                        record.Year,
                        record.Month,
                        null,
                        "Verrechnung",
                        PayrollAmountFormatter.FormatChf(-item.settlement.AmountChf),
                        BuildSalaryAdvanceSettlementHistoryDetail(item.advance, item.settlement))))
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

    private static string BuildSalaryAdvanceSettlementHistoryDetail(
        SalaryAdvance advance,
        SalaryAdvanceSettlement settlement)
    {
        var sourceMonth = $"{advance.Month:00}/{advance.Year}";
        return string.IsNullOrWhiteSpace(settlement.Note)
            ? $"Verrechnung fuer Vorschuss aus {sourceMonth}"
            : $"Verrechnung fuer Vorschuss aus {sourceMonth} | {settlement.Note}";
    }

    private static string? BuildSalaryAdvancePreviewDetail(
        IReadOnlyCollection<SalaryAdvance> relevantAdvances,
        bool settlementsOnly)
    {
        var details = settlementsOnly
            ? relevantAdvances
                .SelectMany(advance => advance.Settlements.Select(settlement => new { advance, settlement }))
                .Select(item => BuildSalaryAdvanceSettlementHistoryDetail(item.advance, item.settlement))
                .ToArray()
            : relevantAdvances
                .Where(advance => advance.AmountChf > 0m)
                .Select(advance => string.IsNullOrWhiteSpace(advance.Note)
                    ? $"Vorschuss {PayrollAmountFormatter.FormatChf(advance.AmountChf)}"
                    : $"{PayrollAmountFormatter.FormatChf(advance.AmountChf)} | {advance.Note}")
                .ToArray();

        return details.Length == 0 ? null : string.Join(" | ", details);
    }

    private static MonthlyPayrollPreviewLineDto BuildVehiclePreviewLine(
        string code,
        string label,
        decimal quantity,
        decimal rateChf,
        decimal amountChf,
        string linkKey,
        string displayTag,
        CultureInfo numberCulture,
        string currencyCode)
    {
        return new MonthlyPayrollPreviewLineDto(
            code,
            label,
            FormatQuantity(quantity, numberCulture),
            rateChf > 0m ? FormatMoney(rateChf, numberCulture, currencyCode) : "-",
            FormatMoney(amountChf, numberCulture, currencyCode),
            quantity > 0m && rateChf <= 0m
                ? "Menge vorhanden, aber kein zentraler CHF-Ansatz in den Einstellungen gepflegt."
                : null,
            false,
            linkKey,
            displayTag,
            GetPreviewColorHint(linkKey));
    }

    private static MonthlyPayrollPreviewLineDto BuildNamedAmountLine(
        string previewCode,
        string label,
        IEnumerable<PayrollRunLine> derivationLines,
        string code,
        decimal rate,
        string linkKey,
        string displayTag,
        CultureInfo numberCulture,
        string currencyCode)
    {
        var matchingLine = derivationLines.SingleOrDefault(line => line.Code == code);
        return new MonthlyPayrollPreviewLineDto(
            previewCode,
            label,
            "-",
            matchingLine is null ? "-" : FormatPercentageRate(rate, numberCulture),
            FormatAmount(matchingLine?.AmountChf, numberCulture, currencyCode),
            null,
            false,
            linkKey,
            displayTag,
            GetPreviewColorHint(linkKey));
    }

    private static MonthlyPayrollPreviewLineDto BuildTrainingAndHolidayLine(
        DateOnly? employeeBirthDate,
        DateOnly payrollReferenceDate,
        IEnumerable<PayrollRunLine> derivationLines,
        PayrollSettings payrollSettings,
        CultureInfo numberCulture,
        string currencyCode)
    {
        var matchingLine = derivationLines.SingleOrDefault(line => line.Code == "AUSBILDUNG_FERIEN");
        var effectiveVacationCompensationRate = payrollSettings.GetVacationCompensationRate(employeeBirthDate, payrollReferenceDate);
        var baseHoursLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BaseHours);
        var supplementLines = derivationLines
            .Where(line => line.LineType is PayrollLineType.NightSupplement or PayrollLineType.SundaySupplement or PayrollLineType.HolidaySupplement)
            .ToArray();
        var detail = matchingLine is null
            ? null
            : BuildTrainingAndHolidayDetail(baseHoursLine, supplementLines, effectiveVacationCompensationRate, payrollSettings.TrainingAndHolidayRate, numberCulture, currencyCode);

        return new MonthlyPayrollPreviewLineDto(
            PayrollPreviewHelpCatalog.TrainingAndHolidayCode,
            "Aus- und Weiterbildungskosten inkl. Ferienentschaedigung",
            "-",
            matchingLine is null ? "-" : FormatPercentageRate(payrollSettings.TrainingAndHolidayRate, numberCulture),
            FormatAmount(matchingLine?.AmountChf, numberCulture, currencyCode),
            detail,
            false,
            "AUSBILDUNG_FERIEN",
            "AW",
            GetPreviewColorHint("AUSBILDUNG_FERIEN"));
    }

    private static IReadOnlyCollection<MonthlyPayrollPreviewDerivationGroupDto> BuildPayrollPreviewDerivationGroups(
        DateOnly payrollReferenceDate,
        DateOnly? employeeBirthDate,
        EmployeeMonthlyRecord monthlyRecord,
        Domain.Employees.EmploymentContract? contract,
        DepartmentOption? department,
        PayrollSettings payrollSettings,
        PayrollWorkSummary workSummary,
        IReadOnlyCollection<Domain.TimeTracking.TimeEntry> timeEntries,
        IReadOnlyCollection<PayrollRunLine> derivationLines,
        IReadOnlyCollection<PayrollDerivationIssue> derivationIssues,
        decimal subtotalChf,
        decimal ahvGrossChf,
        decimal totalChf,
        decimal expensesChf,
        decimal salaryAdvancePayoutAmountChf,
        decimal salaryAdvanceSettlementAmountChf,
        decimal totalPayoutChf,
        CultureInfo numberCulture,
        string currencyCode)
    {
        if (contract is null)
        {
            return
            [
                new MonthlyPayrollPreviewDerivationGroupDto(
                    "Hinweise / Konflikte",
                    [
                        CreateDerivationItem(
                            "ISSUE_CONTRACT",
                            "Hinweis",
                            "Kein gueltiger Vertragsstand",
                            "Lohn-Voransicht kann nur teilweise aufgebaut werden.",
                            null,
                            "Ohne gueltigen Vertrag bleiben ableitbare Lohnpositionen, Totale und Abzuege offen.",
                            "ISSUE")
                    ])
            ];
        }

        var supplementLines = derivationLines
            .Where(line => line.LineType is PayrollLineType.NightSupplement or PayrollLineType.SundaySupplement or PayrollLineType.HolidaySupplement)
            .ToArray();
        var specialSupplementLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.SpecialSupplement);
        var vacationCompensationLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.VacationCompensation);
        var bvgLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BvgDeduction);
        var withholdingTaxLine = derivationLines.SingleOrDefault(line => line.Code == "WITHHOLDING_TAX");
        var withholdingTaxCorrectionLine = derivationLines.SingleOrDefault(line => line.Code == "WITHHOLDING_TAX_CORRECTION");
        var baseLine = derivationLines.SingleOrDefault(line => line.LineType is PayrollLineType.BaseHours or PayrollLineType.MonthlySalary);
        var isMonthlySalary = contract.WageType == Domain.Employees.EmployeeWageType.Monthly;
        var effectiveVacationCompensationRate = payrollSettings.GetVacationCompensationRate(employeeBirthDate, payrollReferenceDate);
        var usesAge50PlusVacationCompensationRate = payrollSettings.UsesVacationCompensationRateAge50Plus(employeeBirthDate, payrollReferenceDate);
        var age50PlusEffectiveDate = payrollSettings.GetVacationCompensationRateAge50PlusEffectiveDate(employeeBirthDate);
        var inputs = new List<MonthlyPayrollPreviewDerivationItemDto>
        {
            CreateDerivationItem("INPUT_BASE_HOURS", "Eingabe", "Arbeitsstunden", $"{FormatQuantity(workSummary.WorkHours, numberCulture)} h", null, "Aus den Zeiteintraegen des gewaelten Monats.", "BASE"),
            CreateDerivationItem("INPUT_NIGHT_HOURS", "Eingabe", "Nachtstunden", $"{FormatQuantity(workSummary.NightHours, numberCulture)} h", null, "Zuschlagspflichtige Nachtstunden aus der Monatserfassung.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("INPUT_SUNDAY_HOURS", "Eingabe", "Sonntagsstunden", $"{FormatQuantity(workSummary.SundayHours, numberCulture)} h", null, "Zuschlagspflichtige Sonntagsstunden aus der Monatserfassung.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("INPUT_HOLIDAY_HOURS", "Eingabe", "Feiertagsstunden", $"{FormatQuantity(workSummary.HolidayHours, numberCulture)} h", null, "Zuschlagspflichtige Feiertagsstunden aus der Monatserfassung.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("INPUT_VEHICLE_P1", "Eingabe", "Pauschalzone 1 Menge", FormatQuantity(timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf), numberCulture), null, "Monatssumme aus den erfassten Zeitzeilen.", "VEHICLE_P1"),
            CreateDerivationItem("INPUT_VEHICLE_P2", "Eingabe", "Pauschalzone 2 Menge", FormatQuantity(timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf), numberCulture), null, "Monatssumme aus den erfassten Zeitzeilen.", "VEHICLE_P2"),
            CreateDerivationItem("INPUT_VEHICLE_R1", "Eingabe", "Regiezone 1 Menge", FormatQuantity(timeEntries.Sum(entry => entry.VehicleRegiezone1Chf), numberCulture), null, "Monatssumme aus den erfassten Zeitzeilen.", "VEHICLE_R1"),
            CreateDerivationItem("INPUT_EXPENSES", "Eingabe", "Spesenblock", FormatMoney(expensesChf, numberCulture, currencyCode), null, "Direkt aus dem monatlichen Spesenblock.", "EXPENSES"),
            CreateDerivationItem("INPUT_ADVANCE_PAYOUT", "Eingabe", "Lohnvorschuss Auszahlung", FormatMoney(salaryAdvancePayoutAmountChf, numberCulture, currencyCode), null, "Manuell erfasster Vorschuss im aktuellen Abrechnungsmonat.", "SALARY_ADVANCE_PAYOUT"),
            CreateDerivationItem("INPUT_ADVANCE_SETTLEMENT", "Eingabe", "Lohnvorschuss Verrechnung", FormatMoney(-salaryAdvanceSettlementAmountChf, numberCulture, currencyCode), null, "Manuell erfasste Verrechnung eines frueheren Vorschusses im aktuellen Abrechnungsmonat.", "SALARY_ADVANCE_SETTLEMENT")
        };

        var rules = new List<MonthlyPayrollPreviewDerivationItemDto>
        {
            CreateDerivationItem("RULE_WAGE_TYPE", "Regel", "Lohnart im Abrechnungsmonat", contract.WageType == Domain.Employees.EmployeeWageType.Monthly ? "Monatslohn" : "Stundenlohn", null, "Aus dem gueltigen Vertragsstand des Monats.", "BASE"),
            CreateDerivationItem("RULE_DEPARTMENT_GAV", "Regel", "Abteilung / GAV", department?.Name ?? "-", null, $"GAV-pflichtig: {(department?.IsGavMandatory == true ? "Ja" : "Nein")}. Aus-/Weiterbildungskosten werden {(department?.IsGavMandatory == true ? "unterdrueckt" : "angewendet")}.", "AUSBILDUNG_FERIEN"),
            CreateDerivationItem("RULE_HOURLY_RATE", "Regel", "Stundenlohn", FormatMoney(contract.HourlyRateChf, numberCulture, currencyCode), null, "Aus dem gueltigen Vertragsstand des Monats.", "BASE"),
            CreateDerivationItem("RULE_MONTHLY_SALARY", "Regel", "Monatslohn-Betrag", FormatMoney(contract.MonthlySalaryAmountChf, numberCulture, currencyCode), null, isMonthlySalary ? "Aus dem gueltigen Vertragsstand des Monats." : "Nicht angewendet, weil die Lohnart Stundenlohn ist.", "BASE"),
            CreateDerivationItem("RULE_SPECIAL_RATE", "Regel", "Spezialzuschlag gemaess Vertrag", FormatMoney(contract.SpecialSupplementRateChf, numberCulture, currencyCode), null, contract.SpecialSupplementRateChf > 0m ? "Vertraglicher CHF-Betrag pro Arbeitsstunde." : "Im Vertragsstand ist kein Spezialzuschlag hinterlegt.", "SPECIAL_CONTRACT"),
            CreateDerivationItem("RULE_NIGHT_RATE", "Regel", "Nachtzuschlagssatz", FormatOptionalPercentageRate(payrollSettings.WorkTimeSupplementSettings.NightSupplementRate, numberCulture), null, "Gespeicherter Monatsparameter fuer Nachtzuschlag.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("RULE_SUNDAY_RATE", "Regel", "Sonntagszuschlagssatz", FormatOptionalPercentageRate(payrollSettings.WorkTimeSupplementSettings.SundaySupplementRate, numberCulture), null, "Gespeicherter Monatsparameter fuer Sonntagszuschlag.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("RULE_HOLIDAY_RATE", "Regel", "Feiertagszuschlagssatz", FormatOptionalPercentageRate(payrollSettings.WorkTimeSupplementSettings.HolidaySupplementRate, numberCulture), null, "Gespeicherter Monatsparameter fuer Feiertagszuschlag.", "TIME_SUPPLEMENTS"),
            CreateDerivationItem("RULE_VACATION_RATE", "Regel", "Ferienentschaedigungssatz", FormatPercentageRate(effectiveVacationCompensationRate, numberCulture), null, BuildVacationCompensationRuleDetail(employeeBirthDate, usesAge50PlusVacationCompensationRate, age50PlusEffectiveDate), "VACATION_COMP"),
            CreateDerivationItem("RULE_VEHICLE_P1_RATE", "Regel", "Pauschalzone 1 Ansatz", FormatMoney(payrollSettings.VehiclePauschalzone1RateChf, numberCulture, currencyCode), null, "Globaler CHF-Ansatz aus dem gespeicherten Monatsparameter-Snapshot.", "VEHICLE_P1"),
            CreateDerivationItem("RULE_VEHICLE_P2_RATE", "Regel", "Pauschalzone 2 Ansatz", FormatMoney(payrollSettings.VehiclePauschalzone2RateChf, numberCulture, currencyCode), null, "Globaler CHF-Ansatz aus dem gespeicherten Monatsparameter-Snapshot.", "VEHICLE_P2"),
            CreateDerivationItem("RULE_VEHICLE_R1_RATE", "Regel", "Regiezone 1 Ansatz", FormatMoney(payrollSettings.VehicleRegiezone1RateChf, numberCulture, currencyCode), null, "Globaler CHF-Ansatz aus dem gespeicherten Monatsparameter-Snapshot.", "VEHICLE_R1"),
            CreateDerivationItem("RULE_AHV_RATE", "Regel", "AHV/IV/EO", FormatPercentageRate(payrollSettings.AhvIvEoRate, numberCulture), null, "Prozentualer Abzug aus dem gespeicherten Monatsparameter-Snapshot.", "AHV_IV_EO"),
            CreateDerivationItem("RULE_ALV_RATE", "Regel", "ALV", FormatPercentageRate(payrollSettings.AlvRate, numberCulture), null, "Prozentualer Abzug aus dem gespeicherten Monatsparameter-Snapshot.", "ALV"),
            CreateDerivationItem("RULE_KTG_RATE", "Regel", "Krankentaggeld/UVG", FormatPercentageRate(payrollSettings.SicknessAccidentInsuranceRate, numberCulture), null, "Prozentualer Abzug aus dem gespeicherten Monatsparameter-Snapshot.", "KTG_UVG"),
            CreateDerivationItem("RULE_TRAINING_RATE", "Regel", "Aus- und Weiterbildung inkl. Ferien", FormatPercentageRate(payrollSettings.TrainingAndHolidayRate, numberCulture), null, "Konfigurierter Satz aus dem gespeicherten Monatsparameter-Snapshot.", "AUSBILDUNG_FERIEN"),
            CreateDerivationItem("RULE_WITHHOLDING_TAX", "Regel", "Quellensteuer", FormatRawPercentageRate(monthlyRecord.WithholdingTaxRatePercent, numberCulture), null, "Manuell pro Mitarbeiter und Monat erfasster Prozentsatz.", "WITHHOLDING_TAX")
        };

        if (contract.MonthlyBvgDeductionChf > 0m)
        {
            rules.Add(CreateDerivationItem("RULE_BVG", "Regel", "BVG", FormatMoney(contract.MonthlyBvgDeductionChf, numberCulture, currencyCode), null, "Fixbetrag aus dem aktuellen Vertragsstand.", "BVG"));
        }

        var steps = new List<MonthlyPayrollPreviewDerivationItemDto>
        {
            CreateDerivationItem("STEP_BASE", "Schritt", "Grundlohn", baseLine is null
                    ? "Noch nicht ableitbar"
                    : FormatMoney(baseLine.AmountChf, numberCulture, currencyCode),
                isMonthlySalary
                    ? FormatMoney(contract.MonthlySalaryAmountChf, numberCulture, currencyCode)
                    : $"{FormatQuantity(workSummary.WorkHours, numberCulture)} h x {FormatMoney(contract.HourlyRateChf, numberCulture, currencyCode)}",
                isMonthlySalary
                    ? "Monatslohn-Betrag aus dem gueltigen Vertragsstand; Arbeitsstunden werden nicht multipliziert."
                    : "Arbeitsstunden multipliziert mit dem Stundenlohn.",
                "BASE"),
            CreateDerivationItem("STEP_SUPPLEMENTS", "Schritt", "Zeitzuschlaege", supplementLines.Length == 0
                ? (workSummary.SpecialHours > 0m ? "Noch nicht ableitbar" : FormatMoney(0m, numberCulture, currencyCode))
                : FormatMoney(supplementLines.Sum(line => line.AmountChf), numberCulture, currencyCode),
                BuildSupplementStepFormula(supplementLines, contract.HourlyRateChf, payrollSettings.WorkTimeSupplementSettings, workSummary, numberCulture, currencyCode),
                supplementLines.Length == 0
                    ? "Keine vollstaendig ableitbaren Nacht-/Sonntags-/Feiertagszuschlaege im Monat."
                    : "Summe der fachlich ableitbaren Zeitzuschlaege.",
                "TIME_SUPPLEMENTS"),
            CreateDerivationItem("STEP_SPECIAL", "Schritt", "Spezialzuschlag gemaess Vertrag", specialSupplementLine is null
                ? FormatMoney(0m, numberCulture, currencyCode)
                : FormatMoney(specialSupplementLine.AmountChf, numberCulture, currencyCode),
                $"{FormatQuantity(workSummary.WorkHours, numberCulture)} h x {FormatMoney(contract.SpecialSupplementRateChf, numberCulture, currencyCode)}",
                contract.SpecialSupplementRateChf > 0m
                    ? "Arbeitsstunden multipliziert mit dem vertraglichen Spezialzuschlag."
                    : "Im Vertragsstand ist kein Spezialzuschlag hinterlegt.",
                "SPECIAL_CONTRACT"),
            CreateDerivationItem("STEP_VEHICLE_P1", "Schritt", "Fahrzeitentschaedigung Pauschalzone 1", FormatMoney(GetVehicleAmount(derivationLines, "VEHICLE_P1"), numberCulture, currencyCode), $"{FormatQuantity(timeEntries.Sum(entry => entry.VehiclePauschalzone1Chf), numberCulture)} x {FormatMoney(payrollSettings.VehiclePauschalzone1RateChf, numberCulture, currencyCode)}", null, "VEHICLE_P1"),
            CreateDerivationItem("STEP_VEHICLE_P2", "Schritt", "Fahrzeitentschaedigung Pauschalzone 2", FormatMoney(GetVehicleAmount(derivationLines, "VEHICLE_P2"), numberCulture, currencyCode), $"{FormatQuantity(timeEntries.Sum(entry => entry.VehiclePauschalzone2Chf), numberCulture)} x {FormatMoney(payrollSettings.VehiclePauschalzone2RateChf, numberCulture, currencyCode)}", null, "VEHICLE_P2"),
            CreateDerivationItem("STEP_VEHICLE_R1", "Schritt", "Fahrzeitentschaedigung Regiezone", FormatMoney(GetVehicleAmount(derivationLines, "VEHICLE_R1"), numberCulture, currencyCode), $"{FormatQuantity(timeEntries.Sum(entry => entry.VehicleRegiezone1Chf), numberCulture)} x {FormatMoney(payrollSettings.VehicleRegiezone1RateChf, numberCulture, currencyCode)}", null, "VEHICLE_R1"),
            CreateDerivationItem("STEP_SUBTOTAL", "Schritt", "Zwischentotal", FormatMoney(subtotalChf, numberCulture, currencyCode), "Basislohn + Zeitzuschlaege + Spezialzuschlag + Fahrzeugentschaedigungen", "Erstes lohnrelevantes Zwischentotal vor weiteren Abzuegen.", "SUBTOTAL"),
            CreateDerivationItem("STEP_VACATION", "Schritt", "Ferienentschaedigung", vacationCompensationLine is null ? FormatMoney(0m, numberCulture, currencyCode) : FormatMoney(vacationCompensationLine.AmountChf, numberCulture, currencyCode), $"{FormatPercentageRate(effectiveVacationCompensationRate, numberCulture)} x {FormatMoney(subtotalChf, numberCulture, currencyCode)}", BuildVacationCompensationStepDetail(employeeBirthDate, usesAge50PlusVacationCompensationRate, age50PlusEffectiveDate), "VACATION_COMP"),
            CreateDerivationItem("STEP_AHV_GROSS", "Schritt", "AHV-pflichtiger Bruttolohn", FormatMoney(ahvGrossChf, numberCulture, currencyCode), "Basislohn + Zuschlaege + Fahrzeugentschaedigungen + Ferienentschaedigung", "Gleiche Bruttolohnbasis wie PayrollRunLine und Jahresuebersicht.", "AHV_GROSS"),
            BuildDeductionDerivationItem("STEP_AHV", "AHV/IV/EO", derivationLines, "AHV_IV_EO", payrollSettings.AhvIvEoRate, "AHV_IV_EO", numberCulture, currencyCode),
            BuildDeductionDerivationItem("STEP_ALV", "ALV", derivationLines, "ALV", payrollSettings.AlvRate, "ALV", numberCulture, currencyCode),
            BuildDeductionDerivationItem("STEP_KTG", "Krankentaggeld/UVG", derivationLines, "KTG_UVG", payrollSettings.SicknessAccidentInsuranceRate, "KTG_UVG", numberCulture, currencyCode),
            department?.IsGavMandatory == true
                ? CreateDerivationItem("STEP_TRAINING", "Schritt", "Aus- und Weiterbildung inkl. Ferien", FormatMoney(0m, numberCulture, currencyCode), "-", "Unterdrueckt, weil die Abteilung GAV-pflichtig ist.", "AUSBILDUNG_FERIEN")
                : BuildTrainingAndHolidayDerivationItem(
                    "STEP_TRAINING",
                    derivationLines,
                    effectiveVacationCompensationRate,
                    payrollSettings.TrainingAndHolidayRate,
                    numberCulture,
                    currencyCode),
            CreateDerivationItem("STEP_WITHHOLDING_TAX", "Schritt", withholdingTaxLine?.Description ?? "Quellensteuer", withholdingTaxLine is null ? FormatMoney(0m, numberCulture, currencyCode) : FormatMoney(withholdingTaxLine.AmountChf, numberCulture, currencyCode), $"{FormatRawPercentageRate(monthlyRecord.WithholdingTaxRatePercent, numberCulture)} x {FormatMoney(ahvGrossChf, numberCulture, currencyCode)}", "Berechnet vom AHV-pflichtigen Bruttolohn.", "WITHHOLDING_TAX"),
            CreateDerivationItem("STEP_TOTAL", "Schritt", "Total", FormatMoney(totalChf, numberCulture, currencyCode), "Lohnrelevante Positionen minus Abzuege, ohne Spesen", "Netto vor separatem Spesenblock.", "TOTAL"),
            CreateDerivationItem("STEP_EXPENSES", "Schritt", "Spesen gemaess Nachweis", FormatMoney(expensesChf, numberCulture, currencyCode), null, "Direkt aus dem monatlichen Spesenblock uebernommen.", "EXPENSES"),
            CreateDerivationItem("STEP_ADVANCE_PAYOUT", "Schritt", "Lohnvorschuss Auszahlung", FormatMoney(salaryAdvancePayoutAmountChf, numberCulture, currencyCode), null, "Erhoeht nur die Auszahlung, nicht Bruttolohn oder Abzugsbasis.", "SALARY_ADVANCE_PAYOUT"),
            CreateDerivationItem("STEP_ADVANCE_SETTLEMENT", "Schritt", "Lohnvorschuss Verrechnung", FormatMoney(-salaryAdvanceSettlementAmountChf, numberCulture, currencyCode), null, "Reduziert nur die Auszahlung, nicht Bruttolohn oder Abzugsbasis.", "SALARY_ADVANCE_SETTLEMENT"),
            CreateDerivationItem("STEP_PAYOUT", "Schritt", "Total Auszahlung", FormatMoney(totalPayoutChf, numberCulture, currencyCode), $"{FormatMoney(totalChf, numberCulture, currencyCode)} + {FormatMoney(expensesChf, numberCulture, currencyCode)} + {FormatMoney(salaryAdvancePayoutAmountChf, numberCulture, currencyCode)} + {FormatMoney(-salaryAdvanceSettlementAmountChf, numberCulture, currencyCode)}", "Summe aus Total, Spesen und Vorschussbewegungen, auf 0.05 gerundet.", "TOTAL_PAYOUT")
        };

        if (bvgLine is not null)
        {
            steps.Insert(12, CreateDerivationItem("STEP_BVG", "Schritt", "BVG", FormatMoney(bvgLine.AmountChf, numberCulture, currencyCode), FormatMoney(contract.MonthlyBvgDeductionChf, numberCulture, currencyCode), "Fixbetrag aus dem Vertragsstand.", "BVG"));
        }

        if (monthlyRecord.WithholdingTaxCorrectionAmountChf != 0m || !string.IsNullOrWhiteSpace(monthlyRecord.WithholdingTaxCorrectionText))
        {
            steps.Insert(
                Math.Min(steps.Count, 14),
                CreateDerivationItem(
                    "STEP_WITHHOLDING_TAX_CORRECTION",
                    "Schritt",
                    "Quellensteuer Korrektur / Rueckzahlung",
                    withholdingTaxCorrectionLine is null ? FormatMoney(0m, numberCulture, currencyCode) : FormatMoney(withholdingTaxCorrectionLine.AmountChf, numberCulture, currencyCode),
                    null,
                    string.IsNullOrWhiteSpace(monthlyRecord.WithholdingTaxCorrectionText) ? "Manuelle Korrektur ohne Beschreibung." : monthlyRecord.WithholdingTaxCorrectionText,
                    "WITHHOLDING_TAX_CORRECTION"));
        }

        var issueItems = derivationIssues
            .Select((issue, index) => CreateDerivationItem(
                $"ISSUE_{index + 1}",
                "Hinweis",
                TranslateDerivationIssue(issue.Code),
                issue.Code,
                null,
                issue.Message,
                "ISSUE"))
            .ToArray();

        var groups = new List<MonthlyPayrollPreviewDerivationGroupDto>
        {
            new("Eingaben", inputs),
            new("Regeln / Saetze", rules),
            new("Rechenschritte", steps)
        };

        if (issueItems.Length > 0)
        {
            groups.Add(new MonthlyPayrollPreviewDerivationGroupDto("Hinweise / Konflikte", issueItems));
        }

        return groups;
    }

    private static IReadOnlyCollection<global::Payroll.Domain.Settings.PayrollPreviewHelpVisibility> DeserializePayrollPreviewHelpVisibilities(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<global::Payroll.Domain.Settings.PayrollPreviewHelpVisibility[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ResolveConfiguredHelpText(
        MonthlyPayrollPreviewLineDto line,
        IReadOnlyDictionary<string, PayrollPreviewHelpOptionDto> helpTextSettings)
    {
        if (!helpTextSettings.TryGetValue(line.Code, out var option) || !option.IsEnabled)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(option.HelpText)
            ? line.Detail
            : option.HelpText.Trim();
    }

    private static string BuildSupplementRateDisplay(
        PayrollWorkSummary workSummary,
        Domain.Employees.WorkTimeSupplementSettings supplementSettings,
        CultureInfo numberCulture)
    {
        var parts = new List<string>();

        if (workSummary.NightHours > 0m && supplementSettings.NightSupplementRate.HasValue)
        {
            parts.Add($"Nacht {FormatPercentageRate(supplementSettings.NightSupplementRate.Value, numberCulture)}");
        }

        if (workSummary.SundayHours > 0m && supplementSettings.SundaySupplementRate.HasValue)
        {
            parts.Add($"Sonntag {FormatPercentageRate(supplementSettings.SundaySupplementRate.Value, numberCulture)}");
        }

        if (workSummary.HolidayHours > 0m && supplementSettings.HolidaySupplementRate.HasValue)
        {
            parts.Add($"Feiertag {FormatPercentageRate(supplementSettings.HolidaySupplementRate.Value, numberCulture)}");
        }

        return parts.Count == 0 ? "-" : string.Join(" | ", parts);
    }

    private async Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollSettings
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PayrollSettings();
    }

    private async Task<PayrollSettings?> TryLoadPayrollSettingsForMonthAsync(DateOnly referenceDate, CancellationToken cancellationToken)
    {
        var fallbackSettings = await LoadCurrentPayrollSettingsAsync(cancellationToken);
        var generalVersions = await _dbContext.PayrollGeneralSettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var hourlyVersions = await _dbContext.PayrollHourlySettingsVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (generalVersions.Count == 0 && hourlyVersions.Count == 0)
        {
            return null;
        }

        return PayrollSettingsVersionResolver.ResolveForDate(
            fallbackSettings,
            generalVersions,
            hourlyVersions,
            referenceDate);
    }

    private async Task<bool> HasFinalizedPayrollRunAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        var periodKey = $"{year:D4}-{month:D2}";
        return await _dbContext.PayrollRuns
            .AsNoTracking()
            .AnyAsync(
                run => run.PeriodKey == periodKey
                    && run.Status == PayrollRunStatus.Finalized
                    && run.Lines.Any(line => line.EmployeeId == employeeId),
                cancellationToken);
    }

    private static PayrollSettings ResolvePayrollSettingsForMonth(
        EmployeeMonthlyRecord monthlyRecord,
        PayrollSettings currentPayrollSettings,
        PayrollSettings? effectivePayrollSettings,
        bool hasFinalizedPayrollRun)
    {
        return (hasFinalizedPayrollRun || effectivePayrollSettings is null) && monthlyRecord.PayrollParameterSnapshot.IsInitialized
            ? monthlyRecord.PayrollParameterSnapshot.ToPayrollSettings()
            : effectivePayrollSettings ?? currentPayrollSettings;
    }

    private static Domain.Employees.EmploymentContract? ResolveContractForMonth(
        EmployeeMonthlyRecord monthlyRecord,
        Domain.Employees.EmploymentContract? relevantContract,
        bool hasFinalizedPayrollRun)
    {
        if (!hasFinalizedPayrollRun && relevantContract is not null)
        {
            return relevantContract;
        }

        return monthlyRecord.EmploymentContractSnapshot.IsInitialized
            ? monthlyRecord.EmploymentContractSnapshot.ToEmploymentContract(monthlyRecord.EmployeeId)
            : relevantContract;
    }

    private static decimal GetVehicleAmount(IEnumerable<PayrollRunLine> derivationLines, string code)
    {
        return derivationLines
            .Where(line => line.Code == code)
            .Sum(line => line.AmountChf);
    }

    private static string FormatAmount(decimal? amountChf, CultureInfo numberCulture, string currencyCode)
    {
        return amountChf.HasValue
            ? FormatMoney(amountChf.Value, numberCulture, currencyCode)
            : "Noch nicht ableitbar";
    }

    private static string FormatPercentageRate(decimal rate, CultureInfo numberCulture)
    {
        return $"{(rate * 100m).ToString("#,##0.###", numberCulture)} %";
    }

    private static string FormatRawPercentageRate(decimal ratePercent, CultureInfo numberCulture)
    {
        return $"{ratePercent.ToString("#,##0.###", numberCulture)} %";
    }

    private static string FormatOptionalPercentageRate(decimal? rate, CultureInfo numberCulture)
    {
        return rate.HasValue
            ? FormatPercentageRate(rate.Value, numberCulture)
            : "offen";
    }

    private static string FormatAmountOrPending(decimal amountChf, bool isPending, CultureInfo numberCulture, string currencyCode)
    {
        return isPending
            ? "Noch nicht ableitbar"
            : FormatMoney(amountChf, numberCulture, currencyCode);
    }

    private static string BuildSupplementDetail(IEnumerable<PayrollRunLine> supplementLines, CultureInfo numberCulture, string currencyCode)
    {
        return string.Join(" | ", supplementLines.Select(line => $"{line.Description} {FormatMoney(line.AmountChf, numberCulture, currencyCode)}"));
    }

    private static string BuildVacationCompensationPreviewDetail(
        bool hasContract,
        DateOnly? employeeBirthDate,
        bool usesAge50PlusVacationCompensationRate,
        DateOnly? age50PlusEffectiveDate)
    {
        if (!hasContract)
        {
            return "Ohne gueltigen Vertrag nicht ableitbar.";
        }

        if (!employeeBirthDate.HasValue)
        {
            return "Ohne Geburtsdatum wird der Standardsatz aus den gespeicherten Monatsparametern verwendet.";
        }

        if (usesAge50PlusVacationCompensationRate)
        {
            return $"Ferienentschaedigung ab 50 Jahren angewendet (gueltig ab {FormatDate(age50PlusEffectiveDate)}).";
        }

        return $"Standardsatz fuer Ferienentschaedigung angewendet. Erhoehter Satz gilt ab {FormatDate(age50PlusEffectiveDate)}.";
    }

    private static string BuildVacationCompensationRuleDetail(
        DateOnly? employeeBirthDate,
        bool usesAge50PlusVacationCompensationRate,
        DateOnly? age50PlusEffectiveDate)
    {
        if (!employeeBirthDate.HasValue)
        {
            return "Standardsatz aus dem gespeicherten Monatsparameter-Snapshot, da kein Geburtsdatum vorliegt.";
        }

        if (usesAge50PlusVacationCompensationRate)
        {
            return $"Ferienentschaedigung ab 50 Jahren angewendet. Der erhoehte Satz gilt seit {FormatDate(age50PlusEffectiveDate)}.";
        }

        return $"Standardsatz angewendet. Der erhoehte Satz gilt ab {FormatDate(age50PlusEffectiveDate)}.";
    }

    private static string BuildVacationCompensationStepDetail(
        DateOnly? employeeBirthDate,
        bool usesAge50PlusVacationCompensationRate,
        DateOnly? age50PlusEffectiveDate)
    {
        if (!employeeBirthDate.HasValue)
        {
            return "Ferienentschaedigung mit Standardsatz berechnet, da kein Geburtsdatum vorliegt.";
        }

        if (usesAge50PlusVacationCompensationRate)
        {
            return $"Ferienentschaedigung ab 50 Jahren angewendet. Berechnung mit dem erhoehten Satz seit {FormatDate(age50PlusEffectiveDate)}.";
        }

        return $"Ferienentschaedigung mit Standardsatz berechnet. Der erhoehte Satz gilt ab {FormatDate(age50PlusEffectiveDate)}.";
    }

    private static string BuildSupplementStepFormula(
        IReadOnlyCollection<PayrollRunLine> supplementLines,
        decimal hourlyRateChf,
        Domain.Employees.WorkTimeSupplementSettings supplementSettings,
        PayrollWorkSummary workSummary,
        CultureInfo numberCulture,
        string currencyCode)
    {
        if (supplementLines.Count == 0)
        {
            var missingParts = new List<string>();

            if (workSummary.NightHours > 0m)
            {
                missingParts.Add($"Nacht {FormatQuantity(workSummary.NightHours, numberCulture)} h x {FormatMoney(hourlyRateChf, numberCulture, currencyCode)} x {FormatOptionalPercentageRate(supplementSettings.NightSupplementRate, numberCulture)}");
            }

            if (workSummary.SundayHours > 0m)
            {
                missingParts.Add($"Sonntag {FormatQuantity(workSummary.SundayHours, numberCulture)} h x {FormatMoney(hourlyRateChf, numberCulture, currencyCode)} x {FormatOptionalPercentageRate(supplementSettings.SundaySupplementRate, numberCulture)}");
            }

            if (workSummary.HolidayHours > 0m)
            {
                missingParts.Add($"Feiertag {FormatQuantity(workSummary.HolidayHours, numberCulture)} h x {FormatMoney(hourlyRateChf, numberCulture, currencyCode)} x {FormatOptionalPercentageRate(supplementSettings.HolidaySupplementRate, numberCulture)}");
            }

            return missingParts.Count == 0
                ? "-"
                : string.Join(" | ", missingParts);
        }

        return string.Join(
            " | ",
            supplementLines.Select(line => $"{line.Description} {FormatQuantity(line.Quantity ?? 0m, numberCulture)} h = {FormatMoney(line.AmountChf, numberCulture, currencyCode)}"));
    }

    private static MonthlyPayrollPreviewDerivationItemDto BuildTrainingAndHolidayDerivationItem(
        string stepId,
        IEnumerable<PayrollRunLine> derivationLines,
        decimal effectiveVacationCompensationRate,
        decimal trainingAndHolidayRate,
        CultureInfo numberCulture,
        string currencyCode)
    {
        var matchingLine = derivationLines.SingleOrDefault(line => line.Code == "AUSBILDUNG_FERIEN");
        var baseHoursLine = derivationLines.SingleOrDefault(line => line.LineType == PayrollLineType.BaseHours);
        var supplementLines = derivationLines
            .Where(line => line.LineType is PayrollLineType.NightSupplement or PayrollLineType.SundaySupplement or PayrollLineType.HolidaySupplement)
            .ToArray();
        var baseHours = baseHoursLine?.Quantity ?? 0m;
        var supplementHours = supplementLines.Sum(line => line.Quantity ?? 0m);
        var effectiveSupplementHours = GetEffectiveTrainingSupplementHours(baseHoursLine, supplementLines);
        var hoursIncludingVacationCompensation = GetTrainingAndHolidayHoursIncludingVacation(baseHoursLine, supplementLines, effectiveVacationCompensationRate);
        var supplementFormula = BuildTrainingSupplementFormula(baseHoursLine, supplementLines, numberCulture);

        return CreateDerivationItem(
            stepId,
            "Schritt",
            "Aus- und Weiterbildung inkl. Ferien",
            FormatAmount(matchingLine?.AmountChf, numberCulture, currencyCode),
            matchingLine is null
                ? "-"
                : $"({FormatQuantity(baseHours, numberCulture)} h + {supplementFormula}) = {FormatQuantity(baseHours + effectiveSupplementHours, numberCulture)} h | x (1 + {FormatPercentageRate(effectiveVacationCompensationRate, numberCulture)}) = {FormatQuantity(hoursIncludingVacationCompensation, numberCulture)} h | x {FormatPercentageRate(trainingAndHolidayRate, numberCulture)}",
            matchingLine is null
                ? "Keine ableitbare Basis aus Basislohn und Zeitzuschlaegen vorhanden."
                : BuildTrainingAndHolidayDetail(baseHoursLine, supplementLines, effectiveVacationCompensationRate, trainingAndHolidayRate, numberCulture, currencyCode),
            "AUSBILDUNG_FERIEN");
    }

    private static MonthlyPayrollPreviewDerivationItemDto BuildDeductionDerivationItem(
        string stepId,
        string label,
        IEnumerable<PayrollRunLine> derivationLines,
        string code,
        decimal rate,
        string linkKey,
        CultureInfo numberCulture,
        string currencyCode)
    {
        var matchingLine = derivationLines.SingleOrDefault(line => line.Code == code);
        var basisDisplay = matchingLine is not null && rate > 0m
            ? FormatMoney(Math.Abs(matchingLine.AmountChf) / rate, numberCulture, currencyCode)
            : "-";

        return CreateDerivationItem(
            stepId,
            "Schritt",
            label,
            FormatAmount(matchingLine?.AmountChf, numberCulture, currencyCode),
            matchingLine is null ? "-" : $"{FormatPercentageRate(rate, numberCulture)} x {basisDisplay}",
            "Abzug aus der im Berechnungslauf abgeleiteten beitragspflichtigen Basis.",
            linkKey);
    }

    private static decimal GetTrainingAndHolidayHoursIncludingVacation(
        PayrollRunLine? baseHoursLine,
        IEnumerable<PayrollRunLine> supplementLines,
        decimal effectiveVacationCompensationRate)
    {
        var baseHours = baseHoursLine?.Quantity ?? 0m;
        var effectiveSupplementHours = GetEffectiveTrainingSupplementHours(baseHoursLine, supplementLines);
        return (baseHours + effectiveSupplementHours) * (1m + effectiveVacationCompensationRate);
    }

    private static decimal GetEffectiveTrainingSupplementHours(
        PayrollRunLine? baseHoursLine,
        IEnumerable<PayrollRunLine> supplementLines)
    {
        var baseHourlyRateChf = baseHoursLine?.RateChf;
        if (!baseHourlyRateChf.HasValue || baseHourlyRateChf.Value <= 0m)
        {
            return 0m;
        }

        return supplementLines.Sum(line => line.AmountChf / baseHourlyRateChf.Value);
    }

    private static string BuildTrainingSupplementFormula(
        PayrollRunLine? baseHoursLine,
        IReadOnlyCollection<PayrollRunLine> supplementLines,
        CultureInfo numberCulture)
    {
        if (supplementLines.Count == 0)
        {
            return "0 h";
        }

        return string.Join(
            " + ",
            supplementLines.Select(line =>
            {
                var supplementRate = GetSupplementPercentage(baseHoursLine, line);
                return $"{FormatQuantity(line.Quantity ?? 0m, numberCulture)} h x {FormatPercentageRate(supplementRate, numberCulture)}";
            }));
    }

    private static decimal GetSupplementPercentage(
        PayrollRunLine? baseHoursLine,
        PayrollRunLine supplementLine)
    {
        var baseHourlyRateChf = baseHoursLine?.RateChf;
        if (!baseHourlyRateChf.HasValue || baseHourlyRateChf.Value <= 0m || !supplementLine.RateChf.HasValue)
        {
            return 0m;
        }

        return supplementLine.RateChf.Value / baseHourlyRateChf.Value;
    }

    private static string BuildTrainingAndHolidayDetail(
        PayrollRunLine? baseHoursLine,
        IReadOnlyCollection<PayrollRunLine> supplementLines,
        decimal effectiveVacationCompensationRate,
        decimal trainingAndHolidayRate,
        CultureInfo numberCulture,
        string currencyCode)
    {
        var baseHours = baseHoursLine?.Quantity ?? 0m;
        var supplementHours = supplementLines.Sum(line => line.Quantity ?? 0m);
        var effectiveSupplementHours = GetEffectiveTrainingSupplementHours(baseHoursLine, supplementLines);
        var hoursIncludingVacationCompensation = GetTrainingAndHolidayHoursIncludingVacation(baseHoursLine, supplementLines, effectiveVacationCompensationRate);
        var trainingAndHolidayAmountChf = hoursIncludingVacationCompensation * (trainingAndHolidayRate * 100m);
        var supplementRateDetail = supplementLines.Count == 0
            ? "-"
            : string.Join(
                " | ",
                supplementLines.Select(line =>
                    $"{line.Description} {FormatQuantity(line.Quantity ?? 0m, numberCulture)} h x {FormatPercentageRate(GetSupplementPercentage(baseHoursLine, line), numberCulture)} = {FormatQuantity((line.Quantity ?? 0m) * GetSupplementPercentage(baseHoursLine, line), numberCulture)} h"));

        return $"Basisstunden {FormatQuantity(baseHours, numberCulture)} h, ZT-Stunden {FormatQuantity(supplementHours, numberCulture)} h, Zuschlags-Prozent {supplementRateDetail}, effektive Zuschlagsstunden {FormatQuantity(effectiveSupplementHours, numberCulture)} h, Gesamtstunden {FormatQuantity(baseHours + effectiveSupplementHours, numberCulture)} h, Ferienaufschlag {FormatPercentageRate(effectiveVacationCompensationRate, numberCulture)}, Endbetrag {FormatMoney(trainingAndHolidayAmountChf, numberCulture, currencyCode)}.";
    }

    private static MonthlyPayrollPreviewDerivationItemDto CreateDerivationItem(
        string stepId,
        string kindLabel,
        string label,
        string valueDisplay,
        string? formulaDisplay,
        string? detail,
        string linkKey)
    {
        return new MonthlyPayrollPreviewDerivationItemDto(
            stepId,
            kindLabel,
            label,
            valueDisplay,
            formulaDisplay,
            detail,
            linkKey,
            GetPreviewDisplayTag(linkKey),
            GetPreviewColorHint(linkKey));
    }

    private static string FormatMoney(decimal value, CultureInfo numberCulture, string currencyCode)
    {
        return PayrollAmountFormatter.FormatMoney(value, currencyCode);
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";
    }

    private static string FormatQuantity(decimal value, CultureInfo numberCulture)
    {
        return value.ToString("#,##0.##", numberCulture);
    }

    private static string GetPreviewDisplayTag(string linkKey)
    {
        return linkKey switch
        {
            "BASE" => "BAS",
            "TIME_SUPPLEMENTS" => "ZT",
            "SPECIAL_CONTRACT" => "SZ",
            "VEHICLE_P1" => "P1",
            "VEHICLE_P2" => "P2",
            "VEHICLE_R1" => "R1",
            "SUBTOTAL" => "SUB",
            "VACATION_COMP" => "FER",
            "AHV_GROSS" => "BRU",
            "AHV_IV_EO" => "AHV",
            "ALV" => "ALV",
            "KTG_UVG" => "UVG",
            "AUSBILDUNG_FERIEN" => "AW",
            "BVG" => "BVG",
            "TOTAL" => "TOT",
            "EXPENSES" => "SPS",
            "SALARY_ADVANCE_PAYOUT" => "VOR",
            "SALARY_ADVANCE_SETTLEMENT" => "VER",
            "TOTAL_PAYOUT" => "AUS",
            _ => "!"
        };
    }

    private static string GetPreviewColorHint(string linkKey)
    {
        return linkKey switch
        {
            "BASE" => "#FFDCEBFF",
            "TIME_SUPPLEMENTS" => "#FFE0F2FE",
            "SPECIAL_CONTRACT" => "#FFFDE7D7",
            "VEHICLE_P1" or "VEHICLE_P2" or "VEHICLE_R1" => "#FFE7F5E8",
            "SUBTOTAL" or "AHV_GROSS" => "#FFEFF4F8",
            "VACATION_COMP" => "#FFFFF4CC",
            "AHV_IV_EO" or "ALV" or "KTG_UVG" or "AUSBILDUNG_FERIEN" or "BVG" => "#FFFCE7E7",
            "TOTAL" => "#FFEAF0F8",
            "EXPENSES" => "#FFF3E8FF",
            "SALARY_ADVANCE_PAYOUT" => "#FFE5EEF9",
            "SALARY_ADVANCE_SETTLEMENT" => "#FFF8ECEC",
            "TOTAL_PAYOUT" => "#FFE4F7EC",
            _ => "#FFF8E7E7"
        };
    }

    private static CultureInfo CreateNumberCulture(string? decimalSeparator, string? thousandsSeparator)
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = decimalSeparator == "." ? "." : ",";
        culture.NumberFormat.NumberGroupSeparator = thousandsSeparator == " " ? " " : PayrollSettings.DefaultThousandsSeparator;
        return culture;
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
    {
        return string.IsNullOrWhiteSpace(currencyCode)
            ? PayrollSettings.DefaultCurrencyCode
            : currencyCode.Trim().ToUpperInvariant();
    }

    private static string TranslateDerivationIssue(string code)
    {
        return code switch
        {
            "MISSING_NIGHT_RULE" => "Nachtzuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "MISSING_SUN_RULE" => "Sonntagszuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "MISSING_HOL_RULE" => "Feiertagszuschlag kann mangels zentralem Satz noch nicht berechnet werden.",
            "AMBIGUOUS_SPECIAL_HOUR_OVERLAP" => "Spezialstunden uebersteigen die Arbeitsstunden. Zuschlagsberechnung bleibt deshalb unvollstaendig.",
            "MONTHLY_SALARY_AMOUNT_MISSING" => "Monatslohn wurde erkannt; im Vertragsstand ist kein Monatslohn-Betrag hinterlegt.",
            _ => code
        };
    }

    private static decimal RoundToFiveRappen(decimal amountChf)
    {
        return Math.Round(amountChf * 20m, 0, MidpointRounding.AwayFromZero) / 20m;
    }

    private sealed record SalaryAdvanceActivityForMonth(
        IReadOnlyCollection<SalaryAdvance> RelevantAdvances,
        IReadOnlyCollection<SalaryAdvance> PayoutAdvances,
        decimal PayoutAmountChf,
        decimal SettlementAmountChf);
}
