using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void NumericFormatManager_ParsesDotAndCommaDecimalsWithoutIntegerFallback()
    {
        NumericFormatManager.ApplyDecimalSeparator(".");

        Assert.True(NumericFormatManager.TryParseDecimal("0.053", out var dotValue));
        Assert.Equal(0.053m, dotValue);

        Assert.True(NumericFormatManager.TryParseDecimal("0,053", out var commaValue));
        Assert.Equal(0.053m, commaValue);

        NumericFormatManager.ApplyDecimalSeparator(",");
    }

    [Fact]
    public async Task InitializeAsync_SelectsFirstEmployeeAndLoadsDetails()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();

        await WaitUntilAsync(() =>
            viewModel.PersonnelNumber == "1000"
            && viewModel.SelectedEmployee?.EmployeeId == employee.EmployeeId
            && viewModel.FirstName == "Anna"
            && viewModel.LastName == "Aktiv");

        Assert.Equal("Anna", viewModel.FirstName);
        Assert.Equal("Aktiv", viewModel.LastName);
        Assert.Contains("geladen", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectedEmployee_LoadsDifferentEmployeeAfterSelectionChange()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var repository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.SelectedEmployee = viewModel.Employees.Single(item => item.EmployeeId == secondEmployee.EmployeeId);
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1001" && viewModel.FirstName == "Bruno");

        Assert.Equal(secondEmployee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.Equal("Bruno", viewModel.FirstName);
        Assert.Equal("Bereit", viewModel.LastName);
        Assert.Contains("geladen", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelCommand_AfterCreatingNewEmployee_RestoresPreviousSelection()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var repository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.SelectedEmployee = viewModel.Employees.Single(item => item.EmployeeId == secondEmployee.EmployeeId);
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1001");

        viewModel.NewEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanCancel);

        viewModel.CancelCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.PersonnelNumber == "1001");

        Assert.Equal(secondEmployee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.False(viewModel.CanSave);
        Assert.Equal("Neueingabe verworfen.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ConfirmDeleteCommand_ArchivesEmployeeAndReloadsCurrentSelection()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        Assert.False(viewModel.CanRequestDelete);
        viewModel.DeleteCommand.Execute(null);
        Assert.False(viewModel.ShowDeleteConfirmation);

        viewModel.EditEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanRequestDelete);

        viewModel.DeleteCommand.Execute(null);
        Assert.True(viewModel.ShowDeleteConfirmation);
        Assert.True(viewModel.ConfirmDeleteCommand.CanExecute(null));
        Assert.True(viewModel.DismissDeleteCommand.CanExecute(null));

        viewModel.ConfirmDeleteCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender archiviert und auf inaktiv gesetzt.");

        Assert.False(viewModel.IsActiveEmployee);
        Assert.Equal(employee.EmployeeId, viewModel.SelectedEmployee?.EmployeeId);
        Assert.Equal("Inaktiv", viewModel.SelectedEmployee?.StatusSummary);
    }

    [Fact]
    public async Task SearchRemainsAvailableInEditMode()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);

        Assert.True(viewModel.CanSearchEmployees);
        Assert.True(viewModel.SearchCommand.CanExecute(null));
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task SearchBecomesAvailableAgainAfterBusyInitialization()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        repository.GetByIdDelay = _ => repository.FirstLoadRelease.Task;
        var viewModel = CreateViewModel(repository);

        var initializeTask = viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.IsBusy);

        Assert.False(viewModel.CanSearchEmployees);

        repository.FirstLoadRelease.SetResult();
        await initializeTask;
        await WaitUntilAsync(() => !viewModel.IsBusy);

        Assert.True(viewModel.CanSearchEmployees);
        Assert.True(viewModel.SearchCommand.CanExecute(null));
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReactivatingEmployee_ClearsExitDateAndSavesActiveStatus()
    {
        var employee = TestEmployeeRepository.CreateInactiveDetails("1000", "Anna", "Archiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanClearExitDate);

        viewModel.IsActiveEmployee = true;

        Assert.Null(viewModel.ExitDate);
        Assert.False(viewModel.CanClearExitDate);

        viewModel.SaveCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender 1000 gespeichert.");

        Assert.True(viewModel.IsActiveEmployee);
        Assert.Null(viewModel.ExitDate);
        Assert.Equal("Aktiv", viewModel.SelectedEmployee?.StatusSummary);
    }

    [Fact]
    public async Task ClearExitDateCommand_RemovesExitDateInEditMode()
    {
        var employee = TestEmployeeRepository.CreateInactiveDetails("1000", "Anna", "Archiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);
        Assert.True(viewModel.CanClearExitDate);

        viewModel.ClearExitDateCommand.Execute(null);

        Assert.Null(viewModel.ExitDate);
        Assert.False(viewModel.CanClearExitDate);
    }

    [Fact]
    public void WorkspaceDefaultsToTimeAndExpenses_AndCanSwitchToPayrollRunsReportingEmployeesSettingsAndHelp()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        Assert.True(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.IsReportingWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);

        viewModel.ShowPayrollRunsCommand.Execute(null);

        Assert.True(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsReportingWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);
        Assert.True(viewModel.ShowEmployeeSelectionArea);
        Assert.True(viewModel.ShowPrimaryWorkspaceArea);

        viewModel.ShowReportingCommand.Execute(null);

        Assert.True(viewModel.IsReportingWorkspace);
        Assert.False(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);
        Assert.True(viewModel.ShowEmployeeSelectionArea);
        Assert.True(viewModel.ShowPrimaryWorkspaceArea);

        viewModel.ShowEmployeesCommand.Execute(null);

        Assert.True(viewModel.IsEmployeeWorkspace);
        Assert.False(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.IsReportingWorkspace);

        viewModel.ShowTimeAndExpensesCommand.Execute(null);

        Assert.True(viewModel.IsTimeAndExpensesWorkspace);
        Assert.False(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.IsReportingWorkspace);
        Assert.False(viewModel.IsEmployeeWorkspace);

        viewModel.ShowSettingsCommand.Execute(null);

        Assert.True(viewModel.IsSettingsWorkspace);
        Assert.False(viewModel.ShowEmployeeSelectionArea);
        Assert.False(viewModel.ShowPrimaryWorkspaceArea);

        viewModel.ShowHelpCommand.Execute(null);

        Assert.True(viewModel.IsHelpWorkspace);
        Assert.False(viewModel.ShowEmployeeSelectionArea);
        Assert.False(viewModel.ShowPrimaryWorkspaceArea);
    }

    [Fact]
    public async Task MonthCaptureOverview_LoadsAndFiltersRowsForSelectedMonth()
    {
        var employeeRepository = new TestEmployeeRepository(
            TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern"),
            TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich"));
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            4,
            [
                new MonthlyTimeCaptureOverviewRowDto(Guid.NewGuid(), "1000", "Anna", "Aktiv", true, true, 12m, 1m, 0m, 0m, 2m, 0m, 0m, 2),
                new MonthlyTimeCaptureOverviewRowDto(Guid.NewGuid(), "1001", "Bruno", "Bereit", true, false, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0)
            ]);

        var monthlyRecordService = new MonthlyRecordService(monthlyRecordRepository);
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            "Test");
        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.MonthCaptureOverviewRows.Count == 2);

        Assert.Equal("04/2026", viewModel.MonthCaptureMonthLabel);

        viewModel.SelectedMonthCaptureFilter = "Ohne Monatserfassung";
        Assert.Single(viewModel.MonthCaptureOverviewRows);
        Assert.Equal("1001", viewModel.MonthCaptureOverviewRows[0].PersonnelNumber);

        viewModel.SelectedMonthCaptureFilter = "Mit Monatserfassung";
        Assert.Single(viewModel.MonthCaptureOverviewRows);
        Assert.Equal("1000", viewModel.MonthCaptureOverviewRows[0].PersonnelNumber);
    }

    [Fact]
    public async Task SettingsWorkspace_LoadsAndSavesCentralSupplementRatesWithoutEmployeeSelection()
    {
        var employeeRepository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            "Test");

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.SettingsNightSupplementRate == "25");

        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.SettingsDecimalSeparator = ",";
        viewModel.SettingsNightSupplementRate = "30";
        viewModel.SettingsSundaySupplementRate = "60";
        viewModel.SettingsHolidaySupplementRate = "110";
        viewModel.SettingsAhvIvEoRate = "5,4";
        viewModel.SettingsAlvRate = "1,2";
        viewModel.SettingsSicknessAccidentInsuranceRate = "0,9";
        viewModel.SettingsTrainingAndHolidayRate = "0,02";
        viewModel.SettingsVacationCompensationRate = "10,64";
        viewModel.SettingsVacationCompensationRateAge50Plus = "12,64";
        viewModel.SettingsVehiclePauschalzone1RateChf = "1,5";
        viewModel.SettingsVehiclePauschalzone2RateChf = "2,5";
        viewModel.SettingsVehicleRegiezone1RateChf = "3,5";
        viewModel.SettingsCompanyAddress = "Blesinger Sicherheits Dienste GmbH\nPostfach 28\n6314 Unteraegeri";
        viewModel.SettingsAppFontFamily = "Aptos";
        viewModel.SettingsAppFontSize = "14";
        viewModel.SettingsAppTextColorHex = "#FF101820";
        viewModel.SettingsAppMutedTextColorHex = "#FF667788";
        viewModel.SettingsAppBackgroundColorHex = "#FFF6F8FB";
        viewModel.SettingsAppAccentColorHex = "#FF224466";
        viewModel.SettingsAppLogoText = "BSD";
        viewModel.SettingsPrintFontFamily = "Helvetica";
        viewModel.SettingsPrintFontSize = "10";
        viewModel.SettingsPrintTextColorHex = "#FF000000";
        viewModel.SettingsPrintMutedTextColorHex = "#FF556677";
        viewModel.SettingsPrintAccentColorHex = "#FFFFFF00";
        viewModel.SettingsPrintLogoText = "BSD";
        viewModel.SettingsPrintTemplate = "BANNER|Lohnblatt|{{Monat}}";
        viewModel.NewDepartmentName = "Werkhof";
        viewModel.AddDepartmentOptionCommand.Execute(null);
        viewModel.SaveSettingsCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.StatusMessage == "Einstellungen gespeichert.");

        Assert.True(viewModel.IsSettingsWorkspace);
        Assert.False(viewModel.ShowEmployeeSelectionArea);
        Assert.False(viewModel.ShowPrimaryWorkspaceArea);
        Assert.Empty(viewModel.Employees);
        Assert.Equal(0.30m, settingsRepository.Current.NightSupplementRate);
        Assert.Equal(0.60m, settingsRepository.Current.SundaySupplementRate);
        Assert.Equal(1.10m, settingsRepository.Current.HolidaySupplementRate);
        Assert.Equal(0.054m, settingsRepository.Current.AhvIvEoRate);
        Assert.Equal(0.012m, settingsRepository.Current.AlvRate);
        Assert.Equal(0.009m, settingsRepository.Current.SicknessAccidentInsuranceRate);
        Assert.Equal(0.0002m, settingsRepository.Current.TrainingAndHolidayRate);
        Assert.Equal(0.1064m, settingsRepository.Current.VacationCompensationRate);
        Assert.Equal(0.1264m, settingsRepository.Current.VacationCompensationRateAge50Plus);
        Assert.Equal(1.5m, settingsRepository.Current.VehiclePauschalzone1RateChf);
        Assert.Equal(2.5m, settingsRepository.Current.VehiclePauschalzone2RateChf);
        Assert.Equal(3.5m, settingsRepository.Current.VehicleRegiezone1RateChf);
        Assert.Contains("Blesinger Sicherheits Dienste", settingsRepository.Current.CompanyAddress, StringComparison.Ordinal);
        Assert.Equal("Aptos", settingsRepository.Current.AppFontFamily);
        Assert.Equal(14m, settingsRepository.Current.AppFontSize);
        Assert.Equal("#FF224466", settingsRepository.Current.AppAccentColorHex);
        Assert.Equal("Helvetica", settingsRepository.Current.PrintFontFamily);
        Assert.Equal(",", settingsRepository.Current.DecimalSeparator);
        Assert.Equal(10m, settingsRepository.Current.PrintFontSize);
        Assert.Equal("BANNER|Lohnblatt|{{Monat}}", settingsRepository.Current.PrintTemplate);
        Assert.Equal("BSD", settingsRepository.Current.AppLogoText);
        Assert.Contains(settingsRepository.Current.Departments, item => item.Name == "Werkhof");
    }

    [Fact]
    public async Task SettingsWorkspace_CreatesBackupAndUsesRestorePath()
    {
        var employeeRepository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var backupRestoreService = new InMemoryBackupRestoreService();
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            backupRestoreService,
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            "Test");

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.BackupDirectoryPath = "/tmp/backups";
        viewModel.BackupFileName = "backup_2026-04-07_14-35";
        viewModel.SelectedBackupContentType = "Nur Konfiguration";

        viewModel.CreateBackupCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage.Contains("Backup erstellt:", StringComparison.Ordinal));

        Assert.Equal("/tmp/backups", backupRestoreService.LastCreateCommand?.TargetDirectoryPath);
        Assert.Equal("backup_2026-04-07_14-35", backupRestoreService.LastCreateCommand?.FileName);
        Assert.Equal(BackupContentType.Configuration, backupRestoreService.LastCreateCommand?.ContentType);

        viewModel.RestoreFilePath = "/tmp/backups/restore.payrollbackup.json";
        viewModel.SelectedRestoreContentType = "Nur Konfiguration";
        viewModel.RestoreBackupCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage.Contains("Restore abgeschlossen:", StringComparison.Ordinal));

        Assert.Equal("/tmp/backups/restore.payrollbackup.json", backupRestoreService.LastRestoreCommand?.BackupFilePath);
        Assert.Equal(BackupContentType.Configuration, backupRestoreService.LastRestoreCommand?.ContentType);
    }

    private static MainWindowViewModel CreateViewModel(TestEmployeeRepository repository)
    {
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());

        return new MainWindowViewModel(
            new EmployeeService(repository),
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(repository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            "Test");
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

    private sealed class TestEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<Guid, EmployeeDetailsDto> _employees;

        public TestEmployeeRepository(params EmployeeDetailsDto[] employees)
        {
            _employees = employees.ToDictionary(employee => employee.EmployeeId);
            FirstLoadRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Func<Guid, Task>? GetByIdDelay { get; set; }
        public TaskCompletionSource FirstLoadRelease { get; }

        public Task<IReadOnlyCollection<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<EmployeeDetailsDto> items = _employees.Values;

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var searchText = query.SearchText.Trim();
                items = items.Where(item =>
                    item.PersonnelNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || item.City.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || (item.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (query.IsActive.HasValue)
            {
                items = items.Where(item => item.IsActive == query.IsActive.Value);
            }

            var result = items
                .OrderBy(item => item.LastName)
                .ThenBy(item => item.FirstName)
                .Select(ToListItem)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EmployeeListItemDto>>(result);
        }

        public async Task<EmployeeDetailsDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            if (GetByIdDelay is not null)
            {
                await GetByIdDelay(employeeId);
            }

            _employees.TryGetValue(employeeId, out var employee);
            return employee;
        }

        public Task<bool> PersonnelNumberExistsAsync(string personnelNumber, Guid? excludingEmployeeId, CancellationToken cancellationToken)
        {
            var trimmedPersonnelNumber = personnelNumber.Trim();
            var result = _employees.Values.Any(item =>
                item.PersonnelNumber == trimmedPersonnelNumber
                && (!excludingEmployeeId.HasValue || item.EmployeeId != excludingEmployeeId.Value));

            return Task.FromResult(result);
        }

        public Task ArchiveAsync(Guid employeeId, CancellationToken cancellationToken)
        {
            if (_employees.TryGetValue(employeeId, out var employee))
            {
                _employees[employeeId] = employee with
                {
                    IsActive = false,
                    ExitDate = employee.ExitDate ?? DateOnly.FromDateTime(DateTime.Today)
                };
            }

            return Task.CompletedTask;
        }

        public Task<EmployeeDetailsDto> SaveAsync(SaveEmployeeCommand command, CancellationToken cancellationToken)
        {
            var employeeId = command.EmployeeId ?? Guid.NewGuid();
            var employee = new EmployeeDetailsDto(
                employeeId,
                command.PersonnelNumber.Trim(),
                command.FirstName.Trim(),
                command.LastName.Trim(),
                command.BirthDate,
                command.EntryDate,
                command.ExitDate,
                command.IsActive,
                command.Street.Trim(),
                command.HouseNumber?.Trim(),
                command.AddressLine2?.Trim(),
                command.PostalCode.Trim(),
                command.City.Trim(),
                command.Country.Trim(),
                command.ResidenceCountry?.Trim(),
                command.Nationality?.Trim(),
                command.PermitCode?.Trim(),
                command.TaxStatus?.Trim(),
                command.IsSubjectToWithholdingTax,
                command.AhvNumber?.Trim(),
                command.Iban?.Trim(),
                command.PhoneNumber?.Trim(),
                command.Email?.Trim(),
                command.DepartmentOptionId,
                command.DepartmentOptionId.HasValue ? "Sicherheit" : null,
                command.EmploymentCategoryOptionId,
                command.EmploymentCategoryOptionId.HasValue ? "A" : null,
                command.EmploymentLocationOptionId,
                command.EmploymentLocationOptionId.HasValue ? "Schachenstr. 7, Emmenbruecke" : null,
                command.ContractValidFrom,
                command.ContractValidTo,
                command.HourlyRateChf,
                command.MonthlyBvgDeductionChf,
                command.SpecialSupplementRateChf);

            _employees[employeeId] = employee;
            return Task.FromResult(employee);
        }

        public static EmployeeDetailsDto CreateDetails(string personnelNumber, string firstName, string lastName, string city)
        {
            return new EmployeeDetailsDto(
                Guid.NewGuid(),
                personnelNumber,
                firstName,
                lastName,
                new DateOnly(1990, 1, 1),
                new DateOnly(2025, 1, 1),
                null,
                true,
                "Beispielstrasse",
                "1",
                null,
                "8000",
                city,
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                "756.0000.0000.00",
                "CH9300762011623852957",
                "+41 79 000 00 00",
                $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.ch",
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Sicherheit",
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "A",
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Schachenstr. 7, Emmenbruecke",
                new DateOnly(2025, 1, 1),
                null,
                32.5m,
                280m,
                3.00m);
        }

        public static EmployeeDetailsDto CreateInactiveDetails(string personnelNumber, string firstName, string lastName, string city)
        {
            return new EmployeeDetailsDto(
                Guid.NewGuid(),
                personnelNumber,
                firstName,
                lastName,
                new DateOnly(1990, 1, 1),
                new DateOnly(2025, 1, 1),
                new DateOnly(2026, 3, 31),
                false,
                "Beispielstrasse",
                "1",
                null,
                "8000",
                city,
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                "756.0000.0000.00",
                "CH9300762011623852957",
                "+41 79 000 00 00",
                $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.ch",
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Sicherheit",
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "A",
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Schachenstr. 7, Emmenbruecke",
                new DateOnly(2025, 1, 1),
                null,
                32.5m,
                280m,
                3.00m);
        }

        private static EmployeeListItemDto ToListItem(EmployeeDetailsDto employee)
        {
            return new EmployeeListItemDto(
                employee.EmployeeId,
                employee.PersonnelNumber,
                $"{employee.FirstName} {employee.LastName}",
                employee.IsActive,
                employee.City,
                employee.Country,
                employee.Email,
                employee.DepartmentName,
                employee.EmploymentCategoryName,
                employee.EmploymentLocationName,
                employee.HourlyRateChf,
                employee.MonthlyBvgDeductionChf,
                employee.ContractValidFrom,
                employee.ContractValidTo);
        }
    }

    private sealed class InMemoryMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        private readonly Dictionary<(int Year, int Month), IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> _overviewRows = [];

        public void SetOverviewRows(int year, int month, IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto> rows)
        {
            _overviewRows[(year, month)] = rows;
        }

        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(new EmployeeMonthlyRecord(employeeId, year, month));
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<EmployeeMonthlyRecord?>(null);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<MonthlyRecordDetailsDto?>(null);
        }

        public Task<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> ListTimeCaptureOverviewAsync(int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(_overviewRows.TryGetValue((year, month), out var rows)
                ? rows
                : (IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>)Array.Empty<MonthlyTimeCaptureOverviewRowDto>());
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

    private sealed class InMemoryBackupRestoreService : IBackupRestoreService
    {
        public CreateBackupCommand? LastCreateCommand { get; private set; }
        public RestoreBackupCommand? LastRestoreCommand { get; private set; }

        public string GetDefaultBackupDirectory()
        {
            return "/tmp/payroll-backups";
        }

        public string CreateDefaultFileName(DateTimeOffset localTimestamp)
        {
            return $"backup_{localTimestamp:yyyy-MM-dd_HH-mm}";
        }

        public Task<BackupFileInfoDto> CreateBackupAsync(CreateBackupCommand command, CancellationToken cancellationToken = default)
        {
            LastCreateCommand = command;
            return Task.FromResult(new BackupFileInfoDto(
                Path.Combine(command.TargetDirectoryPath, command.FileName + ".payrollbackup.json"),
                command.ContentType,
                DateTimeOffset.UtcNow));
        }

        public Task<RestoreResultDto> RestoreBackupAsync(RestoreBackupCommand command, CancellationToken cancellationToken = default)
        {
            LastRestoreCommand = command;
            return Task.FromResult(new RestoreResultDto(command.BackupFilePath, command.ContentType, DateTimeOffset.UtcNow));
        }
    }

    private sealed class InMemoryPayrollSettingsRepository : IPayrollSettingsRepository
    {
        private PayrollSettingsDto _settings = new(
            "Blesinger Sicherheits Dienste GmbH\nPostfach 28\n6314 Unteraegeri",
            "Segoe UI",
            13m,
            "#FF1A2530",
            "#FF5F6B7A",
            "#FFF5F7FA",
            "#FF14324A",
            "PA",
            string.Empty,
            "Helvetica",
            9m,
            "#FF000000",
            "#FF4B5563",
            "#FFFFFF00",
            "PA",
            string.Empty,
            "BANNER|Lohnblatt|{{Monat}}",
            global::Payroll.Domain.Settings.PayrollSettings.DefaultDecimalSeparator,
            0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 0m, 0m, 0m,
            PayrollPreviewHelpCatalog.GetDefaultOptions(),
            [new SettingOptionDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Sicherheit"), new SettingOptionDto(Guid.Parse("11111111-1111-1111-1111-111111111112"), "Buero")],
            [new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "A"), new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222223"), "B"), new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222224"), "C")],
            [new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Schachenstr. 7, Emmenbruecke"), new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333334"), "Weinbergstrasse 8, Baar"), new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333335"), "Rainstrasse 37, Unteraegeri")]);

        public PayrollSettingsDto Current => _settings;

        public Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_settings);
        }

        public Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new WorkTimeSupplementSettings(
                _settings.NightSupplementRate,
                _settings.SundaySupplementRate,
                _settings.HolidaySupplementRate));
        }

        public Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
        {
            _settings = new PayrollSettingsDto(
                command.CompanyAddress,
                command.AppFontFamily,
                command.AppFontSize,
                command.AppTextColorHex,
                command.AppMutedTextColorHex,
                command.AppBackgroundColorHex,
                command.AppAccentColorHex,
                command.AppLogoText,
                command.AppLogoPath,
                command.PrintFontFamily,
                command.PrintFontSize,
                command.PrintTextColorHex,
                command.PrintMutedTextColorHex,
                command.PrintAccentColorHex,
                command.PrintLogoText,
                command.PrintLogoPath,
                command.PrintTemplate,
                command.DecimalSeparator,
                command.NightSupplementRate,
                command.SundaySupplementRate,
                command.HolidaySupplementRate,
                command.AhvIvEoRate,
                command.AlvRate,
                command.SicknessAccidentInsuranceRate,
                command.TrainingAndHolidayRate,
                command.VacationCompensationRate,
                command.VacationCompensationRateAge50Plus,
                command.VehiclePauschalzone1RateChf,
                command.VehiclePauschalzone2RateChf,
                command.VehicleRegiezone1RateChf,
                command.PayrollPreviewHelpOptions,
                command.Departments,
                command.EmploymentCategories,
                command.EmploymentLocations);
            return Task.FromResult(_settings);
        }
    }

    private sealed class TestPdfExportService : IPdfExportService
    {
        public Task<string> ExportPayrollStatementAsync(PayrollStatementPdfDocument document, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("/tmp/Lohnblatt_Test.pdf");
        }
    }
}
