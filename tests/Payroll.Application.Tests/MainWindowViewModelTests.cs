using Payroll.Application.BackupRestore;
using Payroll.Application.AnnualSalary;
using Payroll.Application.Employees;
using Payroll.Application.Imports;
using Payroll.Application.Layout;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Payroll;
using Payroll.Application.Reporting;
using Payroll.Application.SalaryCertificate;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.Imports;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;

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
    public void WorkspaceDefaultsToTimeAndExpenses_AndCanSwitchToPayrollRunsAnnualSalaryReportingEmployeesSettingsAndHelp()
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

        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);

        Assert.True(viewModel.IsAnnualSalaryWorkspace);
        Assert.True(viewModel.ShowAnnualSalaryWorkspace);
        Assert.False(viewModel.IsPayrollRunsWorkspace);
        Assert.False(viewModel.ShowPayrollRunsWorkspace);
        Assert.False(viewModel.CanFinalizePayrollMonth);
        Assert.False(viewModel.CanCreatePayrollPdf);
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
    public async Task PayrollRunsWorkspace_OpenStatusRaisesActionStateAfterEmployeeChange()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var repository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var viewModel = CreateViewModel(
            repository,
            payrollRunService: new PayrollRunService(new OpenPayrollRunRepository()));
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.ShowPayrollRunsCommand.Execute(null);
        changedProperties.Clear();

        viewModel.SelectedEmployee = viewModel.Employees.Single(item => item.EmployeeId == secondEmployee.EmployeeId);
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1001");

        Assert.Equal("offen", viewModel.PayrollRunStatusDisplay);
        Assert.Equal(DateTime.Today, viewModel.PayrollPaymentDate?.Date);
        Assert.True(viewModel.CanFinalizePayrollMonth);
        Assert.True(viewModel.CanExecutePayrollMonthAction);
        Assert.Equal("Monat abschliessen", viewModel.PayrollMonthActionLabel);
        Assert.Contains(nameof(MainWindowViewModel.CanExecutePayrollMonthAction), changedProperties);
    }

    [Fact]
    public async Task SalaryCertificateCommand_IsOnlyEnabledWithLoadedFinalizedAnnualSalaryData()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var exportService = CreateSalaryCertificatePdfExportService();
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        Assert.True(viewModel.CanCreateSalaryCertificatePdf);
        Assert.True(viewModel.CreateSalaryCertificatePdfCommand.CanExecute(null));

        viewModel.AnnualSalaryYear = "2025";

        Assert.False(viewModel.CanCreateSalaryCertificatePdf);
        Assert.False(viewModel.CreateSalaryCertificatePdfCommand.CanExecute(null));
    }

    [Fact]
    public async Task CreateSalaryCertificatePdfAsync_CallsExportServiceWithEmployeeYearAndOutputPath()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var writer = new CaptureSalaryCertificatePdfDocumentWriter();
        var exportService = CreateSalaryCertificatePdfExportService(writer: writer);
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await viewModel.CreateSalaryCertificatePdfAsync(outputPath);

        Assert.Equal(GetWorkspaceTemplatePath(), writer.LastTemplatePath);
        Assert.Equal(outputPath, writer.LastOutputPath);
        Assert.Contains(writer.LastFields!, field => field.PdfFieldName == "TextLinks_D" && field.Value == "2026");
        Assert.Equal($"Lohnausweis erstellt: {outputPath}", viewModel.StatusMessage);
    }

    [Fact]
    public async Task LoadAnnualSalaryAsync_ShowsLatestSalaryCertificateCreatedDate()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var recordRepository = new InMemorySalaryCertificateRecordRepository();
        recordRepository.Seed(new SalaryCertificateRecordDto(
            Guid.NewGuid(),
            employee.EmployeeId,
            2026,
            new DateTimeOffset(2026, 4, 27, 14, 30, 0, TimeSpan.Zero),
            "/tmp/existing.pdf",
            null));
        var exportService = CreateSalaryCertificatePdfExportService(recordRepository: recordRepository);
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        Assert.Equal("Lohnausweis erstellt am 27.04.2026", viewModel.SalaryCertificateCreatedDisplay);
    }

    [Fact]
    public async Task CreateSalaryCertificatePdfAsync_RefreshesSalaryCertificateCreatedDate()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var recordRepository = new InMemorySalaryCertificateRecordRepository();
        var exportService = CreateSalaryCertificatePdfExportService(recordRepository: recordRepository);
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        Assert.Equal("Noch kein Lohnausweis erstellt", viewModel.SalaryCertificateCreatedDisplay);

        await viewModel.CreateSalaryCertificatePdfAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"));

        Assert.Equal($"Lohnausweis erstellt am {DateTimeOffset.Now.ToLocalTime():dd.MM.yyyy}", viewModel.SalaryCertificateCreatedDisplay);
    }

    [Fact]
    public async Task CreateSalaryCertificatePdfAsync_ShowsErrorMessageWhenExportFails()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var exportService = CreateSalaryCertificatePdfExportService(writer: new ThrowingSalaryCertificatePdfDocumentWriter("kaputt"));
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        await viewModel.CreateSalaryCertificatePdfAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"));

        Assert.Equal("Lohnausweis fehlgeschlagen: kaputt", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CreateSalaryCertificatePdfAsync_BlocksWhenAhvNumberIsMissing()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern") with { AhvNumber = null };
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var writer = new CaptureSalaryCertificatePdfDocumentWriter();
        var exportService = CreateSalaryCertificatePdfExportService(writer: writer);
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        await viewModel.CreateSalaryCertificatePdfAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"));

        Assert.Equal("AHV-Nummer fehlt fuer Lohnausweis.", viewModel.StatusMessage);
        Assert.Null(writer.LastOutputPath);
    }

    [Fact]
    public async Task CreateSalaryCertificatePdfAsync_BlocksWhenBirthDateIsMissing()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern") with { BirthDate = null };
        var repository = new TestEmployeeRepository(employee);
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var writer = new CaptureSalaryCertificatePdfDocumentWriter();
        var exportService = CreateSalaryCertificatePdfExportService(writer: writer);
        var viewModel = CreateViewModel(repository, annualSalaryService: annualSalaryService, salaryCertificatePdfExportService: exportService);

        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PersonnelNumber == "1000");
        viewModel.AnnualSalaryYear = "2026";
        viewModel.MainNavigationItems.Single(item => item.Section == MainSection.AnnualSalary).ActivateCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage == "Jahreslohn 2026 geladen.");

        await viewModel.CreateSalaryCertificatePdfAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"));

        Assert.Equal("Geburtsdatum fehlt fuer Lohnausweis.", viewModel.StatusMessage);
        Assert.Null(writer.LastOutputPath);
    }

    [Fact]
    public async Task TimeAndExpenses_LockFollowsActivePayrollRunStatus()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");

        var openViewModel = CreateViewModel(
            new TestEmployeeRepository(employee),
            payrollRunService: new PayrollRunService(new OpenPayrollRunRepository()));
        openViewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        await openViewModel.InitializeAsync();
        await WaitUntilAsync(() => openViewModel.PersonnelNumber == "1000"
            && openViewModel.PayrollRunStatusDisplay == "offen");

        Assert.False(openViewModel.MonthlyRecord.IsLocked);
        Assert.True(openViewModel.MonthlyRecord.CanManageRecord);
        Assert.True(openViewModel.MonthlyRecord.NewTimeEntryCommand.CanExecute(null));

        var finalizedViewModel = CreateViewModel(
            new TestEmployeeRepository(employee),
            payrollRunService: new PayrollRunService(new OpenPayrollRunRepository(finalized: true)));
        finalizedViewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        await finalizedViewModel.InitializeAsync();
        await WaitUntilAsync(() => finalizedViewModel.PayrollRunStatusDisplay == "abgeschlossen");

        Assert.True(finalizedViewModel.MonthlyRecord.IsLocked);
        Assert.False(finalizedViewModel.MonthlyRecord.CanSaveTimeEntry);
        Assert.False(finalizedViewModel.MonthlyRecord.CanSaveExpenseEntry);

        var cancelledViewModel = CreateViewModel(
            new TestEmployeeRepository(employee),
            payrollRunService: new PayrollRunService(new OpenPayrollRunRepository(cancelled: true)));
        cancelledViewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        await cancelledViewModel.InitializeAsync();
        await WaitUntilAsync(() => cancelledViewModel.PayrollRunStatusDisplay == "storniert");

        Assert.False(cancelledViewModel.MonthlyRecord.IsLocked);
        Assert.True(cancelledViewModel.MonthlyRecord.CanManageRecord);
    }

    [Fact]
    public async Task TimeAndExpenses_MonthChangeRefreshesPayrollRunLock()
    {
        var employee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var repository = new OpenPayrollRunRepository(finalized: true, finalizedPeriodKey: "2026-03");
        var viewModel = CreateViewModel(
            new TestEmployeeRepository(employee),
            payrollRunService: new PayrollRunService(repository));

        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        await viewModel.InitializeAsync();
        await WaitUntilAsync(() => viewModel.PayrollRunStatusDisplay == "abgeschlossen");

        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        await WaitUntilAsync(() => viewModel.PayrollRunStatusDisplay == "offen");

        Assert.False(viewModel.MonthlyRecord.IsLocked);
        Assert.True(viewModel.MonthlyRecord.CanManageRecord);
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
    public async Task ReportingWorkspace_MonthChange_UsesCentralSelectedMonthAndReloadsRows()
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
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(Guid.NewGuid(), "1000", "Anna", "Aktiv", true, false, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0),
                new MonthlyTimeCaptureOverviewRowDto(Guid.NewGuid(), "1001", "Bruno", "Bereit", true, true, 9m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.ShowReportingCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.IsReportingWorkspace && viewModel.MonthCaptureOverviewRows.Count == 2);

        Assert.Equal("04/2026", viewModel.MonthCaptureMonthLabel);
        Assert.Equal("1000", viewModel.MonthCaptureOverviewRows[0].PersonnelNumber);

        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        await WaitUntilAsync(() =>
            viewModel.MonthCaptureMonthLabel == "05/2026"
            && viewModel.MonthCaptureOverviewRows.Count == 2
            && viewModel.MonthCaptureOverviewRows[0].PersonnelNumber == "1000"
            && viewModel.MonthCaptureOverviewRows[1].PersonnelNumber == "1001");

        Assert.Equal("05/2026", viewModel.MonthCaptureMonthLabel);
        Assert.Equal(0m, viewModel.MonthCaptureOverviewRows[0].HoursWorked);
        Assert.Equal(9m, viewModel.MonthCaptureOverviewRows[1].HoursWorked);
    }

    [Fact]
    public async Task ReportingTotals_LoadsFinalizedPayrollRunLinesAndReactsToMonthRange()
    {
        var employeeRepository = new TestEmployeeRepository(
            TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern"),
            TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich"));
        var payrollRunRepository = new ReportingTotalsPayrollRunRepository(
            CreateFinalizedPayrollRun("2026-01", Guid.NewGuid(), 100m, 20m, 5m),
            CreateFinalizedPayrollRun("2026-02", Guid.NewGuid(), 200m, 10m, 2m),
            CreateDraftPayrollRun("2026-03", Guid.NewGuid(), 999m));
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var reportingService = new ReportingService(
            new EmployeeService(employeeRepository),
            monthlyRecordService,
            new PayrollSettingsService(settingsRepository),
            new TestPdfExportService(),
            payrollRunRepository);
        var viewModel = new MainWindowViewModel(
            new EmployeeService(employeeRepository),
            CreateImportService(employeeRepository),
            new InMemoryBackupRestoreService(),
            new PayrollSettingsService(settingsRepository),
            reportingService,
            monthlyRecordService,
            new MonthlyRecordViewModel(monthlyRecordService),
            CreateLayoutParameterFilesViewModel(),
            "Test");

        await viewModel.InitializeAsync();
        viewModel.ShowReportingCommand.Execute(null);

        await WaitUntilAsync(() => viewModel.ReportingPayrollTotalsRows.Count > 0);

        Assert.Contains(viewModel.ReportingPayrollTotalsRows, line => line.Label == "Basislohn" && line.AmountChf == 300m);
        Assert.Contains(viewModel.ReportingPayrollTotalsRows, line => line.Label == "Spesen gemaess Nachweis" && line.AmountChf == 30m);
        Assert.Contains(viewModel.ReportingPayrollTotalsRows, line => line.Label == "Total Auszahlung" && line.AmountChf == 323m);

        viewModel.ReportingPayrollTotalsUseFullYear = false;
        viewModel.SelectedReportingPayrollTotalsFromMonth = "01";
        viewModel.SelectedReportingPayrollTotalsToMonth = "01";

        await WaitUntilAsync(() => viewModel.ReportingPayrollTotalsRows.Any(line => line.Label == "Basislohn" && line.AmountChf == 100m));

        Assert.DoesNotContain(viewModel.ReportingPayrollTotalsRows, line => line.Label == "ALV");
        Assert.Contains(viewModel.ReportingPayrollTotalsRows, line => line.Label == "Total Auszahlung" && line.AmountChf == 115m);
    }

    [Fact]
    public async Task PayrollRunsFilter_LohnlaufMoeglich_IgnoresEmptyMonthCaptures()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            4,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 12m, 1m, 0m, 0m, 2m, 0m, 0m, 2),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.ShowPayrollRunsCommand.Execute(null);
        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 1);

        Assert.Single(viewModel.Employees);
        Assert.Equal("1000", viewModel.Employees[0].PersonnelNumber);
    }

    [Fact]
    public async Task PayrollRunsFilter_LohnlaufMoeglich_ReactsToMonthChange()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            4,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 12m, 1m, 0m, 0m, 2m, 0m, 0m, 2),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, false, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0)
            ]);
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.ShowPayrollRunsCommand.Execute(null);
        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 1 && viewModel.Employees[0].PersonnelNumber == "1000");

        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        await WaitUntilAsync(() => viewModel.Employees.Count == 0);

        Assert.Empty(viewModel.Employees);
    }

    [Fact]
    public async Task PayrollRunsFilter_LohnlaufMoeglich_UsesCurrentSelectedMonthWhenFilterIsApplied()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            4,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 12m, 1m, 0m, 0m, 2m, 0m, 0m, 2),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, false, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0)
            ]);
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.ShowPayrollRunsCommand.Execute(null);
        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 0);

        Assert.Empty(viewModel.Employees);
    }

    [Fact]
    public async Task PayrollRunsFilter_LohnlaufMoeglich_ShowsEmployeesWithWorkedHours()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 9m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        await viewModel.InitializeAsync();
        viewModel.ShowPayrollRunsCommand.Execute(null);
        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 1 && viewModel.Employees[0].PersonnelNumber == "1001");

        Assert.Single(viewModel.Employees);
        Assert.Equal("1001", viewModel.Employees[0].PersonnelNumber);
    }

    [Fact]
    public async Task TimeAndExpensesFilter_LohnlaufMoeglich_ShowsOnlyEmployeesWithPayrollRelevantData()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 9m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsTimeAndExpensesWorkspace);

        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 1 && viewModel.Employees[0].PersonnelNumber == "1001");

        Assert.Single(viewModel.Employees);
        Assert.Equal("1001", viewModel.Employees[0].PersonnelNumber);
    }

    [Fact]
    public async Task EmployeeFilter_LohnlaufMoeglich_PersistsAcrossWorkspaceSwitches()
    {
        var firstEmployee = TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern");
        var secondEmployee = TestEmployeeRepository.CreateDetails("1001", "Bruno", "Bereit", "Zuerich");
        var employeeRepository = new TestEmployeeRepository(firstEmployee, secondEmployee);
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        monthlyRecordRepository.SetOverviewRows(
            2026,
            5,
            [
                new MonthlyTimeCaptureOverviewRowDto(firstEmployee.EmployeeId, "1000", "Anna", "Aktiv", true, true, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 1),
                new MonthlyTimeCaptureOverviewRowDto(secondEmployee.EmployeeId, "1001", "Bruno", "Bereit", true, true, 9m, 0m, 0m, 0m, 0m, 0m, 0m, 1)
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
        viewModel.MonthlyRecord.SelectedMonth = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

        await viewModel.InitializeAsync();
        viewModel.SelectedActivityFilter = "Lohnlauf moeglich";

        await WaitUntilAsync(() => viewModel.Employees.Count == 1 && viewModel.Employees[0].PersonnelNumber == "1001");

        viewModel.ShowPayrollRunsCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.IsPayrollRunsWorkspace);
        Assert.Single(viewModel.Employees);
        Assert.Equal("1001", viewModel.Employees[0].PersonnelNumber);

        viewModel.ShowTimeAndExpensesCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.IsTimeAndExpensesWorkspace);
        Assert.Single(viewModel.Employees);
        Assert.Equal("1001", viewModel.Employees[0].PersonnelNumber);
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
        var csvReader = new InMemoryCsvImportFileReader();
        csvReader.SetDocument(
            "/tmp/stunden.csv",
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                []));
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(repository, csvReader, new InMemoryMonthlyRecordRepository());
        var viewModel = new MainWindowViewModel(
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

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);

        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "Stunden";

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

    [Fact]
    public async Task TimeImport_LoadingSavedMappingWithLoadedFileEnablesImportWithoutFieldChange()
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

        var csvReader = new InMemoryCsvImportFileReader();
        csvReader.SetDocument(
            "/tmp/stunden.csv",
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                []));

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(
            employeeRepository,
            csvReader,
            new InMemoryMonthlyRecordRepository(),
            importMappingConfigurationRepository: importRepository);
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
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";

        var savedItem = viewModel.TimeImportConfigurations.Single(item => item.Name == "Stunden CSV");
        viewModel.SelectedTimeImportConfiguration = savedItem;

        await WaitUntilAsync(() => viewModel.TimeImportConfigurationName == "Stunden CSV" && viewModel.CanImportTimeData);

        Assert.True(viewModel.CanImportTimeData);
    }

    [Fact]
    public async Task TimeImport_BlockedReason_ShowsMissingHeadersUntilFileContextIsRead()
    {
        var repository = new TestEmployeeRepository();
        var viewModel = CreateViewModel(repository);

        await viewModel.InitializeAsync();
        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";

        Assert.False(viewModel.CanImportTimeData);
        Assert.Contains(viewModel.TimeImportBlockedReasons, reason => reason.Contains("Header", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TimeImport_MissingOptionalColumnDoesNotBlockAndShowsWarning()
    {
        var employeeRepository = new TestEmployeeRepository();
        var csvReader = new InMemoryCsvImportFileReader();
        csvReader.SetDocument(
            "/tmp/stunden.csv",
            new CsvImportDocumentDto(
                ["Personalnummer", "1001"],
                []));
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(employeeRepository, csvReader, new InMemoryMonthlyRecordRepository());
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
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "1001";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "sunday_hours").SelectedCsvColumn = "1005";

        await WaitUntilAsync(() => viewModel.CanImportTimeData);

        Assert.True(viewModel.CanImportTimeData);
        Assert.Contains(viewModel.TimeImportWarnings, warning => warning.Contains("1005", StringComparison.Ordinal));
        Assert.Empty(viewModel.TimeImportBlockedReasons);
    }

    [Fact]
    public async Task TimeImport_MissingRequiredColumnBlocksImport()
    {
        var employeeRepository = new TestEmployeeRepository();
        var csvReader = new InMemoryCsvImportFileReader();
        csvReader.SetDocument(
            "/tmp/stunden.csv",
            new CsvImportDocumentDto(
                ["Personalnummer"],
                []));
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(employeeRepository, csvReader, new InMemoryMonthlyRecordRepository());
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
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "1001";

        Assert.False(viewModel.CanImportTimeData);
        Assert.Contains(viewModel.TimeImportBlockedReasons, reason => reason.Contains("Arbeitsstunden", StringComparison.OrdinalIgnoreCase) || reason.Contains("1001", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TimeImport_AfterRestartWithSameFileAndMapping_KeepsImportedMonthBlockedButEnablesDifferentMonth()
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

        var csvReader = new InMemoryCsvImportFileReader();
        csvReader.SetDocument(
            "/tmp/stunden.csv",
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                []));

        var importStatusRepository = new InMemoryImportExecutionStatusRepository(
            new ImportedMonthStatusDto(2026, 1, DateTimeOffset.UtcNow));
        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(
            employeeRepository,
            csvReader,
            new InMemoryMonthlyRecordRepository(),
            importStatusRepository,
            importRepository);
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
        viewModel.TimeImportCsvFilePath = "/tmp/stunden.csv";
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 1, 1));
        viewModel.SelectedTimeImportConfiguration = viewModel.TimeImportConfigurations.Single(item => item.Name == "Stunden CSV");

        await WaitUntilAsync(() => viewModel.TimeImportConfigurationName == "Stunden CSV");
        Assert.False(viewModel.CanImportTimeData);
        Assert.Contains(viewModel.TimeImportBlockedReasons, reason => reason.Contains("bereits importiert", StringComparison.OrdinalIgnoreCase));

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 2, 1));

        await WaitUntilAsync(() => viewModel.CanImportTimeData);
        Assert.True(viewModel.CanImportTimeData);
        Assert.Empty(viewModel.TimeImportBlockedReasons);
    }

    [Fact]
    public async Task TimeImport_CanReuseSameMappingAcrossFilesAndMonthsWithoutOverwritingPreviousMonth()
    {
        var employeeRepository = new TestEmployeeRepository(TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern"));
        var csvReader = new InMemoryCsvImportFileReader();
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        var importStatusRepository = new InMemoryImportExecutionStatusRepository();
        var aprilPath = "/tmp/stunden-april.csv";
        var mayPath = "/tmp/stunden-mai.csv";
        csvReader.SetDocument(
            aprilPath,
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                [
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Personalnummer"] = "1000",
                        ["Stunden"] = "8"
                    }
                ]));
        csvReader.SetDocument(
            mayPath,
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                [
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Personalnummer"] = "1000",
                        ["Stunden"] = "9"
                    }
                ]));

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(employeeRepository, csvReader, monthlyRecordRepository, importStatusRepository);
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

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = aprilPath;
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "Stunden";

        Assert.True(viewModel.CanImportTimeData);

        var aprilPrepared = await viewModel.PrepareTimeImportPreviewAsync();
        Assert.True(aprilPrepared);
        await viewModel.ImportSelectedTimeDataAsync();
        Assert.False(viewModel.CanImportTimeData);

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 5, 1));
        viewModel.TimeImportCsvFilePath = mayPath;
        await viewModel.ReloadTimeImportCsvAsync();

        Assert.True(viewModel.CanImportTimeData);

        var mayPrepared = await viewModel.PrepareTimeImportPreviewAsync();
        Assert.True(mayPrepared);
        await viewModel.ImportSelectedTimeDataAsync();

        var employee = await employeeRepository.GetByPersonnelNumberAsync("1000", CancellationToken.None);
        var aprilRecord = await monthlyRecordRepository.GetOrCreateAsync(employee!.EmployeeId, 2026, 4, CancellationToken.None);
        var mayRecord = await monthlyRecordRepository.GetOrCreateAsync(employee.EmployeeId, 2026, 5, CancellationToken.None);

        Assert.Single(aprilRecord.TimeEntries);
        Assert.Single(mayRecord.TimeEntries);
        Assert.Equal(8m, aprilRecord.TimeEntries.Single().HoursWorked);
        Assert.Equal(9m, mayRecord.TimeEntries.Single().HoursWorked);
        Assert.True(await importStatusRepository.ExistsAsync(ImportConfigurationType.TimeData, 2026, 4, CancellationToken.None));
        Assert.True(await importStatusRepository.ExistsAsync(ImportConfigurationType.TimeData, 2026, 5, CancellationToken.None));
    }

    [Fact]
    public async Task TimeImport_DeletingImportedMonthEnablesImportAgain()
    {
        var employeeRepository = new TestEmployeeRepository(TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern"));
        var csvReader = new InMemoryCsvImportFileReader();
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        var importStatusRepository = new InMemoryImportExecutionStatusRepository();
        var path = "/tmp/stunden.csv";
        csvReader.SetDocument(
            path,
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                [
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Personalnummer"] = "1000",
                        ["Stunden"] = "8"
                    }
                ]));

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(employeeRepository, csvReader, monthlyRecordRepository, importStatusRepository);
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
        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        viewModel.TimeImportCsvFilePath = path;
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "Stunden";

        Assert.True(await viewModel.PrepareTimeImportPreviewAsync());
        await viewModel.ImportSelectedTimeDataAsync();
        Assert.False(viewModel.CanImportTimeData);

        viewModel.SelectedImportedTimeMonth = viewModel.ImportedTimeMonths.Single(item => item.Year == 2026 && item.Month == 4);
        await viewModel.DeleteImportedTimeMonthAsync();

        Assert.True(viewModel.CanImportTimeData);
    }

    [Fact]
    public async Task TimeImport_DuplicateDetectionUsesCurrentMonthPerEmployee()
    {
        var employeeRepository = new TestEmployeeRepository(TestEmployeeRepository.CreateDetails("1000", "Anna", "Aktiv", "Bern"));
        var csvReader = new InMemoryCsvImportFileReader();
        var monthlyRecordRepository = new InMemoryMonthlyRecordRepository();
        var importStatusRepository = new InMemoryImportExecutionStatusRepository();
        var path = "/tmp/stunden.csv";
        csvReader.SetDocument(
            path,
            new CsvImportDocumentDto(
                ["Personalnummer", "Stunden"],
                [
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Personalnummer"] = "1000",
                        ["Stunden"] = "8"
                    }
                ]));

        var settingsRepository = new InMemoryPayrollSettingsRepository();
        var monthlyRecordService = new MonthlyRecordService(new InMemoryMonthlyRecordRepository());
        var importService = CreateImportService(employeeRepository, csvReader, monthlyRecordRepository, importStatusRepository);
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
        viewModel.TimeImportCsvFilePath = path;
        await viewModel.ReloadTimeImportCsvAsync();
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "personnel_number").SelectedCsvColumn = "Personalnummer";
        viewModel.TimeImportFieldMappings.Single(item => item.FieldKey == "hours_worked").SelectedCsvColumn = "Stunden";

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        Assert.True(await viewModel.PrepareTimeImportPreviewAsync());
        await viewModel.ImportSelectedTimeDataAsync();

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 4, 1));
        Assert.True(await viewModel.PrepareTimeImportPreviewAsync());
        var aprilPreview = Assert.Single(viewModel.TimeImportPreviewItems);
        Assert.True(aprilPreview.MonthlyDataExists);
        Assert.Equal("Monatsdaten vorhanden", aprilPreview.Status);

        viewModel.TimeImportMonth = new DateTimeOffset(new DateTime(2026, 5, 1));
        Assert.True(await viewModel.PrepareTimeImportPreviewAsync());
        var mayPreview = Assert.Single(viewModel.TimeImportPreviewItems);
        Assert.False(mayPreview.MonthlyDataExists);
        Assert.Equal("Import bereit", mayPreview.Status);
    }

    private static MainWindowViewModel CreateViewModel(
        TestEmployeeRepository repository,
        InMemoryPayrollSettingsRepository? settingsRepository = null,
        PayrollRunService? payrollRunService = null,
        AnnualSalaryService? annualSalaryService = null,
        SalaryCertificatePdfExportService? salaryCertificatePdfExportService = null)
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
            "Test",
            annualSalaryService: annualSalaryService,
            payrollRunService: payrollRunService,
            salaryCertificatePdfExportService: salaryCertificatePdfExportService);
    }

    private static SalaryCertificatePdfExportService CreateSalaryCertificatePdfExportService(
        ISalaryCertificatePdfDocumentWriter? writer = null,
        ISalaryCertificateRecordRepository? recordRepository = null)
    {
        var annualSalaryService = new AnnualSalaryService(new StubAnnualSalaryRepository(hasFinalizedMonth: true));
        var salaryCertificateService = new SalaryCertificateService(annualSalaryService);
        var settingsService = new PayrollSettingsService(new InMemoryPayrollSettingsRepositoryForSalaryCertificate());

        return new SalaryCertificatePdfExportService(
            salaryCertificateService,
            settingsService,
            new StubSalaryCertificatePdfFormFieldReader(),
            writer ?? new CaptureSalaryCertificatePdfDocumentWriter(),
            recordRepository ?? new InMemorySalaryCertificateRecordRepository());
    }

    private static string GetWorkspaceTemplatePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var designSystemPath = Path.Combine(current.FullName, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml");
            if (File.Exists(designSystemPath))
            {
                return Path.Combine(current.FullName, PayrollSettings.DefaultSalaryCertificatePdfTemplatePath);
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Lohnausweis-Vorlage im Workspace wurde nicht gefunden.");
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

    private static ImportService CreateImportService(
        IEmployeeRepository employeeRepository,
        ICsvImportFileReader csvImportFileReader,
        IEmployeeMonthlyRecordRepository monthlyRecordRepository,
        IImportExecutionStatusRepository? importExecutionStatusRepository = null,
        IImportMappingConfigurationRepository? importMappingConfigurationRepository = null)
    {
        return new ImportService(
            importMappingConfigurationRepository ?? new InMemoryImportMappingConfigurationRepository(),
            csvImportFileReader,
            employeeRepository,
            monthlyRecordRepository,
            importExecutionStatusRepository ?? new InMemoryImportExecutionStatusRepository());
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

    private sealed class OpenPayrollRunRepository : IPayrollRunRepository
    {
        private readonly bool _finalized;
        private readonly bool _cancelled;
        private readonly string _finalizedPeriodKey;

        public OpenPayrollRunRepository(bool finalized = false, bool cancelled = false, string finalizedPeriodKey = "2026-03")
        {
            _finalized = finalized;
            _cancelled = cancelled;
            _finalizedPeriodKey = finalizedPeriodKey;
        }

        public Task<IReadOnlyCollection<PayrollRun>> ListFinalizedRunsAsync(int year, int fromMonth, int toMonth, CancellationToken cancellationToken)
        {
            if (_finalized && year == 2026 && fromMonth <= 3 && toMonth >= 3)
            {
                return Task.FromResult<IReadOnlyCollection<PayrollRun>>([CreateRun(_finalizedPeriodKey, PayrollRunStatus.Finalized)]);
            }

            return Task.FromResult<IReadOnlyCollection<PayrollRun>>([]);
        }

        public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(_finalized && periodKey == _finalizedPeriodKey
                ? CreateRun(periodKey, PayrollRunStatus.Finalized)
                : null);
        }

        public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodForUpdateAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken)
        {
            return GetFinalizedRunForEmployeePeriodAsync(employeeId, periodKey, cancellationToken);
        }

        public Task<PayrollRun?> GetLatestRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(_cancelled && periodKey == _finalizedPeriodKey
                ? CreateRun(periodKey, PayrollRunStatus.Cancelled)
                : null);
        }

        public Task<bool> HasCancelledRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(_cancelled && periodKey == _finalizedPeriodKey);
        }

        public Task<PayrollRunMonthlyInputDto?> LoadMonthlyInputAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult<PayrollRunMonthlyInputDto?>(null);
        }

        public Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PayrollSettings());
        }

        public Task<PayrollSettings> LoadPayrollSettingsForPeriodAsync(int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PayrollSettings());
        }

        public void Add(PayrollRun payrollRun)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static PayrollRun CreateRun(string periodKey, PayrollRunStatus status)
        {
            var run = new PayrollRun(periodKey, new DateOnly(2026, 4, 5));
            run.FinalizeRun();
            if (status == PayrollRunStatus.Cancelled)
            {
                run.Cancel(DateTimeOffset.UtcNow);
            }

            return run;
        }
    }

    private sealed class ReportingTotalsPayrollRunRepository : IPayrollRunRepository
    {
        private readonly IReadOnlyCollection<PayrollRun> _runs;

        public ReportingTotalsPayrollRunRepository(params PayrollRun[] runs)
        {
            _runs = runs;
        }

        public Task<IReadOnlyCollection<PayrollRun>> ListFinalizedRunsAsync(int year, int fromMonth, int toMonth, CancellationToken cancellationToken)
        {
            var filtered = _runs
                .Where(run => run.Status == PayrollRunStatus.Finalized)
                .Where(run => run.PeriodKey.StartsWith($"{year:D4}-", StringComparison.Ordinal))
                .Where(run => int.TryParse(run.PeriodKey.AsSpan(5, 2), out var month) && month >= fromMonth && month <= toMonth)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<PayrollRun>>(filtered);
        }

        public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken) => Task.FromResult<PayrollRun?>(null);
        public Task<PayrollRun?> GetFinalizedRunForEmployeePeriodForUpdateAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken) => Task.FromResult<PayrollRun?>(null);
        public Task<PayrollRun?> GetLatestRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken) => Task.FromResult<PayrollRun?>(null);
        public Task<bool> HasCancelledRunForEmployeePeriodAsync(Guid employeeId, string periodKey, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<PayrollRunMonthlyInputDto?> LoadMonthlyInputAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken) => Task.FromResult<PayrollRunMonthlyInputDto?>(null);
        public Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(new PayrollSettings());
        public Task<PayrollSettings> LoadPayrollSettingsForPeriodAsync(int year, int month, CancellationToken cancellationToken) => Task.FromResult(new PayrollSettings());
        public void Add(PayrollRun payrollRun) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static PayrollRun CreateFinalizedPayrollRun(string periodKey, Guid employeeId, decimal baseAmountChf, decimal expensesChf, decimal deductionAmountChf)
    {
        var run = new PayrollRun(periodKey, new DateOnly(2026, 4, 5));
        run.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.BaseHours, "BASE", "Basislohn", baseAmountChf));
        if (expensesChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.Expense, "EXPENSES", "Spesen gemaess Nachweis", expensesChf));
        }

        if (deductionAmountChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employeeId, PayrollLineType.SocialContribution, "AHV_IV_EO", "AHV/IV/EO", deductionAmountChf));
        }

        run.FinalizeRun();
        return run;
    }

    private static PayrollRun CreateDraftPayrollRun(string periodKey, Guid employeeId, decimal baseAmountChf)
    {
        var run = new PayrollRun(periodKey, new DateOnly(2026, 4, 5));
        run.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.BaseHours, "BASE", "Basislohn", baseAmountChf));
        return run;
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
        private readonly Dictionary<string, CsvImportDocumentDto> _documentsByPath = new(StringComparer.OrdinalIgnoreCase);

        public void SetDocument(string filePath, CsvImportDocumentDto document)
        {
            _documentsByPath[filePath] = document;
        }

        public Task<CsvImportDocumentDto> ReadAsync(ReadCsvImportDocumentCommand command, CancellationToken cancellationToken)
        {
            if (_documentsByPath.TryGetValue(command.FilePath, out var document))
            {
                return Task.FromResult(document);
            }

            return Task.FromResult(new CsvImportDocumentDto(["Personalnummer"], []));
        }
    }

    private sealed class InMemoryMonthlyRecordRepository : IEmployeeMonthlyRecordRepository
    {
        private readonly Dictionary<(int Year, int Month), IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> _overviewRows = [];
        private readonly Dictionary<(Guid EmployeeId, int Year, int Month), EmployeeMonthlyRecord> _records = [];

        public void SetOverviewRows(int year, int month, IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto> rows)
        {
            _overviewRows[(year, month)] = rows;
        }

        public Task<EmployeeMonthlyRecord> GetOrCreateAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            var key = (employeeId, year, month);
            if (!_records.TryGetValue(key, out var record))
            {
                record = new EmployeeMonthlyRecord(employeeId, year, month);
                _records[key] = record;
            }

            return Task.FromResult(record);
        }

        public Task<EmployeeMonthlyRecord?> GetByIdAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<EmployeeMonthlyRecord?>(null);
        }

        public Task<MonthlyRecordDetailsDto?> GetDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
        {
            return Task.FromResult<MonthlyRecordDetailsDto?>(null);
        }

        public Task<bool> HasTimeEntriesAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken)
        {
            var exists = _records.TryGetValue((employeeId, year, month), out var record)
                && record.TimeEntries.Count > 0;
            return Task.FromResult(exists);
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
            foreach (var record in _records.Values.Where(item => item.Year == year && item.Month == month).ToArray())
            {
                foreach (var timeEntry in record.TimeEntries.ToArray())
                {
                    record.RemoveTimeEntry(timeEntry.Id);
                }
            }

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
        private readonly Dictionary<(ImportConfigurationType Type, int Year, int Month), ImportedMonthStatusDto> _items = [];

        public InMemoryImportExecutionStatusRepository(params ImportedMonthStatusDto[] items)
        {
            foreach (var item in items)
            {
                _items[(ImportConfigurationType.TimeData, item.Year, item.Month)] = item;
            }
        }

        public Task<bool> ExistsAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            return Task.FromResult(_items.ContainsKey((type, year, month)));
        }

        public Task MarkImportedAsync(ImportConfigurationType type, int year, int month, DateTimeOffset importedAtUtc, CancellationToken cancellationToken)
        {
            _items[(type, year, month)] = new ImportedMonthStatusDto(year, month, importedAtUtc);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ImportConfigurationType type, int year, int month, CancellationToken cancellationToken)
        {
            _items.Remove((type, year, month));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ImportedMonthStatusDto>> ListAsync(ImportConfigurationType type, CancellationToken cancellationToken)
        {
            var items = _items
                .Where(item => item.Key.Type == type)
                .Select(item => item.Value)
                .OrderBy(item => item.Year)
                .ThenBy(item => item.Month)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<ImportedMonthStatusDto>>(items);
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
                global::Payroll.Domain.Settings.PayrollSettings.DefaultSalaryCertificatePdfTemplatePath,
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
                command.SalaryCertificatePdfTemplatePath,
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

    private sealed class StubAnnualSalaryRepository : IAnnualSalaryRepository
    {
        private readonly bool _hasFinalizedMonth;

        public StubAnnualSalaryRepository(bool hasFinalizedMonth)
        {
            _hasFinalizedMonth = hasFinalizedMonth;
        }

        public Task<AnnualSalaryOverviewDto> GetOverviewAsync(AnnualSalaryOverviewQuery query, CancellationToken cancellationToken)
        {
            var months = _hasFinalizedMonth
                ? new[]
                {
                    new AnnualSalaryMonthDto(1, "Januar", true, false, true, 1000m, 80m, 20m, 0m, 0m, 100m, 50m, 25m, 30m, 825m)
                }
                : Array.Empty<AnnualSalaryMonthDto>();

            return Task.FromResult(new AnnualSalaryOverviewDto(
                query.EmployeeId,
                "1000",
                "Anna",
                "Aktiv",
                "756.1234.5678.97",
                new DateOnly(1990, 1, 1),
                query.Year,
                months,
                new AnnualSalaryTotalsDto(1000m, 80m, 20m, 0m, 0m, 100m, 50m, 25m, 30m, 825m)));
        }
    }

    private sealed class StubSalaryCertificatePdfFormFieldReader : ISalaryCertificatePdfFormFieldReader
    {
        private static readonly IReadOnlyCollection<string> FieldNames = SalaryCertificatePdfFieldMappingCatalog.CreateInitialMapping()
            .Select(mapping => mapping.PdfFieldName)
            .ToArray();

        public Task<IReadOnlyCollection<string>> ReadFieldNamesAsync(string templatePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FieldNames);
        }
    }

    private sealed class CaptureSalaryCertificatePdfDocumentWriter : ISalaryCertificatePdfDocumentWriter
    {
        public string? LastTemplatePath { get; private set; }
        public string? LastOutputPath { get; private set; }
        public IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto>? LastFields { get; private set; }

        public Task WriteAsync(string templatePath, string outputPath, IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields, CancellationToken cancellationToken = default)
        {
            LastTemplatePath = templatePath;
            LastOutputPath = outputPath;
            LastFields = fields;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSalaryCertificatePdfDocumentWriter : ISalaryCertificatePdfDocumentWriter
    {
        private readonly string _message;

        public ThrowingSalaryCertificatePdfDocumentWriter(string message)
        {
            _message = message;
        }

        public Task WriteAsync(string templatePath, string outputPath, IReadOnlyCollection<SalaryCertificatePdfFieldWriteDto> fields, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(_message);
        }
    }

    private sealed class InMemorySalaryCertificateRecordRepository : ISalaryCertificateRecordRepository
    {
        private readonly List<SalaryCertificateRecordDto> _records = [];

        public void Seed(SalaryCertificateRecordDto record)
        {
            _records.Add(record);
        }

        public void Add(global::Payroll.Domain.SalaryCertificate.SalaryCertificateRecord record)
        {
            _records.Add(new SalaryCertificateRecordDto(
                record.Id,
                record.EmployeeId,
                record.Year,
                record.CreatedAtUtc,
                record.OutputFilePath,
                record.FileHash));
        }

        public Task<SalaryCertificateRecordDto?> GetLatestAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
        {
            var latest = _records
                .Where(record => record.EmployeeId == employeeId && record.Year == year)
                .OrderByDescending(record => record.CreatedAtUtc)
                .FirstOrDefault();

            return Task.FromResult(latest);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPayrollSettingsRepositoryForSalaryCertificate : IPayrollSettingsRepository
    {
        public Task<PayrollSettingsDto> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PayrollSettingsDto(
                string.Empty,
                PayrollSettings.DefaultAppFontFamily,
                PayrollSettings.DefaultAppFontSize,
                PayrollSettings.DefaultAppTextColorHex,
                PayrollSettings.DefaultAppMutedTextColorHex,
                PayrollSettings.DefaultAppBackgroundColorHex,
                PayrollSettings.DefaultAppAccentColorHex,
                PayrollSettings.DefaultAppLogoText,
                string.Empty,
                PayrollSettings.DefaultPrintFontFamily,
                PayrollSettings.DefaultPrintFontSize,
                PayrollSettings.DefaultPrintTextColorHex,
                PayrollSettings.DefaultPrintMutedTextColorHex,
                PayrollSettings.DefaultPrintAccentColorHex,
                PayrollSettings.DefaultPrintLogoText,
                string.Empty,
                string.Empty,
                GetWorkspaceTemplatePath(),
                PayrollSettings.DefaultDecimalSeparator,
                PayrollSettings.DefaultThousandsSeparator,
                PayrollSettings.DefaultCurrencyCode,
                null,
                null,
                null,
                PayrollSettings.DefaultAhvIvEoRate,
                PayrollSettings.DefaultAlvRate,
                PayrollSettings.DefaultSicknessAccidentInsuranceRate,
                PayrollSettings.DefaultTrainingAndHolidayRate,
                PayrollSettings.DefaultVacationCompensationRate,
                PayrollSettings.DefaultVacationCompensationRateAge50Plus,
                0m,
                0m,
                0m,
                [],
                [],
                [],
                []));
        }

        public Task<WorkTimeSupplementSettings> GetWorkTimeSupplementSettingsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(WorkTimeSupplementSettings.Empty);
        }

        public Task<PayrollSettingsDto> SaveAsync(SavePayrollSettingsCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
