using Microsoft.EntityFrameworkCore;
using Payroll.Application.MonthlyRecords;
using Payroll.Domain.Expenses;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.MonthlyRecords;

public sealed class EmployeeMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
{
    private readonly PayrollDbContext _dbContext;

    public EmployeeMonthlyRecordRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
    {
        var existingRecord = await _dbContext.EmployeeMonthlyRecords
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntries)
            .Include(record => record.VehicleCompensations)
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
            .Include(record => record.ExpenseEntries)
            .Include(record => record.VehicleCompensations)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        var monthlyRecord = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntries)
            .Include(record => record.VehicleCompensations)
            .SingleOrDefaultAsync(record => record.Id == monthlyRecordId, cancellationToken);

        if (monthlyRecord is null)
        {
            return null;
        }

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleAsync(item => item.Id == monthlyRecord.EmployeeId, cancellationToken);

        var contract = await LoadRelevantContractAsync(monthlyRecord.EmployeeId, monthlyRecord.PeriodStart, monthlyRecord.PeriodEnd, cancellationToken);

        var previewNotes = BuildPreviewNotes(monthlyRecord, contract is not null);
        var previewRows = await BuildPreviewRowsAsync(monthlyRecord.EmployeeId, cancellationToken);

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
            monthlyRecord.ExpenseEntries.Sum(entry => entry.AmountChf),
            monthlyRecord.VehicleCompensations.Sum(entry => entry.AmountChf));

        var timeEntries = monthlyRecord.TimeEntries
            .OrderBy(entry => entry.WorkDate)
            .Select(entry => new MonthlyTimeEntryDto(
                entry.Id,
                entry.WorkDate,
                entry.HoursWorked,
                entry.NightHours,
                entry.SundayHours,
                entry.HolidayHours,
                entry.Note))
            .ToArray();

        var expenseEntries = monthlyRecord.ExpenseEntries
            .OrderBy(entry => entry.ExpenseDate)
            .Select(entry => new MonthlyExpenseEntryDto(
                entry.Id,
                entry.ExpenseDate,
                entry.AmountChf))
            .ToArray();

        var vehicleCompensations = monthlyRecord.VehicleCompensations
            .OrderBy(entry => entry.CompensationDate)
            .Select(entry => new MonthlyVehicleCompensationDto(
                entry.Id,
                entry.CompensationDate,
                entry.AmountChf,
                entry.Description))
            .ToArray();

        return new MonthlyRecordDetailsDto(
            header,
            timeEntries,
            expenseEntries,
            vehicleCompensations,
            new MonthlyRecordPreviewDto(previewRows, previewNotes));
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

        if (monthlyRecord.VehicleCompensations.Count == 0)
        {
            notes.Add("Keine Fahrzeugentschaedigung im aktuellen Monat erfasst.");
        }

        if (notes.Count == 0)
        {
            notes.Add("Monatsvorschau zeigt derzeit Verdichtung ohne automatische Payroll-Berechnung.");
        }

        return notes;
    }

    private async Task<IReadOnlyCollection<MonthlyPreviewRowDto>> BuildPreviewRowsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var monthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntries)
            .Include(record => record.VehicleCompensations)
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
                .Concat(record.ExpenseEntries.Select(entry => new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    entry.ExpenseDate,
                    "Spese",
                    $"{entry.AmountChf:0.00} CHF",
                    ExpenseEntry.DisplayName)))
                .Concat(record.VehicleCompensations.Select(entry => new MonthlyPreviewRowDto(
                    record.Year,
                    record.Month,
                    entry.CompensationDate,
                    "Fahrzeug",
                    $"{entry.AmountChf:0.00} CHF",
                    entry.Description)))
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
}
