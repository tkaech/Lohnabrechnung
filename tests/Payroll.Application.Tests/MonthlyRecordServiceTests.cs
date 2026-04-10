using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.Tests;

public sealed class MonthlyRecordServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_CreatesSingleMonthlyRecordPerEmployeeAndMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);

        var first = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));
        var second = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        Assert.Equal(first.Header.MonthlyRecordId, second.Header.MonthlyRecordId);
        Assert.Single(repository.MonthlyRecords);
    }

    [Fact]
    public async Task SaveTimeEntryAsync_RejectsDateOutsideMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);
        var details = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SaveTimeEntryAsync(
                new SaveMonthlyTimeEntryCommand(
                    details.Header.MonthlyRecordId,
                    null,
                    new DateOnly(2026, 5, 1),
                    8m,
                    0m,
                    0m,
                    0m,
                    0m,
                    0m,
                    0m,
                    null)));
    }

    [Fact]
    public async Task SaveExpenseEntryAsync_StoresSingleMonthlyExpenseBlock()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);
        var details = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        await service.SaveExpenseEntryAsync(
            new SaveMonthlyExpenseEntryCommand(
                details.Header.MonthlyRecordId,
                18.50m));

        var updated = await service.SaveExpenseEntryAsync(
            new SaveMonthlyExpenseEntryCommand(
                details.Header.MonthlyRecordId,
                80m));

        Assert.NotNull(updated.ExpenseEntry);
        Assert.Equal(80m, updated.Header.TotalExpensesChf);
        Assert.Equal(0m, updated.Header.TotalVehicleCompensationChf);
        Assert.Equal(80m, updated.ExpenseEntry!.ExpensesTotalChf);
    }

    private sealed class InMemoryMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        private readonly Dictionary<Guid, EmployeeMonthlyRecord> _monthlyRecordsById = [];
        private readonly Guid _employeeId;
        private readonly string _employeeName;

        public InMemoryMonthlyRecordRepository(Guid employeeId, string employeeName)
        {
            _employeeId = employeeId;
            _employeeName = employeeName;
        }

        public IReadOnlyCollection<EmployeeMonthlyRecord> MonthlyRecords => _monthlyRecordsById.Values;

        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            var existing = _monthlyRecordsById.Values.SingleOrDefault(item =>
                item.EmployeeId == employeeId && item.Year == year && item.Month == month);

            if (existing is not null)
            {
                return Task.FromResult(existing);
            }

            var created = new EmployeeMonthlyRecord(employeeId, year, month);
            _monthlyRecordsById[created.Id] = created;
            return Task.FromResult(created);
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            _monthlyRecordsById.TryGetValue(monthlyRecordId, out var record);
            return Task.FromResult(record);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            _monthlyRecordsById.TryGetValue(monthlyRecordId, out var record);
            if (record is null)
            {
                return Task.FromResult<MonthlyRecordDetailsDto?>(null);
            }

            var details = new MonthlyRecordDetailsDto(
                new MonthlyRecordHeaderDto(
                    record.Id,
                    _employeeId,
                    _employeeName,
                    "Nora",
                    "Feld",
                    "1000",
                    record.Year,
                    record.Month,
                    record.Status,
                    null,
                    null,
                    null,
                    null,
                    record.TimeEntries.Sum(entry => entry.HoursWorked),
                    record.TimeEntries.Sum(entry => entry.SupplementHours),
                    record.ExpenseEntry?.ExpensesTotalChf ?? 0m,
                    record.TimeEntries.Sum(entry => entry.VehicleCompensationTotalChf)),
                record.TimeEntries.Select(entry => new MonthlyTimeEntryDto(
                    entry.Id,
                    entry.WorkDate,
                    entry.HoursWorked,
                    entry.NightHours,
                    entry.SundayHours,
                    entry.HolidayHours,
                    entry.VehiclePauschalzone1Chf,
                    entry.VehiclePauschalzone2Chf,
                    entry.VehicleRegiezone1Chf,
                    entry.Note)).ToArray(),
                record.TimeEntries.Select(entry => new MonthlyTimeEntryDto(
                    entry.Id,
                    entry.WorkDate,
                    entry.HoursWorked,
                    entry.NightHours,
                    entry.SundayHours,
                    entry.HolidayHours,
                    entry.VehiclePauschalzone1Chf,
                    entry.VehiclePauschalzone2Chf,
                    entry.VehicleRegiezone1Chf,
                    entry.Note)).ToArray(),
                record.ExpenseEntry is null
                    ? null
                    : new MonthlyExpenseEntryDto(
                        record.ExpenseEntry.Id,
                        record.ExpenseEntry.ExpensesTotalChf),
                record.ExpenseEntry is null
                    ? Array.Empty<HistoricalMonthlyExpenseEntryDto>()
                    : [new HistoricalMonthlyExpenseEntryDto(
                        record.ExpenseEntry.Id,
                        record.Year,
                        record.Month,
                        record.ExpenseEntry.ExpensesTotalChf)],
                new MonthlyRecordPreviewDto(
                    Array.Empty<MonthlyPreviewRowDto>(),
                    ["Testvorschau"]),
                new MonthlyPayrollPreviewDto(
                    [
                        new MonthlyPayrollPreviewLineDto(PayrollPreviewHelpCatalog.BaseSalaryCode, "Basislohn", "0 h", "0.00 CHF", "0.00 CHF", null, false),
                        new MonthlyPayrollPreviewLineDto(PayrollPreviewHelpCatalog.TotalPayoutCode, "Total Auszahlung", "-", "gerundet auf 0.05", $"{(record.ExpenseEntry?.ExpensesTotalChf ?? 0m):0.00} CHF", null, true)
                    ],
                    ["Test-Lohnvorschau"]));

            return Task.FromResult<MonthlyRecordDetailsDto?>(details);
        }

        public Task<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> ListTimeCaptureOverviewAsync(int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>)Array.Empty<MonthlyTimeCaptureOverviewRowDto>());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void ClearTracking()
        {
        }

        public void MarkAsAdded<TEntity>(TEntity entity) where TEntity : class
        {
        }
    }
}
