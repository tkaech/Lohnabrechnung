using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.Imports;
using Payroll.Application.Layout;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.Imports;
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
    public async Task SavingCurrentContract_UpdatesLoadedCurrentContractOnly()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);
        viewModel.HourlyRateChf = "35.00";

        viewModel.SaveCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender 1000 gespeichert.");

        Assert.Equal(employee.CurrentContractId, repository.LastSavedCommand?.EditingContractId);
        Assert.False(viewModel.ShowContractVersionDialog);
    }

    [Fact]
    public async Task NewContractVersionDialog_CreatesNewVersionWithoutOverwritingCurrentOne()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.EditEmployeeCommand.Execute(null);
        viewModel.OpenNewContractVersionDialogCommand.Execute(null);
        viewModel.NewContractVersionValidFrom = new DateTimeOffset(new DateTime(2025, 2, 1));
        viewModel.ConfirmContractVersionDialogCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender 1000 gespeichert.");
        var savedEmployee = await repository.GetByIdAsync(employee.EmployeeId, CancellationToken.None);

        Assert.Null(repository.LastSavedCommand?.EditingContractId);
        Assert.Equal(new DateOnly(2025, 2, 1), repository.LastSavedCommand?.ContractValidFrom);
        Assert.Equal(2, savedEmployee?.ContractHistory.Count);
        Assert.Contains(savedEmployee!.ContractHistory, item => item.ContractId == employee.CurrentContractId);
    }

    [Fact]
    public async Task ContractHistoryDialog_OpensSeparatelyFromMainEditFlow()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");

        viewModel.OpenContractVersionDialogCommand.Execute(null);

        Assert.True(viewModel.ShowContractVersionDialog);
        Assert.False(viewModel.ShowContractVersionCreateSection);
    }

    [Fact]
    public async Task NewEmployee_DefaultsToHourlyWageType_AndPersistsSelectedValue()
    {
        var repository = new TestEmployeeRepository();
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();

        viewModel.NewEmployeeCommand.Execute(null);

        Assert.Equal("Stundenlohn", viewModel.SelectedWageType);

        viewModel.PersonnelNumber = "3000";
        viewModel.FirstName = "Mila";
        viewModel.LastName = "Test";
        viewModel.EntryDate = new DateTimeOffset(new DateTime(2026, 1, 1));
        viewModel.ContractValidFrom = new DateTimeOffset(new DateTime(2026, 1, 1));
        viewModel.Street = "Testweg";
        viewModel.PostalCode = "6000";
        viewModel.City = "Luzern";
        viewModel.Country = "Schweiz";
        viewModel.SelectedWageType = "Monatslohn";

        viewModel.SaveCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.StatusMessage == "Mitarbeitender 3000 gespeichert.");

        Assert.Equal("Monatslohn", viewModel.SelectedWageType);
        Assert.Equal(EmployeeWageType.Monthly, repository.LastSavedCommand?.WageType);
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
        var importService = CreateImportService(employeeRepository);
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            importService,
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            CreateLayoutParameterFilesViewModel(),
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
        var importService = CreateImportService(employeeRepository);
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            importService,
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            CreateLayoutParameterFilesViewModel(),
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
        Assert.Single(settingsRepository.Current.GeneralSettingsHistory!);
        Assert.Single(settingsRepository.Current.HourlySettingsHistory!);
    }

    [Fact]
    public async Task GeneralSettingsHistoryDialog_OpensSeparatelyFromNormalSave()
    {
        var repository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = CreateViewModel(repository, settingsRepository);

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.OpenGeneralSettingsVersionDialogCommand.Execute(null);

        Assert.True(viewModel.ShowSettingsVersionDialog);
        Assert.False(viewModel.ShowSettingsVersionCreateSection);
        Assert.Single(viewModel.SettingsVersionHistory);
    }

    [Fact]
    public async Task NewGeneralSettingsVersion_CreatesSeparateHistoryEntry()
    {
        var repository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = CreateViewModel(repository, settingsRepository);

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.SettingsAhvIvEoRate = "5,4";
        viewModel.OpenNewGeneralSettingsVersionDialogCommand.Execute(null);
        viewModel.NewSettingsVersionValidFrom = new DateTimeOffset(new DateTime(2026, 2, 1));
        viewModel.ConfirmSettingsVersionDialogCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.StatusMessage == "Einstellungen gespeichert.");

        Assert.Equal(2, settingsRepository.Current.GeneralSettingsHistory!.Count);
        Assert.Equal(0.054m, settingsRepository.Current.AhvIvEoRate);
    }

    [Fact]
    public async Task NewHourlySettingsVersion_DoesNotOverwriteExistingVersion()
    {
        var repository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var viewModel = CreateViewModel(repository, settingsRepository);

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);
        var previousCurrentId = settingsRepository.Current.CurrentHourlySettingsVersionId;
        viewModel.SettingsNightSupplementRate = "35";
        viewModel.OpenNewHourlySettingsVersionDialogCommand.Execute(null);
        viewModel.NewSettingsVersionValidFrom = new DateTimeOffset(new DateTime(2026, 3, 1));
        viewModel.ConfirmSettingsVersionDialogCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.StatusMessage == "Einstellungen gespeichert.");

        Assert.NotEqual(previousCurrentId, settingsRepository.Current.CurrentHourlySettingsVersionId);
        Assert.Equal(2, settingsRepository.Current.HourlySettingsHistory!.Count);
        Assert.Contains(settingsRepository.Current.HourlySettingsHistory, item => item.VersionId == previousCurrentId && item.ValidTo == new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task SettingsWorkspace_CreatesBackupAndUsesRestorePath()
    {
        var employeeRepository = new TestEmployeeRepository();
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var backupRestoreService = new InMemoryBackupRestoreService();
        var importService = CreateImportService(employeeRepository);
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            importService,
            backupRestoreService,
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            new MonthlyRecordService(new InMemoryMonthlyRecordRepository()),
            new MonthlyRecordViewModel(new MonthlyRecordService(new InMemoryMonthlyRecordRepository())),
            CreateLayoutParameterFilesViewModel(),
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

    [Fact]
    public async Task SelectingSavedPersonImportConfiguration_LoadsItImmediately()
    {
        var employeeRepository = new TestEmployeeRepository();
        var importRepository = new InMemoryImportMappingConfigurationRepository();
        await importRepository.SaveAsync(
            new SaveImportConfigurationCommand(
                null,
                ImportConfigurationType.PersonData,
                "Personenstamm 2",
                ";",
                true,
                "\"",
                [
                    new ImportFieldMappingDto("personnel_number", "Personalnummer", false),
                    new ImportFieldMappingDto("first_name", "Vorname", true)
                ]),
            CancellationToken.None);

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = new ImportService(
            importRepository,
            new InMemoryCsvImportFileReader(),
            employeeRepository,
            new InMemoryMonthlyRecordRepository(),
            new InMemoryImportExecutionStatusRepository());
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            importService,
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            CreateLayoutParameterFilesViewModel(),
            "Test");

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);

        var savedItem = viewModel.PersonImportConfigurations.Single(item => item.Name == "Personenstamm 2");
        viewModel.PersonImportConfigurationName = string.Empty;
        viewModel.SelectedPersonImportConfiguration = savedItem;

        await WaitUntilAsync(() => viewModel.PersonImportConfigurationName == "Personenstamm 2");

        Assert.Equal("Personenstamm 2", viewModel.PersonImportConfigurationName);
        Assert.Contains("geladen", viewModel.PersonImportStatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Personalnummer", viewModel.PersonImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn);
    }

    [Fact]
    public async Task TimeImport_RequiresMonthSelectionBeforeImportIsEnabled()
    {
        var repository = new TestEmployeeRepository();
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);

        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";
        foreach (var row in viewModel.TimeImportFieldMappings)
        {
            if (row.FieldKey == "personnel_number")
            {
                row.ApplyAvailableCsvColumns(["Personalnummer", "Stunden"]);
                row.SelectedCsvColumn = "Personalnummer";
            }
            else if (row.FieldKey == "hours_worked")
            {
                row.ApplyAvailableCsvColumns(["Personalnummer", "Stunden"]);
                row.SelectedCsvColumn = "Stunden";
            }
        }

        viewModel.TimeImportMonth = null;

        Assert.False(viewModel.CanImportTimeData);
    }

    [Fact]
    public async Task SelectingSavedTimeImportConfiguration_LoadsItImmediately()
    {
        var employeeRepository = new TestEmployeeRepository();
        var importRepository = new InMemoryImportMappingConfigurationRepository();
        await importRepository.SaveAsync(
            new SaveImportConfigurationCommand(
                null,
                ImportConfigurationType.TimeData,
                "Stunden CSV",
                ";",
                true,
                "\"",
                [
                    new ImportFieldMappingDto("personnel_number", "Personalnummer", false),
                    new ImportFieldMappingDto("hours_worked", "Stunden", false)
                ]),
            CancellationToken.None);

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = new ImportService(
            importRepository,
            new InMemoryCsvImportFileReader(),
            employeeRepository,
            new InMemoryMonthlyRecordRepository(),
            new InMemoryImportExecutionStatusRepository());
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            importService,
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(employeeRepository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            CreateLayoutParameterFilesViewModel(),
            "Test");

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);

        var savedItem = viewModel.TimeImportConfigurations.Single(item => item.Name == "Stunden CSV");
        viewModel.TimeImportConfigurationName = string.Empty;
        viewModel.SelectedTimeImportConfiguration = savedItem;

        await WaitUntilAsync(() => viewModel.TimeImportConfigurationName == "Stunden CSV");

        Assert.Equal("Stunden CSV", viewModel.TimeImportConfigurationName);
        Assert.Contains("geladen", viewModel.TimeImportStatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Personalnummer", viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn);
        Assert.Equal("Stunden", viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn);
    }

    private static MainWindowViewModel CreateViewModel(TestEmployeeRepository repository, InMemoryPayrollSettingsRepository? settingsRepository = null)
    {
        settingsRepository ??= new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(repository);

        return new MainWindowViewModel(
            new EmployeeService(repository),
            importService,
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            new ReportingService(
                new EmployeeService(repository),
                monthlyRecordService,
                new PayrollSettingsService(settingsRepository),
                new TestPdfExportService()),
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            CreateLayoutParameterFilesViewModel(),
            "Test");
    }

    private static LayoutParameterFilesViewModel CreateLayoutParameterFilesViewModel()
    {
        return new LayoutParameterFilesViewModel(new LayoutParameterFileService(new InMemoryLayoutParameterFileRepository()));
    }

    private static ImportService CreateImportService(IEmployeeRepository employeeRepository)
    {
        return new ImportService(
            new InMemoryImportMappingConfigurationRepository(),
            new InMemoryCsvImportFileReader(),
            employeeRepository,
            new InMemoryMonthlyRecordRepository(),
            new InMemoryImportExecutionStatusRepository());
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
        public SaveEmployeeCommand? LastSavedCommand { get; private set; }

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

        public Task<EmployeeDetailsDto?> GetByPersonnelNumberAsync(string personnelNumber, CancellationToken cancellationToken)
        {
            var trimmedPersonnelNumber = personnelNumber.Trim();
            var employee = _employees.Values.SingleOrDefault(item => item.PersonnelNumber == trimmedPersonnelNumber);
            return Task.FromResult(employee);
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
            LastSavedCommand = command;
            var employeeId = command.EmployeeId ?? Guid.NewGuid();
            var existingEmployee = _employees.GetValueOrDefault(employeeId);
            var history = existingEmployee?.ContractHistory.ToList()
                ?? [];

            if (command.EditingContractId.HasValue)
            {
                var existingContract = history.SingleOrDefault(item => item.ContractId == command.EditingContractId.Value)
                    ?? new EmploymentContractVersionDto(
                        command.EditingContractId.Value,
                        command.ContractValidFrom,
                        command.ContractValidTo,
                        command.HourlyRateChf,
                        command.MonthlyBvgDeductionChf,
                        command.SpecialSupplementRateChf,
                        true);

                history.RemoveAll(item => item.ContractId == existingContract.ContractId);
                history.Add(existingContract with
                {
                    ValidFrom = command.ContractValidFrom,
                    ValidTo = command.ContractValidTo,
                    HourlyRateChf = command.HourlyRateChf,
                    MonthlyBvgDeductionChf = command.MonthlyBvgDeductionChf,
                    SpecialSupplementRateChf = command.SpecialSupplementRateChf
                });
            }
            else
            {
                var newContractId = Guid.NewGuid();
                var previousCurrent = history.FirstOrDefault(item => item.IsCurrent);
                if (previousCurrent is not null)
                {
                    history.RemoveAll(item => item.ContractId == previousCurrent.ContractId);
                    history.Add(previousCurrent with
                    {
                        ValidTo = command.ContractValidFrom.AddDays(-1),
                        IsCurrent = false
                    });
                }

                history.Add(new EmploymentContractVersionDto(
                    newContractId,
                    command.ContractValidFrom,
                    command.ContractValidTo,
                    command.HourlyRateChf,
                    command.MonthlyBvgDeductionChf,
                    command.SpecialSupplementRateChf,
                    true));
            }

            var currentContract = history
                .Where(item => item.IsCurrent)
                .OrderByDescending(item => item.ValidFrom)
                .FirstOrDefault()
                ?? history.OrderByDescending(item => item.ValidFrom).First();

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
                command.WageType,
                currentContract.ContractId,
                currentContract.ValidFrom,
                currentContract.ValidTo,
                currentContract.HourlyRateChf,
                currentContract.MonthlyBvgDeductionChf,
                currentContract.SpecialSupplementRateChf,
                history.ToArray());

            _employees[employeeId] = employee;
            return Task.FromResult(employee);
        }

        public static EmployeeDetailsDto CreateDetails(string personnelNumber, string firstName, string lastName, string city)
        {
            var contractId = Guid.NewGuid();
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
                EmployeeWageType.Hourly,
                contractId,
                new DateOnly(2025, 1, 1),
                null,
                32.5m,
                280m,
                3.00m,
                [
                    new EmploymentContractVersionDto(
                        contractId,
                        new DateOnly(2025, 1, 1),
                        null,
                        32.5m,
                        280m,
                        3.00m,
                        true)
                ]);
        }

        public static EmployeeDetailsDto CreateInactiveDetails(string personnelNumber, string firstName, string lastName, string city)
        {
            var contractId = Guid.NewGuid();
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
                EmployeeWageType.Hourly,
                contractId,
                new DateOnly(2025, 1, 1),
                null,
                32.5m,
                280m,
                3.00m,
                [
                    new EmploymentContractVersionDto(
                        contractId,
                        new DateOnly(2025, 1, 1),
                        null,
                        32.5m,
                        280m,
                        3.00m,
                        true)
                ]);
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

    private sealed class InMemoryImportMappingConfigurationRepository : IImportMappingConfigurationRepository
    {
        private readonly Dictionary<Guid, ImportConfigurationDto> _configurations = [];

        public Task<IReadOnlyCollection<ImportConfigurationListItemDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
        {
            var items = _configurations.Values
                .Where(item => item.Type == type)
                .OrderBy(item => item.Name)
                .Select(item => new ImportConfigurationListItemDto(item.ConfigurationId, item.Name))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<ImportConfigurationListItemDto>>(items);
        }

        public Task<ImportConfigurationDto?> GetByIdAsync(Guid configurationId, CancellationToken cancellationToken)
        {
            _configurations.TryGetValue(configurationId, out var configuration);
            return Task.FromResult(configuration);
        }

        public Task<ImportConfigurationDto> SaveAsync(SaveImportConfigurationCommand command, CancellationToken cancellationToken)
        {
            var configurationId = command.ConfigurationId ?? Guid.NewGuid();
            var configuration = new ImportConfigurationDto(
                configurationId,
                command.Type,
                command.Name,
                command.Delimiter,
                command.FieldsEnclosed,
                command.TextQualifier,
                command.Mappings);
            _configurations[configurationId] = configuration;
            return Task.FromResult(configuration);
        }
    }

    private sealed class InMemoryCsvImportFileReader : ICsvImportFileReader
    {
        public Task<CsvImportDocumentDto> ReadAsync(ReadCsvImportDocumentCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CsvImportDocumentDto(["Personalnummer"], []));
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

        public Task DeleteTimeEntriesForMonthAsync(int year, int month, CancellationToken cancellationToken)
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

    private sealed class InMemoryImportExecutionStatusRepository : IImportExecutionStatusRepository
    {
        public Task<bool> ExistsAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task MarkImportedAsync(ImportConfigurationType type, int year, int month, DateTimeOffset importedAtUtc, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ImportedMonthStatusDto>>([]);
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
        private PayrollSettingsDto _settings;

        public InMemoryPayrollSettingsRepository()
        {
            var generalId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
            var hourlyId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
            var monthlySalaryId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");

            _settings = new PayrollSettingsDto(
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
                global::Payroll.Domain.Settings.PayrollSettings.DefaultThousandsSeparator,
                global::Payroll.Domain.Settings.PayrollSettings.DefaultCurrencyCode,
                0.25m, 0.50m, 1.00m, 0.053m, 0.011m, 0.00821m, 0.00015m, 0.1064m, 0.1264m, 0m, 0m, 0m,
                PayrollPreviewHelpCatalog.GetDefaultOptions(),
                [new SettingOptionDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Sicherheit"), new SettingOptionDto(Guid.Parse("11111111-1111-1111-1111-111111111112"), "Buero")],
                [new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "A"), new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222223"), "B"), new SettingOptionDto(Guid.Parse("22222222-2222-2222-2222-222222222224"), "C")],
                [new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Schachenstr. 7, Emmenbruecke"), new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333334"), "Weinbergstrasse 8, Baar"), new SettingOptionDto(Guid.Parse("33333333-3333-3333-3333-333333333335"), "Rainstrasse 37, Unteraegeri")],
                generalId,
                new DateOnly(2026, 1, 1),
                null,
                [new PayrollGeneralSettingsVersionDto(generalId, new DateOnly(2026, 1, 1), null, 0.053m, 0.011m, 0.00821m, 0.00015m, true)],
                hourlyId,
                new DateOnly(2026, 1, 1),
                null,
                [new PayrollHourlySettingsVersionDto(hourlyId, new DateOnly(2026, 1, 1), null, 0.25m, 0.50m, 1.00m, 0.1064m, 0.1264m, 0m, 0m, 0m, true)],
                monthlySalaryId,
                new DateOnly(2026, 1, 1),
                null,
                [new PayrollMonthlySalarySettingsVersionDto(monthlySalaryId, new DateOnly(2026, 1, 1), null, true)]);
        }

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
            var generalHistory = (_settings.GeneralSettingsHistory ?? []).ToList();
            var hourlyHistory = (_settings.HourlySettingsHistory ?? []).ToList();
            var monthlySalaryHistory = (_settings.MonthlySalarySettingsHistory ?? []).ToList();

            var currentGeneralId = UpsertGeneralHistory(generalHistory, command);
            var currentHourlyId = UpsertHourlyHistory(hourlyHistory, command);
            var currentMonthlySalaryId = UpsertMonthlySalaryHistory(monthlySalaryHistory, command);

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
                command.ThousandsSeparator,
                command.CurrencyCode,
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
                command.EmploymentLocations,
                currentGeneralId,
                command.GeneralSettingsValidFrom,
                generalHistory.Single(item => item.VersionId == currentGeneralId).ValidTo,
                generalHistory.ToArray(),
                currentHourlyId,
                command.HourlySettingsValidFrom,
                hourlyHistory.Single(item => item.VersionId == currentHourlyId).ValidTo,
                hourlyHistory.ToArray(),
                currentMonthlySalaryId,
                command.MonthlySalarySettingsValidFrom,
                monthlySalaryHistory.Single(item => item.VersionId == currentMonthlySalaryId).ValidTo,
                monthlySalaryHistory.ToArray());
            return Task.FromResult(_settings);
        }

        private static Guid UpsertGeneralHistory(List<PayrollGeneralSettingsVersionDto> history, SavePayrollSettingsCommand command)
        {
            if (command.EditingGeneralSettingsVersionId.HasValue)
            {
                var current = history.Single(item => item.VersionId == command.EditingGeneralSettingsVersionId.Value);
                history.Remove(current);
                history.Add(current with
                {
                    ValidFrom = command.GeneralSettingsValidFrom,
                    ValidTo = command.GeneralSettingsValidTo,
                    AhvIvEoRate = command.AhvIvEoRate,
                    AlvRate = command.AlvRate,
                    SicknessAccidentInsuranceRate = command.SicknessAccidentInsuranceRate,
                    TrainingAndHolidayRate = command.TrainingAndHolidayRate,
                    IsCurrent = true
                });

                return current.VersionId;
            }

            var newId = Guid.NewGuid();
            var previous = history.Single(item => item.IsCurrent);
            history.Remove(previous);
            history.Add(previous with { ValidTo = command.GeneralSettingsValidFrom.AddDays(-1), IsCurrent = false });
            history.Add(new PayrollGeneralSettingsVersionDto(newId, command.GeneralSettingsValidFrom, command.GeneralSettingsValidTo, command.AhvIvEoRate, command.AlvRate, command.SicknessAccidentInsuranceRate, command.TrainingAndHolidayRate, true));
            return newId;
        }

        private static Guid UpsertHourlyHistory(List<PayrollHourlySettingsVersionDto> history, SavePayrollSettingsCommand command)
        {
            if (command.EditingHourlySettingsVersionId.HasValue)
            {
                var current = history.Single(item => item.VersionId == command.EditingHourlySettingsVersionId.Value);
                history.Remove(current);
                history.Add(current with
                {
                    ValidFrom = command.HourlySettingsValidFrom,
                    ValidTo = command.HourlySettingsValidTo,
                    NightSupplementRate = command.NightSupplementRate,
                    SundaySupplementRate = command.SundaySupplementRate,
                    HolidaySupplementRate = command.HolidaySupplementRate,
                    VacationCompensationRate = command.VacationCompensationRate,
                    VacationCompensationRateAge50Plus = command.VacationCompensationRateAge50Plus,
                    VehiclePauschalzone1RateChf = command.VehiclePauschalzone1RateChf,
                    VehiclePauschalzone2RateChf = command.VehiclePauschalzone2RateChf,
                    VehicleRegiezone1RateChf = command.VehicleRegiezone1RateChf,
                    IsCurrent = true
                });

                return current.VersionId;
            }

            var newId = Guid.NewGuid();
            var previous = history.Single(item => item.IsCurrent);
            history.Remove(previous);
            history.Add(previous with { ValidTo = command.HourlySettingsValidFrom.AddDays(-1), IsCurrent = false });
            history.Add(new PayrollHourlySettingsVersionDto(newId, command.HourlySettingsValidFrom, command.HourlySettingsValidTo, command.NightSupplementRate, command.SundaySupplementRate, command.HolidaySupplementRate, command.VacationCompensationRate, command.VacationCompensationRateAge50Plus, command.VehiclePauschalzone1RateChf, command.VehiclePauschalzone2RateChf, command.VehicleRegiezone1RateChf, true));
            return newId;
        }

        private static Guid UpsertMonthlySalaryHistory(List<PayrollMonthlySalarySettingsVersionDto> history, SavePayrollSettingsCommand command)
        {
            if (command.EditingMonthlySalarySettingsVersionId.HasValue)
            {
                var current = history.Single(item => item.VersionId == command.EditingMonthlySalarySettingsVersionId.Value);
                history.Remove(current);
                history.Add(current with
                {
                    ValidFrom = command.MonthlySalarySettingsValidFrom,
                    ValidTo = command.MonthlySalarySettingsValidTo,
                    IsCurrent = true
                });

                return current.VersionId;
            }

            var newId = Guid.NewGuid();
            var previous = history.Single(item => item.IsCurrent);
            history.Remove(previous);
            history.Add(previous with { ValidTo = command.MonthlySalarySettingsValidFrom.AddDays(-1), IsCurrent = false });
            history.Add(new PayrollMonthlySalarySettingsVersionDto(newId, command.MonthlySalarySettingsValidFrom, command.MonthlySalarySettingsValidTo, true));
            return newId;
        }
    }

    private sealed class TestPdfExportService : IPdfExportService
    {
        public Task<string> ExportPayrollStatementAsync(PayrollStatementPdfDocument document, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("/tmp/Lohnblatt_Test.pdf");
        }
    }

    private sealed class InMemoryLayoutParameterFileRepository : ILayoutParameterFileRepository
    {
        public Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<LayoutParameterFileSummaryDto>>(
            [
                new LayoutParameterFileSummaryDto("design-system", "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml", DateTimeOffset.UtcNow)
            ]);
        }

        public Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LayoutParameterFileDocumentDto(
                key,
                "Design System",
                "src/Payroll.Desktop/Styles/DesignSystem.axaml",
                "<Styles />",
                []));
        }

        public Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LayoutParameterFileDocumentDto(
                command.Key,
                "Design System",
                "src/Payroll.Desktop/Styles/DesignSystem.axaml",
                command.Content,
                []));
        }

        public Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LayoutParameterFileDocumentDto(
                command.Key,
                "Design System",
                "src/Payroll.Desktop/Styles/DesignSystem.axaml",
                "<Styles />",
                []));
        }
    }
}
