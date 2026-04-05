using Payroll.Application.MonthlyRecords;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.MonthlyRecords;
using System.Globalization;

namespace Payroll.Application.Tests;

public sealed class MonthlyRecordViewModelTests
{
    [Fact]
    public async Task SetEmployeeAsync_LoadsCurrentMonthAndEnablesSaving()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Anna Aktiv");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Anna Aktiv");

        Assert.True(viewModel.CanSaveTimeEntry);
        Assert.True(viewModel.CanSaveExpenseEntry);
        Assert.Contains("Anna Aktiv", viewModel.ContextTitle, StringComparison.Ordinal);
        Assert.Contains("Status:", viewModel.StatusSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectedMonth_ReloadsContextAndSaveWorksWithoutManualLoadCommand()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Bruno Bereit");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Bruno Bereit");

        viewModel.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        await WaitUntilAsync(() => viewModel.CanSaveTimeEntry);

        viewModel.TimeDate = "2026-05-03";
        viewModel.HoursWorked = "8";
        viewModel.NightHours = "0";
        viewModel.SundayHours = "0";
        viewModel.HolidayHours = "0";
        viewModel.TimeNote = "Fruehdienst";

        viewModel.SaveTimeEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

        Assert.Single(viewModel.TimeEntries);
        Assert.Contains("05/2026", viewModel.ContextDescription, StringComparison.Ordinal);
        Assert.Contains("8", viewModel.TotalsSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VehicleCompensation_SaveAndSelectFlow_WorksWithinMonthlyContext()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Clara Car");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Clara Car");

        viewModel.VehicleCompensationDate = "2026-04-30";
        viewModel.VehicleCompensationAmount = "95.5";
        viewModel.VehicleCompensationDescription = "Privatfahrzeug April";

        viewModel.SaveVehicleCompensationCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.VehicleCompensations.Count == 1);

        var savedEntry = viewModel.VehicleCompensations.Single();
        viewModel.SelectedVehicleCompensation = savedEntry;

        Assert.Equal("2026-04-30", viewModel.VehicleCompensationDate);
        Assert.Equal("95.50", viewModel.VehicleCompensationAmount);
        Assert.Equal("Privatfahrzeug April", viewModel.VehicleCompensationDescription);
        Assert.Contains("Fahrzeug 95.50 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveTimeEntry_WithSwissCultureAndIsoDate_PersistsEntry()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-CH");
        CultureInfo.CurrentUICulture = new CultureInfo("de-CH");

        try
        {
            var employeeId = Guid.NewGuid();
            var repository = new InMemoryMonthlyRecordRepository();
            repository.RegisterEmployee(employeeId, "Dora Datum");
            var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

            await viewModel.SetEmployeeAsync(employeeId, "Dora Datum");

            viewModel.TimeDate = "2026-04-01";
            viewModel.HoursWorked = "3";
            viewModel.NightHours = "2";
            viewModel.SundayHours = "2";
            viewModel.HolidayHours = "2";
            viewModel.TimeNote = "test";

            viewModel.SaveTimeEntryCommand.Execute(null);
            await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

            Assert.Single(viewModel.TimeEntries);
            Assert.Contains("Stunden 3", viewModel.TotalsSummary, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task PreviewRows_ListAllAvailableMonthsTabularly()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Eva Eintrag");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Eva Eintrag");

        viewModel.TimeDate = "2026-04-02";
        viewModel.HoursWorked = "8";
        viewModel.NightHours = "1";
        viewModel.SundayHours = "0";
        viewModel.HolidayHours = "0";
        viewModel.TimeNote = "Fruehdienst";
        viewModel.SaveTimeEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

        viewModel.ExpenseDate = "2026-04-03";
        viewModel.ExpenseAmount = "18.50";
        viewModel.SaveExpenseEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ExpenseEntries.Count == 1);

        viewModel.VehicleCompensationDate = "2026-04-04";
        viewModel.VehicleCompensationAmount = "95";
        viewModel.VehicleCompensationDescription = "Auto";
        viewModel.SaveVehicleCompensationCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.VehicleCompensations.Count == 1);

        viewModel.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        await WaitUntilAsync(() => viewModel.CanSaveTimeEntry);

        viewModel.TimeDate = "2026-05-06";
        viewModel.HoursWorked = "6";
        viewModel.NightHours = "0";
        viewModel.SundayHours = "0";
        viewModel.HolidayHours = "0";
        viewModel.TimeNote = "Spaetdienst";
        viewModel.SaveTimeEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.PreviewRows.Any(row => row.MonthLabel == "05/2026"));

        Assert.True(viewModel.PreviewRows.Count >= 4);
        Assert.Contains(viewModel.PreviewRows, row => row.MonthLabel == "04/2026" && row.EntryType == "Zeit");
        Assert.Contains(viewModel.PreviewRows, row => row.MonthLabel == "04/2026" && row.EntryType == "Spese");
        Assert.Contains(viewModel.PreviewRows, row => row.MonthLabel == "04/2026" && row.EntryType == "Fahrzeug");
        Assert.Contains(viewModel.PreviewRows, row => row.MonthLabel == "05/2026" && row.EntryType == "Zeit");
        Assert.Contains("alle vorhandenen Monate", viewModel.PreviewSummary, StringComparison.Ordinal);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var startedAt = DateTime.UtcNow;

        while (!condition())
        {
            if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met within the expected time.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class InMemoryMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        private readonly Dictionary<(Guid EmployeeId, int Year, int Month), EmployeeMonthlyRecord> _records = [];
        private readonly Dictionary<string, string> _employeeNames = [];

        public void RegisterEmployee(Guid employeeId, string employeeName)
        {
            _employeeNames[employeeId.ToString("N")] = employeeName;
        }

        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            if (_records.TryGetValue((employeeId, year, month), out var existingRecord))
            {
                return Task.FromResult(existingRecord);
            }

            var createdRecord = new EmployeeMonthlyRecord(employeeId, year, month);
            _records[(employeeId, year, month)] = createdRecord;
            return Task.FromResult(createdRecord);
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            var record = _records.Values.SingleOrDefault(item => item.Id == monthlyRecordId);
            return Task.FromResult<EmployeeMonthlyRecord?>(record);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            var record = _records.Values.SingleOrDefault(item => item.Id == monthlyRecordId);
            if (record is null)
            {
                return Task.FromResult<MonthlyRecordDetailsDto?>(null);
            }

            var employeeName = _employeeNames.TryGetValue(record.EmployeeId.ToString("N"), out var name)
                ? name
                : "Unbekannt";

            var details = new MonthlyRecordDetailsDto(
                new MonthlyRecordHeaderDto(
                    record.Id,
                    record.EmployeeId,
                    employeeName,
                    record.Year,
                    record.Month,
                    record.Status,
                    null,
                    null,
                    null,
                    null,
                    record.TimeEntries.Sum(item => item.HoursWorked),
                    record.TimeEntries.Sum(item => item.NightHours + item.SundayHours + item.HolidayHours),
                    record.ExpenseEntries.Sum(item => item.AmountChf),
                    record.VehicleCompensations.Sum(item => item.AmountChf)),
                record.TimeEntries
                    .OrderBy(item => item.WorkDate)
                    .Select(item => new MonthlyTimeEntryDto(item.Id, item.WorkDate, item.HoursWorked, item.NightHours, item.SundayHours, item.HolidayHours, item.Note))
                    .ToArray(),
                record.ExpenseEntries
                    .OrderBy(item => item.ExpenseDate)
                    .Select(item => new MonthlyExpenseEntryDto(item.Id, item.ExpenseDate, item.AmountChf))
                    .ToArray(),
                record.VehicleCompensations
                    .OrderBy(item => item.CompensationDate)
                    .Select(item => new MonthlyVehicleCompensationDto(item.Id, item.CompensationDate, item.AmountChf, item.Description))
                    .ToArray(),
                new MonthlyRecordPreviewDto(
                    BuildPreviewRows(_records.Values.Where(item => item.EmployeeId == record.EmployeeId)),
                    Array.Empty<string>()));

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

        private static IReadOnlyCollection<MonthlyPreviewRowDto> BuildPreviewRows(IEnumerable<EmployeeMonthlyRecord> records)
        {
            var rows = records
                .OrderByDescending(record => record.Year)
                .ThenByDescending(record => record.Month)
                .SelectMany(record =>
                {
                    var monthRows = record.TimeEntries
                        .Select(item => new MonthlyPreviewRowDto(
                            record.Year,
                            record.Month,
                            item.WorkDate,
                            "Zeit",
                            $"{item.HoursWorked:0.##} h",
                            string.IsNullOrWhiteSpace(item.Note) ? "Keine Zusatzangaben" : item.Note!))
                        .Concat(record.ExpenseEntries.Select(item => new MonthlyPreviewRowDto(
                            record.Year,
                            record.Month,
                            item.ExpenseDate,
                            "Spese",
                            $"{item.AmountChf:0.00} CHF",
                            global::Payroll.Domain.Expenses.ExpenseEntry.DisplayName)))
                        .Concat(record.VehicleCompensations.Select(item => new MonthlyPreviewRowDto(
                            record.Year,
                            record.Month,
                            item.CompensationDate,
                            "Fahrzeug",
                            $"{item.AmountChf:0.00} CHF",
                            item.Description)))
                        .OrderBy(item => item.EntryDate)
                        .ThenBy(item => item.EntryType)
                        .ToArray();

                    return monthRows.Length == 0
                        ? [new MonthlyPreviewRowDto(record.Year, record.Month, null, "Monat", "-", "Keine Eintraege")]
                        : monthRows;
                })
                .ToArray();

            return rows;
        }
    }
}
