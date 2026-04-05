using Payroll.Application.MonthlyRecords;
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
                    null)));
    }

    [Fact]
    public async Task SaveExpenseEntryAsync_KeepsVehicleCompensationSeparated()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);
        var details = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        var updated = await service.SaveExpenseEntryAsync(
            new SaveMonthlyExpenseEntryCommand(
                details.Header.MonthlyRecordId,
                null,
                new DateOnly(2026, 4, 10),
                18.50m));

        Assert.Single(updated.ExpenseEntries);
        Assert.Empty(updated.VehicleCompensations);
    }

    [Fact]
    public async Task SaveExpenseEntryAsync_StoresSingleMonthlyExpenseTotal()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);
        var details = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        await service.SaveExpenseEntryAsync(
            new SaveMonthlyExpenseEntryCommand(
                details.Header.MonthlyRecordId,
                null,
                new DateOnly(2026, 4, 10),
                18.50m));

        var updated = await service.SaveExpenseEntryAsync(
            new SaveMonthlyExpenseEntryCommand(
                details.Header.MonthlyRecordId,
                null,
                new DateOnly(2026, 4, 30),
                80m));

        Assert.Single(updated.ExpenseEntries);
        Assert.Equal(80m, updated.Header.TotalExpensesChf);
        Assert.Equal(new DateOnly(2026, 4, 30), updated.ExpenseEntries.Single().ExpenseDate);
    }

    [Fact]
    public async Task SaveVehicleCompensationAsync_SavesSeparateVehicleCompensation()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository(employeeId, "Anna Aktiv");
        var service = new MonthlyRecordService(repository);
        var details = await service.GetOrCreateAsync(new MonthlyRecordQuery(employeeId, 2026, 4));

        var updated = await service.SaveVehicleCompensationAsync(
            new SaveMonthlyVehicleCompensationCommand(
                details.Header.MonthlyRecordId,
                null,
                new DateOnly(2026, 4, 30),
                120m,
                "Firmenfahrzeug"));

        Assert.Empty(updated.ExpenseEntries);
        Assert.Single(updated.VehicleCompensations);
        Assert.Equal(120m, updated.Header.TotalVehicleCompensationChf);
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
                    record.Year,
                    record.Month,
                    record.Status,
                    null,
                    null,
                    null,
                    null,
                    record.TimeEntries.Sum(entry => entry.HoursWorked),
                    record.TimeEntries.Sum(entry => entry.SupplementHours),
                    record.ExpenseEntries.Sum(entry => entry.AmountChf),
                    record.VehicleCompensations.Sum(entry => entry.AmountChf)),
                record.TimeEntries.Select(entry => new MonthlyTimeEntryDto(
                    entry.Id,
                    entry.WorkDate,
                    entry.HoursWorked,
                    entry.NightHours,
                    entry.SundayHours,
                    entry.HolidayHours,
                    entry.Note)).ToArray(),
                record.ExpenseEntries.Select(entry => new MonthlyExpenseEntryDto(
                    entry.Id,
                    entry.ExpenseDate,
                    entry.AmountChf)).ToArray(),
                record.VehicleCompensations.Select(entry => new MonthlyVehicleCompensationDto(
                    entry.Id,
                    entry.CompensationDate,
                    entry.AmountChf,
                    entry.Description)).ToArray(),
                new MonthlyRecordPreviewDto(
                    Array.Empty<MonthlyPreviewRowDto>(),
                    ["Testvorschau"]));

            return Task.FromResult<MonthlyRecordDetailsDto?>(details);
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
