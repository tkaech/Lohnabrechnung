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
        viewModel.VehiclePauschalzone1 = "10";
        viewModel.VehiclePauschalzone2 = "11";
        viewModel.VehicleRegiezone1 = "12";
        viewModel.TimeNote = "Fruehdienst";

        viewModel.SaveTimeEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

        Assert.Single(viewModel.TimeEntries);
        Assert.Contains("05/2026", viewModel.ContextDescription, StringComparison.Ordinal);
        Assert.Contains("8", viewModel.TotalsSummary, StringComparison.Ordinal);
        Assert.Contains("Fahrzeug 33.00 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MonthlyExpenseValues_SaveAndReloadFlow_WorksWithinMonthlyContext()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Clara Car");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Clara Car");

        viewModel.ExpensesTotal = "40";

        viewModel.SaveExpenseEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ActionMessage == "Spesen gespeichert.");

        Assert.Equal("40.00", viewModel.ExpensesTotal);
        Assert.Contains("Spesen 40.00 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
        Assert.False(viewModel.HasPayrollPreviewLines);
        Assert.Single(viewModel.PayrollPreviewNotes, note => note == "Monat noch nicht erfasst");
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
            viewModel.VehiclePauschalzone1 = "1";
            viewModel.VehiclePauschalzone2 = "2";
            viewModel.VehicleRegiezone1 = "3";
            viewModel.TimeNote = "test";

            viewModel.SaveTimeEntryCommand.Execute(null);
            await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

            Assert.Single(viewModel.TimeEntries);
            Assert.Contains("Stunden 3", viewModel.TotalsSummary, StringComparison.Ordinal);
            Assert.Contains("Fahrzeug 6.00 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task TimePayrollMonth_TracksSelectedMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Eva Eintrag");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Eva Eintrag");
        viewModel.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        await WaitUntilAsync(() => viewModel.CanSaveTimeEntry);

        Assert.Equal("05/2026", viewModel.TimePayrollMonth);
        Assert.Equal("05/2026", viewModel.ExpensePayrollMonth);
    }

    [Fact]
    public async Task HistoryLists_LoadEntriesFromPreviousMonthsChronologically()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Gina Verlauf");

        var aprilRecord = await repository.GetOrCreateAsync(employeeId, 2026, 4, CancellationToken.None);
        aprilRecord.SaveTimeEntry(null, new DateOnly(2026, 4, 12), 7m, 0m, 0m, 0m, 0m, 0m, 0m, "April");
        aprilRecord.SaveExpenseEntry(18.50m);

        var mayRecord = await repository.GetOrCreateAsync(employeeId, 2026, 5, CancellationToken.None);
        mayRecord.SaveTimeEntry(null, new DateOnly(2026, 5, 3), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Mai");
        mayRecord.SaveExpenseEntry(22m);

        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository))
        {
            SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employeeId, "Gina Verlauf");
        await WaitUntilAsync(() => viewModel.TimeEntryHistory.Count == 2 && viewModel.ExpenseEntryHistory.Count == 2);

        Assert.Equal(2, viewModel.TimeEntryHistory.Count);
        Assert.Equal(new DateOnly(2026, 4, 12), viewModel.TimeEntryHistory[0].WorkDate);
        Assert.Equal(new DateOnly(2026, 5, 3), viewModel.TimeEntryHistory[1].WorkDate);
        Assert.Equal(2, viewModel.ExpenseEntryHistory.Count);
        Assert.Equal((2026, 4), (viewModel.ExpenseEntryHistory[0].Year, viewModel.ExpenseEntryHistory[0].Month));
        Assert.Equal((2026, 5), (viewModel.ExpenseEntryHistory[1].Year, viewModel.ExpenseEntryHistory[1].Month));
    }

    [Fact]
    public async Task ActivateMonthFromTimeEntryAsync_SwitchesSelectedMonthAndLoadsTargetMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Hugo Historie");

        var aprilRecord = await repository.GetOrCreateAsync(employeeId, 2026, 4, CancellationToken.None);
        aprilRecord.SaveTimeEntry(null, new DateOnly(2026, 4, 8), 6m, 0m, 0m, 0m, 0m, 0m, 0m, "April");

        var mayRecord = await repository.GetOrCreateAsync(employeeId, 2026, 5, CancellationToken.None);
        mayRecord.SaveTimeEntry(null, new DateOnly(2026, 5, 3), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Mai");

        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository))
        {
            SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employeeId, "Hugo Historie");
        await WaitUntilAsync(() => viewModel.TimeEntryHistory.Count == 2);

        await viewModel.ActivateMonthFromTimeEntryAsync(viewModel.TimeEntryHistory[0]);
        await WaitUntilAsync(() => viewModel.TimePayrollMonth == "04/2026" && viewModel.SelectedTimeEntry is not null);

        Assert.Equal("04/2026", viewModel.TimePayrollMonth);
        Assert.Single(viewModel.TimeEntries);
        Assert.Equal(new DateOnly(2026, 4, 8), viewModel.TimeEntries[0].WorkDate);
        Assert.NotNull(viewModel.SelectedTimeEntry);
        Assert.Equal("2026-04-08", viewModel.TimeDate);
        Assert.Equal("6", viewModel.HoursWorked);
        Assert.Contains("04/2026", viewModel.ContextDescription, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivateMonthFromExpenseEntryAsync_SwitchesSelectedMonthAndLoadsTargetMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Iris Historie");

        var aprilRecord = await repository.GetOrCreateAsync(employeeId, 2026, 4, CancellationToken.None);
        aprilRecord.SaveExpenseEntry(18.50m);

        var mayRecord = await repository.GetOrCreateAsync(employeeId, 2026, 5, CancellationToken.None);
        mayRecord.SaveExpenseEntry(22m);

        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository))
        {
            SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employeeId, "Iris Historie");
        await WaitUntilAsync(() => viewModel.ExpenseEntryHistory.Count == 2);

        await viewModel.ActivateMonthFromExpenseEntryAsync(viewModel.ExpenseEntryHistory[0]);
        await WaitUntilAsync(() => viewModel.ExpensePayrollMonth == "04/2026" && viewModel.SelectedExpenseEntry is not null);

        Assert.Equal("04/2026", viewModel.ExpensePayrollMonth);
        Assert.Equal("18.50", viewModel.ExpensesTotal);
        Assert.NotNull(viewModel.SelectedExpenseEntry);
        Assert.Equal((2026, 4), (viewModel.SelectedExpenseEntry!.Year, viewModel.SelectedExpenseEntry.Month));
        Assert.Contains("04/2026", viewModel.ContextDescription, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PayrollPreview_LoadsBaseTotalAndPayoutForCurrentMonth()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Fina Vorschau");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Fina Vorschau");

        viewModel.TimeDate = "2026-04-03";
        viewModel.HoursWorked = "8";
        viewModel.SaveTimeEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.TimeEntries.Count == 1);

        viewModel.ExpensesTotal = "18.50";
        viewModel.SaveExpenseEntryCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.ActionMessage == "Spesen gespeichert.");

        Assert.Contains(viewModel.PayrollPreviewLines, line => line.Label == "Basislohn" && line.AmountDisplay == "240.00 CHF");
        Assert.Contains(viewModel.PayrollPreviewLines, line => line.Label == "AHV-pflichtiger Bruttolohn" && line.AmountDisplay == "240.00 CHF");
        Assert.Contains(viewModel.PayrollPreviewLines, line => line.Label == "Total Auszahlung");
    }

    [Fact]
    public async Task PayrollPreview_WithoutTimeEntries_ShowsMonthNotRecordedHintOnly()
    {
        var employeeId = Guid.NewGuid();
        var repository = new InMemoryMonthlyRecordRepository();
        repository.RegisterEmployee(employeeId, "Nina NochNichtErfasst");
        var viewModel = new MonthlyRecordViewModel(new MonthlyRecordService(repository));

        await viewModel.SetEmployeeAsync(employeeId, "Nina NochNichtErfasst");

        Assert.False(viewModel.HasPayrollPreviewLines);
        Assert.Equal("Monat noch nicht erfasst", viewModel.PayrollPreviewSummary);
        Assert.Single(viewModel.PayrollPreviewNotes, note => note == "Monat noch nicht erfasst");
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
                    record.ExpenseEntry?.ExpensesTotalChf ?? 0m,
                    record.TimeEntries.Sum(item => item.VehicleCompensationTotalChf)),
                record.TimeEntries
                    .OrderBy(item => item.WorkDate)
                    .Select(item => new MonthlyTimeEntryDto(item.Id, item.WorkDate, item.HoursWorked, item.NightHours, item.SundayHours, item.HolidayHours, item.VehiclePauschalzone1Chf, item.VehiclePauschalzone2Chf, item.VehicleRegiezone1Chf, item.Note))
                    .ToArray(),
                _records.Values
                    .Where(item => item.EmployeeId == record.EmployeeId)
                    .SelectMany(item => item.TimeEntries)
                    .OrderBy(item => item.WorkDate)
                    .Select(item => new MonthlyTimeEntryDto(item.Id, item.WorkDate, item.HoursWorked, item.NightHours, item.SundayHours, item.HolidayHours, item.VehiclePauschalzone1Chf, item.VehiclePauschalzone2Chf, item.VehicleRegiezone1Chf, item.Note))
                    .ToArray(),
                record.ExpenseEntry is null
                    ? null
                    : new MonthlyExpenseEntryDto(
                        record.ExpenseEntry.Id,
                        record.ExpenseEntry.ExpensesTotalChf),
                _records.Values
                    .Where(item => item.EmployeeId == record.EmployeeId && item.ExpenseEntry is not null)
                    .OrderBy(item => item.Year)
                    .ThenBy(item => item.Month)
                    .Select(item => new HistoricalMonthlyExpenseEntryDto(
                        item.ExpenseEntry!.Id,
                        item.Year,
                        item.Month,
                        item.ExpenseEntry.ExpensesTotalChf))
                    .ToArray(),
                new MonthlyRecordPreviewDto(
                    BuildPreviewRows(_records.Values.Where(item => item.EmployeeId == record.EmployeeId)),
                    Array.Empty<string>()),
                BuildPayrollPreview(record));

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

        private static MonthlyPayrollPreviewDto BuildPayrollPreview(EmployeeMonthlyRecord record)
        {
            if (record.TimeEntries.Count == 0)
            {
                return new MonthlyPayrollPreviewDto([], ["Monat noch nicht erfasst"]);
            }

            var baseAmount = record.TimeEntries.Sum(item => item.HoursWorked) * 30m;
            var expenses = record.ExpenseEntry?.ExpensesTotalChf ?? 0m;

            return new MonthlyPayrollPreviewDto(
                [
                    new MonthlyPayrollPreviewLineDto("Basislohn", $"{record.TimeEntries.Sum(item => item.HoursWorked):0.##} h", "30.00 CHF", $"{baseAmount:0.00} CHF", null, false),
                    new MonthlyPayrollPreviewLineDto("AHV-pflichtiger Bruttolohn", "-", "-", $"{baseAmount:0.00} CHF", null, true),
                    new MonthlyPayrollPreviewLineDto("Spesen gemaess Nachweis", "-", "-", $"{expenses:0.00} CHF", null, false),
                    new MonthlyPayrollPreviewLineDto("Total Auszahlung", "-", "gerundet auf 0.05", $"{(baseAmount + expenses):0.00} CHF", null, true)
                ],
                ["Test-Lohnvorschau"]);
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
                        .Concat(record.TimeEntries
                            .Where(item => item.VehicleCompensationTotalChf > 0m)
                            .Select(item => new MonthlyPreviewRowDto(
                                record.Year,
                                record.Month,
                                item.WorkDate,
                                "Fahrzeug",
                                $"{item.VehicleCompensationTotalChf:0.00} CHF",
                                $"P1 {item.VehiclePauschalzone1Chf:0.00} | P2 {item.VehiclePauschalzone2Chf:0.00} | R1 {item.VehicleRegiezone1Chf:0.00}")))
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
