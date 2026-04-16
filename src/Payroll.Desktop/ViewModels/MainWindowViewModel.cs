using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.Imports;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;
using Payroll.Desktop.Styles;
using Payroll.Domain.Employees;

namespace Payroll.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private enum WorkspaceSection
    {
        TimeAndExpenses,
        PayrollRuns,
        Reporting,
        Employees,
        Settings,
        Help
    }

    private const string ActivityFilterAll = "Alle";
    private const string ActivityFilterActive = "Aktiv";
    private const string ActivityFilterInactive = "Inaktiv";
    private const string MonthCaptureFilterAll = "Alle Mitarbeitenden";
    private const string MonthCaptureFilterWithoutMonth = "Ohne Monatserfassung";
    private const string MonthCaptureFilterWithMonth = "Mit Monatserfassung";
    private const string WithholdingTaxUnknown = "Ungeklaert";
    private const string WithholdingTaxYes = "Ja";
    private const string WithholdingTaxNo = "Nein";
    private const string WageTypeHourlyLabel = "Stundenlohn";
    private const string WageTypeMonthlyLabel = "Monatslohn";
    private const string BackupTypeConfigurationLabel = "Nur Konfiguration";
    private const string BackupTypeUserDataLabel = "Nur Nutzdaten";
    private const string BackupTypeBothLabel = "Beides";
    private const string DefaultEnvironmentLabel = "Unbekannt";
    private const string ThousandsSeparatorApostropheLabel = "Apostroph (')";
    private const string ThousandsSeparatorSpaceLabel = "Leerzeichen";
    private const string SettingsVersionAreaGeneral = "Allgemein";
    private const string SettingsVersionAreaHourly = "Stundenlohn";
    private const string SettingsVersionAreaMonthlySalary = "Monatslohn";
    private static readonly string StartupArgumentsHelpText =
        "--db-path=/voller/pfad/zur/datei.db" + Environment.NewLine +
        "--environment=Development|Production|Test";

    private readonly EmployeeService _employeeService;
    private readonly ImportService _importService;
    private readonly IBackupRestoreService _backupRestoreService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly ReportingService _reportingService;
    private readonly MonthlyRecordService _monthlyRecordService;
    private readonly SqlExplorerViewModel _sqlExplorer;
    private EmployeeListItemViewModel? _selectedEmployee;
    private Guid? _currentEmployeeId;
    private Guid? _pendingEmployeeId;
    private Guid? _returnEmployeeId;
    private string _searchText = string.Empty;
    private string _selectedActivityFilter = ActivityFilterAll;
    private string _personnelNumber = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private DateTimeOffset? _birthDate;
    private DateTimeOffset? _entryDate;
    private DateTimeOffset? _exitDate;
    private bool _isActive = true;
    private string _street = string.Empty;
    private string? _houseNumber;
    private string? _addressLine2;
    private string _postalCode = string.Empty;
    private string _city = string.Empty;
    private string _country = "Schweiz";
    private string? _residenceCountry;
    private string? _nationality;
    private string? _permitCode;
    private string? _taxStatus;
    private string _selectedWithholdingTaxOption = WithholdingTaxUnknown;
    private string? _ahvNumber;
    private string? _iban;
    private string? _phoneNumber;
    private string? _email;
    private EditableSettingOptionViewModel? _selectedDepartmentOption;
    private EditableSettingOptionViewModel? _selectedEmploymentCategoryOption;
    private EditableSettingOptionViewModel? _selectedEmploymentLocationOption;
    private string _selectedWageType = WageTypeHourlyLabel;
    private EditableSettingOptionViewModel? _selectedSettingsDepartment;
    private EditableSettingOptionViewModel? _selectedSettingsEmploymentCategory;
    private EditableSettingOptionViewModel? _selectedSettingsEmploymentLocation;
    private string _newDepartmentName = string.Empty;
    private string _newEmploymentCategoryName = string.Empty;
    private string _newEmploymentLocationName = string.Empty;
    private DateTimeOffset? _contractValidFrom;
    private DateTimeOffset? _contractValidTo;
    private string _hourlyRateChf = string.Empty;
    private string _monthlyBvgDeductionChf = string.Empty;
    private string _specialSupplementRateChf = string.Empty;
    private string? _settingsNightSupplementRate;
    private string? _settingsSundaySupplementRate;
    private string? _settingsHolidaySupplementRate;
    private DateTimeOffset? _settingsGeneralValidFrom;
    private DateTimeOffset? _settingsGeneralValidTo;
    private DateTimeOffset? _settingsHourlyValidFrom;
    private DateTimeOffset? _settingsHourlyValidTo;
    private DateTimeOffset? _settingsMonthlySalaryValidFrom;
    private DateTimeOffset? _settingsMonthlySalaryValidTo;
    private string _settingsAhvIvEoRate = string.Empty;
    private string _settingsAlvRate = string.Empty;
    private string _settingsSicknessAccidentInsuranceRate = string.Empty;
    private string _settingsTrainingAndHolidayRate = string.Empty;
    private string _settingsVacationCompensationRate = string.Empty;
    private string _settingsVacationCompensationRateAge50Plus = string.Empty;
    private string _settingsVehiclePauschalzone1RateChf = string.Empty;
    private string _settingsVehiclePauschalzone2RateChf = string.Empty;
    private string _settingsVehicleRegiezone1RateChf = string.Empty;
    private string _settingsCompanyAddress = string.Empty;
    private string _settingsAppFontFamily = string.Empty;
    private string _settingsAppFontSize = string.Empty;
    private string _settingsAppTextColorHex = string.Empty;
    private string _settingsAppMutedTextColorHex = string.Empty;
    private string _settingsAppBackgroundColorHex = string.Empty;
    private string _settingsAppAccentColorHex = string.Empty;
    private string _settingsAppLogoText = string.Empty;
    private string _settingsAppLogoPath = string.Empty;
    private string _settingsPrintFontFamily = string.Empty;
    private string _settingsPrintFontSize = string.Empty;
    private string _settingsPrintTextColorHex = string.Empty;
    private string _settingsPrintMutedTextColorHex = string.Empty;
    private string _settingsPrintAccentColorHex = string.Empty;
    private string _settingsPrintLogoText = string.Empty;
    private string _settingsPrintLogoPath = string.Empty;
    private string _settingsPrintTemplate = string.Empty;
    private string _settingsDecimalSeparator = Payroll.Domain.Settings.PayrollSettings.DefaultDecimalSeparator;
    private string _settingsThousandsSeparator = ThousandsSeparatorApostropheLabel;
    private string _settingsCurrencyCode = Payroll.Domain.Settings.PayrollSettings.DefaultCurrencyCode;
    private string _backupDirectoryPath = string.Empty;
    private string _backupFileName = string.Empty;
    private string _selectedBackupContentType = BackupTypeBothLabel;
    private string _restoreFilePath = string.Empty;
    private string _selectedRestoreContentType = BackupTypeBothLabel;
    private string _personImportCsvFilePath = string.Empty;
    private string _selectedPersonImportDelimiter = "Semikolon (;)";
    private bool _personImportFieldsEnclosed = true;
    private string _selectedPersonImportTextQualifier = "Doppelte Anfuehrungszeichen (\")";
    private string _personImportConfigurationName = string.Empty;
    private ImportConfigurationItemViewModel? _selectedPersonImportConfiguration;
    private string _personImportStatusMessage = "CSV-Datei laden, Mapping zuordnen und danach importieren.";
    private string _timeImportCsvFilePath = string.Empty;
    private string _selectedTimeImportDelimiter = "Semikolon (;)";
    private bool _timeImportFieldsEnclosed = true;
    private string _selectedTimeImportTextQualifier = "Doppelte Anfuehrungszeichen (\")";
    private string _timeImportConfigurationName = string.Empty;
    private ImportConfigurationItemViewModel? _selectedTimeImportConfiguration;
    private DateTimeOffset? _timeImportMonth = new(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
    private ImportedMonthStatusItemViewModel? _selectedImportedTimeMonth;
    private string _timeImportStatusMessage = "CSV-Datei laden, Mapping zuordnen, Importmonat waehlen und danach importieren.";
    private string _appLogoText = ThemeTokens.BrandLogoText;
    private Bitmap? _appLogoImage;
    private string _statusMessage = "Mitarbeitende koennen links ausgewaehlt werden.";
    private bool _isBusy;
    private bool _isEditing;
    private bool _isCreatingNew;
    private bool _showDeleteConfirmation;
    private bool _showContractVersionDialog;
    private bool _showContractVersionCreateSection;
    private bool _showSettingsVersionDialog;
    private bool _showSettingsVersionCreateSection;
    private DateTimeOffset? _newContractVersionValidFrom;
    private DateTimeOffset? _newContractVersionValidTo;
    private DateTimeOffset? _newSettingsVersionValidFrom;
    private DateTimeOffset? _newSettingsVersionValidTo;
    private Guid? _loadedCurrentContractId;
    private Guid? _loadedCurrentGeneralSettingsVersionId;
    private Guid? _loadedCurrentHourlySettingsVersionId;
    private Guid? _loadedCurrentMonthlySalarySettingsVersionId;
    private EmploymentContractHistoryItemViewModel? _selectedContractHistoryEntry;
    private PayrollCalculationSettingsVersionItemViewModel? _selectedSettingsVersionHistoryEntry;
    private string _selectedSettingsVersionArea = SettingsVersionAreaGeneral;
    private string _employeeCountSummary = "Keine Mitarbeitenden geladen.";
    private string _selectedMonthCaptureFilter = MonthCaptureFilterAll;
    private string _monthCaptureSummary = "Keine Stundenerfassungen geladen.";
    private IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto> _allMonthCaptureOverviewRows = [];
    private IReadOnlyCollection<PayrollGeneralSettingsVersionDto> _generalSettingsHistory = [];
    private IReadOnlyCollection<PayrollHourlySettingsVersionDto> _hourlySettingsHistory = [];
    private IReadOnlyCollection<PayrollMonthlySalarySettingsVersionDto> _monthlySalarySettingsHistory = [];
    private WorkspaceSection _currentSection = WorkspaceSection.TimeAndExpenses;

    public MainWindowViewModel(EmployeeService employeeService, ImportService importService, IBackupRestoreService backupRestoreService, PayrollSettingsService payrollSettingsService, ReportingService reportingService, MonthlyRecordService monthlyRecordService, MonthlyRecordViewModel monthlyRecord, string workspaceLabel, string? databasePath = null, string? environmentName = null)
        : this(
            employeeService,
            importService,
            backupRestoreService,
            payrollSettingsService,
            reportingService,
            monthlyRecordService,
            new SqlExplorerViewModel(),
            monthlyRecord,
            workspaceLabel,
            databasePath,
            environmentName)
    {
    }

    public MainWindowViewModel(EmployeeService employeeService, ImportService importService, IBackupRestoreService backupRestoreService, PayrollSettingsService payrollSettingsService, ReportingService reportingService, MonthlyRecordService monthlyRecordService, SqlExplorerViewModel sqlExplorer, MonthlyRecordViewModel monthlyRecord, string workspaceLabel, string? databasePath = null, string? environmentName = null)
    {
        _employeeService = employeeService;
        _importService = importService;
        _backupRestoreService = backupRestoreService;
        _payrollSettingsService = payrollSettingsService;
        _reportingService = reportingService;
        _monthlyRecordService = monthlyRecordService;
        _sqlExplorer = sqlExplorer;
        MonthlyRecord = monthlyRecord;
        WorkspaceLabel = workspaceLabel;
        DatabasePathDisplay = string.IsNullOrWhiteSpace(databasePath) ? "Kein Pfad verfuegbar." : databasePath;
        EnvironmentNameDisplay = string.IsNullOrWhiteSpace(environmentName) ? DefaultEnvironmentLabel : environmentName;
        Employees = [];
        DepartmentOptions = [];
        EmploymentCategoryOptions = [];
        EmploymentLocationOptions = [];
        PayrollPreviewHelpOptions = [];
        ContractHistory = [];
        SettingsVersionHistory = [];
        PersonImportConfigurations = [];
        TimeImportConfigurations = [];
        PersonImportFieldMappings = [];
        PersonImportPreviewItems = [];
        TimeImportFieldMappings = [];
        ImportedTimeMonths = [];
        MonthCaptureOverviewRows = [];
        ActivityFilters = [ActivityFilterAll, ActivityFilterActive, ActivityFilterInactive];
        MonthCaptureFilters = [MonthCaptureFilterAll, MonthCaptureFilterWithoutMonth, MonthCaptureFilterWithMonth];
        WithholdingTaxOptions = [WithholdingTaxUnknown, WithholdingTaxYes, WithholdingTaxNo];
        WageTypeOptions = [WageTypeHourlyLabel, WageTypeMonthlyLabel];
        BackupContentTypeOptions = [BackupTypeConfigurationLabel, BackupTypeUserDataLabel, BackupTypeBothLabel];
        DecimalSeparatorOptions = [",", "."];
        ThousandsSeparatorOptions = [ThousandsSeparatorApostropheLabel, ThousandsSeparatorSpaceLabel];
        PersonImportDelimiterOptions =
        [
            "Semikolon (;)",
            "Komma (,)",
            "Tabulator",
            "Pipe (|)"
        ];
        PersonImportTextQualifierOptions =
        [
            "Doppelte Anfuehrungszeichen (\")",
            "Einfache Anfuehrungszeichen (')"
        ];
        RefreshCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        SearchCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        NewEmployeeCommand = new DelegateCommand(BeginCreateEmployee, () => CanStartCreate);
        EditEmployeeCommand = new DelegateCommand(BeginEditEmployee, () => CanStartEdit);
        SaveCommand = new DelegateCommand(SaveAsync, () => CanSave);
        CancelCommand = new DelegateCommand(CancelAsync, () => CanCancel);
        DeleteCommand = new DelegateCommand(RequestDelete, () => CanRequestDelete);
        ConfirmDeleteCommand = new DelegateCommand(ConfirmDeleteAsync, () => CanConfirmDelete);
        DismissDeleteCommand = new DelegateCommand(DismissDeleteConfirmation, () => CanDismissDelete);
        OpenContractVersionDialogCommand = new DelegateCommand(OpenContractVersionDialogFromButton, () => CanOpenContractVersionDialog);
        OpenNewContractVersionDialogCommand = new DelegateCommand(OpenNewContractVersionDialogFromButton, () => CanOpenNewContractVersionDialog);
        ConfirmContractVersionDialogCommand = new DelegateCommand(ConfirmContractVersionDialogAsync, () => CanConfirmContractVersionDialog);
        DismissContractVersionDialogCommand = new DelegateCommand(DismissContractVersionDialog, () => CanDismissContractVersionDialog);
        OpenGeneralSettingsVersionDialogCommand = new DelegateCommand(OpenGeneralSettingsVersionDialogFromButton, () => CanOpenGeneralSettingsVersionDialog);
        OpenNewGeneralSettingsVersionDialogCommand = new DelegateCommand(OpenNewGeneralSettingsVersionDialogFromButton, () => CanOpenNewGeneralSettingsVersionDialog);
        OpenHourlySettingsVersionDialogCommand = new DelegateCommand(OpenHourlySettingsVersionDialogFromButton, () => CanOpenHourlySettingsVersionDialog);
        OpenNewHourlySettingsVersionDialogCommand = new DelegateCommand(OpenNewHourlySettingsVersionDialogFromButton, () => CanOpenNewHourlySettingsVersionDialog);
        OpenMonthlySalarySettingsVersionDialogCommand = new DelegateCommand(OpenMonthlySalarySettingsVersionDialogFromButton, () => CanOpenMonthlySalarySettingsVersionDialog);
        OpenNewMonthlySalarySettingsVersionDialogCommand = new DelegateCommand(OpenNewMonthlySalarySettingsVersionDialogFromButton, () => CanOpenNewMonthlySalarySettingsVersionDialog);
        ConfirmSettingsVersionDialogCommand = new DelegateCommand(ConfirmSettingsVersionDialogAsync, () => CanConfirmSettingsVersionDialog);
        DismissSettingsVersionDialogCommand = new DelegateCommand(DismissSettingsVersionDialog, () => CanDismissSettingsVersionDialog);
        ClearExitDateCommand = new DelegateCommand(ClearExitDate, () => CanClearExitDate);
        ShowTimeAndExpensesCommand = new DelegateCommand(SwitchToTimeAndExpensesWorkspace, () => !IsTimeAndExpensesWorkspace);
        ShowPayrollRunsCommand = new DelegateCommand(SwitchToPayrollRunsWorkspace, () => !IsPayrollRunsWorkspace);
        ShowReportingCommand = new DelegateCommand(SwitchToReportingWorkspace, () => !IsReportingWorkspace);
        ShowEmployeesCommand = new DelegateCommand(SwitchToEmployeesWorkspace, () => !IsEmployeeWorkspace);
        ShowSettingsCommand = new DelegateCommand(SwitchToSettingsWorkspace, () => !IsSettingsWorkspace);
        ShowHelpCommand = new DelegateCommand(SwitchToHelpWorkspace, () => !IsHelpWorkspace);
        SaveSettingsCommand = new DelegateCommand(SaveSettingsAsync, () => CanSaveSettings);
        LoadPersonImportCsvCommand = new DelegateCommand(LoadPersonImportCsvAsync, () => CanLoadPersonImportCsv);
        SavePersonImportConfigurationCommand = new DelegateCommand(SavePersonImportConfigurationAsync, () => CanSavePersonImportConfiguration);
        LoadPersonImportConfigurationCommand = new DelegateCommand(LoadSelectedPersonImportConfigurationAsync, () => CanLoadPersonImportConfiguration);
        ImportPersonDataCommand = new DelegateCommand(async () => await PreparePersonImportPreviewAsync(), () => CanImportPersonData);
        LoadTimeImportCsvCommand = new DelegateCommand(LoadTimeImportCsvAsync, () => CanLoadTimeImportCsv);
        SaveTimeImportConfigurationCommand = new DelegateCommand(SaveTimeImportConfigurationAsync, () => CanSaveTimeImportConfiguration);
        LoadTimeImportConfigurationCommand = new DelegateCommand(LoadSelectedTimeImportConfigurationAsync, () => CanLoadTimeImportConfiguration);
        ImportTimeDataCommand = new DelegateCommand(async () => await ImportTimeDataAsync(), () => CanImportTimeData);
        DeleteImportedTimeMonthCommand = new DelegateCommand(DeleteImportedTimeMonthAsync, () => CanDeleteImportedTimeMonth);
        CreateBackupCommand = new DelegateCommand(CreateBackupAsync, () => CanCreateBackup);
        RestoreBackupCommand = new DelegateCommand(RestoreBackupAsync, () => CanRestoreBackup);
        CreatePayrollPdfCommand = new DelegateCommand(CreatePayrollPdfAsync, () => CanCreatePayrollPdf);
        AddDepartmentOptionCommand = new DelegateCommand(AddDepartmentOption, () => CanManageSettingsOptions);
        RemoveDepartmentOptionCommand = new DelegateCommand(RemoveDepartmentOption, () => CanRemoveDepartmentOption);
        AddEmploymentCategoryOptionCommand = new DelegateCommand(AddEmploymentCategoryOption, () => CanManageSettingsOptions);
        RemoveEmploymentCategoryOptionCommand = new DelegateCommand(RemoveEmploymentCategoryOption, () => CanRemoveEmploymentCategoryOption);
        AddEmploymentLocationOptionCommand = new DelegateCommand(AddEmploymentLocationOption, () => CanManageSettingsOptions);
        RemoveEmploymentLocationOptionCommand = new DelegateCommand(RemoveEmploymentLocationOption, () => CanRemoveEmploymentLocationOption);
        BackupDirectoryPath = _backupRestoreService.GetDefaultBackupDirectory();
        BackupFileName = _backupRestoreService.CreateDefaultFileName(DateTimeOffset.Now);
        MonthlyRecord.PropertyChanged += OnMonthlyRecordPropertyChanged;
        MonthlyRecord.TimeCaptureChanged += OnMonthlyRecordTimeCaptureChanged;
        InitializeImportFieldMappings();

        ClearFormForEmptyState();
    }

    public string Title => "PayrollApp - Monatserfassung";
    public string WorkspaceLabel { get; }
    public string DatabasePathDisplay { get; }
    public string EnvironmentNameDisplay { get; }
    public string StartupArgumentsHelp => StartupArgumentsHelpText;
    public MonthlyRecordViewModel MonthlyRecord { get; }
    public SqlExplorerViewModel SqlExplorer => _sqlExplorer;
    public string AppLogoText
    {
        get => _appLogoText;
        private set => SetProperty(ref _appLogoText, value);
    }
    public Bitmap? AppLogoImage
    {
        get => _appLogoImage;
        private set
        {
            if (SetProperty(ref _appLogoImage, value))
            {
                RaisePropertyChanged(nameof(HasAppLogoImage));
                RaisePropertyChanged(nameof(ShowAppLogoText));
            }
        }
    }
    public bool HasAppLogoImage => AppLogoImage is not null;
    public bool ShowAppLogoText => !HasAppLogoImage;
    public ObservableCollection<EmployeeListItemViewModel> Employees { get; }
    public ObservableCollection<EditableSettingOptionViewModel> DepartmentOptions { get; }
    public ObservableCollection<EditableSettingOptionViewModel> EmploymentCategoryOptions { get; }
    public ObservableCollection<EditableSettingOptionViewModel> EmploymentLocationOptions { get; }
    public ObservableCollection<PayrollPreviewHelpToggleViewModel> PayrollPreviewHelpOptions { get; }
    public ObservableCollection<EmploymentContractHistoryItemViewModel> ContractHistory { get; }
    public ObservableCollection<PayrollCalculationSettingsVersionItemViewModel> SettingsVersionHistory { get; }
    public ObservableCollection<ImportConfigurationItemViewModel> PersonImportConfigurations { get; }
    public ObservableCollection<ImportConfigurationItemViewModel> TimeImportConfigurations { get; }
    public ObservableCollection<ImportFieldMappingRowViewModel> PersonImportFieldMappings { get; }
    public ObservableCollection<PersonImportPreviewItemViewModel> PersonImportPreviewItems { get; }
    public ObservableCollection<ImportFieldMappingRowViewModel> TimeImportFieldMappings { get; }
    public ObservableCollection<ImportedMonthStatusItemViewModel> ImportedTimeMonths { get; }
    public ObservableCollection<MonthlyTimeCaptureOverviewRowDto> MonthCaptureOverviewRows { get; }
    public IReadOnlyList<string> ActivityFilters { get; }
    public IReadOnlyList<string> MonthCaptureFilters { get; }
    public IReadOnlyList<string> WithholdingTaxOptions { get; }
    public IReadOnlyList<string> WageTypeOptions { get; }
    public IReadOnlyList<string> BackupContentTypeOptions { get; }
    public IReadOnlyList<string> DecimalSeparatorOptions { get; }
    public IReadOnlyList<string> ThousandsSeparatorOptions { get; }
    public IReadOnlyList<string> PersonImportDelimiterOptions { get; }
    public IReadOnlyList<string> PersonImportTextQualifierOptions { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand NewEmployeeCommand { get; }
    public DelegateCommand EditEmployeeCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand ConfirmDeleteCommand { get; }
    public DelegateCommand DismissDeleteCommand { get; }
    public DelegateCommand OpenContractVersionDialogCommand { get; }
    public DelegateCommand OpenNewContractVersionDialogCommand { get; }
    public DelegateCommand ConfirmContractVersionDialogCommand { get; }
    public DelegateCommand DismissContractVersionDialogCommand { get; }
    public DelegateCommand OpenGeneralSettingsVersionDialogCommand { get; }
    public DelegateCommand OpenNewGeneralSettingsVersionDialogCommand { get; }
    public DelegateCommand OpenHourlySettingsVersionDialogCommand { get; }
    public DelegateCommand OpenNewHourlySettingsVersionDialogCommand { get; }
    public DelegateCommand OpenMonthlySalarySettingsVersionDialogCommand { get; }
    public DelegateCommand OpenNewMonthlySalarySettingsVersionDialogCommand { get; }
    public DelegateCommand ConfirmSettingsVersionDialogCommand { get; }
    public DelegateCommand DismissSettingsVersionDialogCommand { get; }
    public DelegateCommand ClearExitDateCommand { get; }
    public DelegateCommand ShowTimeAndExpensesCommand { get; }
    public DelegateCommand ShowPayrollRunsCommand { get; }
    public DelegateCommand ShowReportingCommand { get; }
    public DelegateCommand ShowEmployeesCommand { get; }
    public DelegateCommand ShowSettingsCommand { get; }
    public DelegateCommand ShowHelpCommand { get; }
    public DelegateCommand SaveSettingsCommand { get; }
    public DelegateCommand LoadPersonImportCsvCommand { get; }
    public DelegateCommand SavePersonImportConfigurationCommand { get; }
    public DelegateCommand LoadPersonImportConfigurationCommand { get; }
    public DelegateCommand ImportPersonDataCommand { get; }
    public DelegateCommand LoadTimeImportCsvCommand { get; }
    public DelegateCommand SaveTimeImportConfigurationCommand { get; }
    public DelegateCommand LoadTimeImportConfigurationCommand { get; }
    public DelegateCommand ImportTimeDataCommand { get; }
    public DelegateCommand DeleteImportedTimeMonthCommand { get; }
    public DelegateCommand CreateBackupCommand { get; }
    public DelegateCommand RestoreBackupCommand { get; }
    public DelegateCommand CreatePayrollPdfCommand { get; }
    public DelegateCommand AddDepartmentOptionCommand { get; }
    public DelegateCommand RemoveDepartmentOptionCommand { get; }
    public DelegateCommand AddEmploymentCategoryOptionCommand { get; }
    public DelegateCommand RemoveEmploymentCategoryOptionCommand { get; }
    public DelegateCommand AddEmploymentLocationOptionCommand { get; }
    public DelegateCommand RemoveEmploymentLocationOptionCommand { get; }
    public bool IsTimeAndExpensesWorkspace => _currentSection == WorkspaceSection.TimeAndExpenses;
    public bool IsPayrollRunsWorkspace => _currentSection == WorkspaceSection.PayrollRuns;
    public bool IsReportingWorkspace => _currentSection == WorkspaceSection.Reporting;
    public bool IsEmployeeWorkspace => _currentSection == WorkspaceSection.Employees;
    public bool IsSettingsWorkspace => _currentSection == WorkspaceSection.Settings;
    public bool IsHelpWorkspace => _currentSection == WorkspaceSection.Help;
    public bool ShowTimeAndExpensesWorkspace => IsTimeAndExpensesWorkspace;
    public bool ShowPayrollRunsWorkspace => IsPayrollRunsWorkspace;
    public bool ShowReportingWorkspace => IsReportingWorkspace;
    public bool ShowEmployeeWorkspace => IsEmployeeWorkspace;
    public bool ShowSettingsWorkspace => IsSettingsWorkspace;
    public bool ShowHelpWorkspace => IsHelpWorkspace;
    public bool ShowEmployeeSelectionArea => !IsSettingsWorkspace && !IsHelpWorkspace;
    public bool ShowPrimaryWorkspaceArea => !IsSettingsWorkspace && !IsHelpWorkspace;
    public bool ShowPrimaryWorkspaceHeader => !IsTimeAndExpensesWorkspace && !IsEmployeeWorkspace;
    public string MonthCaptureMonthLabel => MonthlyRecord.SelectedMonth.HasValue
        ? $"{MonthlyRecord.SelectedMonth.Value:MM/yyyy}"
        : "-";
    public string MonthCaptureSummary
    {
        get => _monthCaptureSummary;
        private set => SetProperty(ref _monthCaptureSummary, value);
    }
    public string SelectedMonthCaptureFilter
    {
        get => _selectedMonthCaptureFilter;
        set
        {
            if (SetProperty(ref _selectedMonthCaptureFilter, value))
            {
                ApplyMonthCaptureOverviewFilter();
            }
        }
    }

    public string FormTitle => _isCreatingNew
        ? "Neuer Mitarbeitender"
        : _currentEmployeeId.HasValue
            ? (_isEditing ? "Mitarbeitenden bearbeiten" : "Mitarbeitenden anzeigen")
            : "Keine Auswahl";

    public string FormDescription => _isCreatingNew
        ? "Neue Stammdaten erfassen und anschliessend speichern."
        : _currentEmployeeId.HasValue
            ? (_isEditing
                ? "Aenderungen werden erst nach Speichern uebernommen."
                : "Die Daten werden erst nach einem expliziten Klick auf Bearbeiten veraendert.")
            : "Links eine Person auswaehlen oder eine neue Person anlegen.";

    public string DeleteConfirmationText => "Mitarbeitende werden aus fachlichen Gruenden nicht physisch geloescht. Die Aktion archiviert den Datensatz, setzt ihn auf inaktiv und hinterlegt ein Austrittsdatum von heute, falls noch keines vorhanden ist.";

    public bool AreFieldsReadOnly => !_isEditing;
    public bool CanEditFields => _isEditing && !IsBusy;
    public bool CanSearchEmployees => !IsBusy;
    public bool CanSelectEmployees => !_isEditing;
    public bool CanBrowseEmployees => !IsBusy && !_isEditing;
    public bool CanStartCreate => !IsBusy && !_isEditing;
    public bool CanStartEdit => !IsBusy && !_isEditing && _currentEmployeeId.HasValue;
    public bool CanSave => !IsBusy && _isEditing && !ShowContractVersionDialog;
    public bool CanCancel => !IsBusy && _isEditing;
    public bool CanRequestDelete => !IsBusy && _isEditing && _currentEmployeeId.HasValue && IsActiveEmployee;
    public bool CanConfirmDelete => !IsBusy && _showDeleteConfirmation && _currentEmployeeId.HasValue;
    public bool CanDismissDelete => !IsBusy && _showDeleteConfirmation;
    public bool CanOpenContractVersionDialog => !IsBusy && _currentEmployeeId.HasValue;
    public bool CanOpenNewContractVersionDialog => !IsBusy && _isEditing && _currentEmployeeId.HasValue;
    public bool CanConfirmContractVersionDialog => !IsBusy && ShowContractVersionDialog && ShowContractVersionCreateSection && NewContractVersionValidFrom.HasValue;
    public bool CanDismissContractVersionDialog => !IsBusy && ShowContractVersionDialog;
    public bool CanClearExitDate => CanEditFields && ExitDate.HasValue;
    public bool CanOpenGeneralSettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanOpenNewGeneralSettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanOpenHourlySettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanOpenNewHourlySettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanOpenMonthlySalarySettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanOpenNewMonthlySalarySettingsVersionDialog => !IsBusy && IsSettingsWorkspace;
    public bool CanConfirmSettingsVersionDialog => !IsBusy && ShowSettingsVersionDialog && ShowSettingsVersionCreateSection && NewSettingsVersionValidFrom.HasValue;
    public bool CanDismissSettingsVersionDialog => !IsBusy && ShowSettingsVersionDialog;
    public bool CanSaveSettings => !IsBusy && IsSettingsWorkspace && !ShowSettingsVersionDialog;
    public bool CanLoadPersonImportCsv => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(PersonImportCsvFilePath);
    public bool CanSavePersonImportConfiguration => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(PersonImportConfigurationName);
    public bool CanLoadPersonImportConfiguration => !IsBusy && IsSettingsWorkspace && SelectedPersonImportConfiguration is not null;
    public bool CanImportPersonData => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(PersonImportCsvFilePath) && PersonImportValidationErrors.Count == 0 && PersonImportHasCsvHeaders;
    public bool CanLoadTimeImportCsv => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(TimeImportCsvFilePath);
    public bool CanSaveTimeImportConfiguration => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(TimeImportConfigurationName);
    public bool CanLoadTimeImportConfiguration => !IsBusy && IsSettingsWorkspace && SelectedTimeImportConfiguration is not null;
    public bool CanImportTimeData => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(TimeImportCsvFilePath) && TimeImportValidationErrors.Count == 0 && TimeImportHasCsvHeaders && TimeImportMonth.HasValue;
    public bool CanDeleteImportedTimeMonth => !IsBusy && IsSettingsWorkspace && SelectedImportedTimeMonth is not null;
    public bool CanCreateBackup => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(BackupDirectoryPath) && !string.IsNullOrWhiteSpace(BackupFileName);
    public bool CanRestoreBackup => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(RestoreFilePath);
    public bool CanCreatePayrollPdf => !IsBusy && IsPayrollRunsWorkspace && _currentEmployeeId.HasValue && MonthlyRecord.SelectedMonth.HasValue;
    public bool CanManageSettingsOptions => !IsBusy && IsSettingsWorkspace;
    public bool CanRemoveDepartmentOption => CanManageSettingsOptions && SelectedSettingsDepartment is not null;
    public bool CanRemoveEmploymentCategoryOption => CanManageSettingsOptions && SelectedSettingsEmploymentCategory is not null;
    public bool CanRemoveEmploymentLocationOption => CanManageSettingsOptions && SelectedSettingsEmploymentLocation is not null;
    public bool ShowViewActions => !_isEditing;
    public bool ShowEditActions => _isEditing;
    public bool ShowContractVersionCreateSection => _showContractVersionCreateSection;
    public bool ShowSettingsVersionCreateSection => _showSettingsVersionCreateSection;
    public bool ShowDeleteConfirmation
    {
        get => _showDeleteConfirmation;
        private set
        {
            if (SetProperty(ref _showDeleteConfirmation, value))
            {
                RaisePropertyChanged(nameof(CanConfirmDelete));
                RaisePropertyChanged(nameof(CanDismissDelete));
                ConfirmDeleteCommand.RaiseCanExecuteChanged();
                DismissDeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowContractVersionDialog
    {
        get => _showContractVersionDialog;
        private set
        {
            if (SetProperty(ref _showContractVersionDialog, value))
            {
                RaisePropertyChanged(nameof(CanSave));
                RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
                RaisePropertyChanged(nameof(CanDismissContractVersionDialog));
                ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
                DismissContractVersionDialogCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowSettingsVersionDialog
    {
        get => _showSettingsVersionDialog;
        private set
        {
            if (SetProperty(ref _showSettingsVersionDialog, value))
            {
                RaisePropertyChanged(nameof(CanSaveSettings));
                RaisePropertyChanged(nameof(CanConfirmSettingsVersionDialog));
                RaisePropertyChanged(nameof(CanDismissSettingsVersionDialog));
                ConfirmSettingsVersionDialogCommand.RaiseCanExecuteChanged();
                DismissSettingsVersionDialogCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EmployeeCountSummary
    {
        get => _employeeCountSummary;
        private set => SetProperty(ref _employeeCountSummary, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string SelectedActivityFilter
    {
        get => _selectedActivityFilter;
        set => SetProperty(ref _selectedActivityFilter, value);
    }

    public EmployeeListItemViewModel? SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            if (!SetProperty(ref _selectedEmployee, value) || value is null || _isEditing)
            {
                return;
            }

            if (IsBusy)
            {
                _pendingEmployeeId = value.EmployeeId;
                return;
            }

            _ = LoadEmployeeAsync(value.EmployeeId);
        }
    }

    public string PersonnelNumber
    {
        get => _personnelNumber;
        set => SetProperty(ref _personnelNumber, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public DateTimeOffset? BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }

    public DateTimeOffset? EntryDate
    {
        get => _entryDate;
        set => SetProperty(ref _entryDate, value);
    }

    public DateTimeOffset? ExitDate
    {
        get => _exitDate;
        set
        {
            if (SetProperty(ref _exitDate, value))
            {
                RaisePropertyChanged(nameof(CanClearExitDate));
                ClearExitDateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsActiveEmployee
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                if (value && ExitDate.HasValue)
                {
                    ExitDate = null;
                }

                RaisePropertyChanged(nameof(CanRequestDelete));
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Street
    {
        get => _street;
        set => SetProperty(ref _street, value);
    }

    public string? HouseNumber
    {
        get => _houseNumber;
        set => SetProperty(ref _houseNumber, value);
    }

    public string? AddressLine2
    {
        get => _addressLine2;
        set => SetProperty(ref _addressLine2, value);
    }

    public string PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    public string? ResidenceCountry
    {
        get => _residenceCountry;
        set => SetProperty(ref _residenceCountry, value);
    }

    public string? Nationality
    {
        get => _nationality;
        set => SetProperty(ref _nationality, value);
    }

    public string? PermitCode
    {
        get => _permitCode;
        set => SetProperty(ref _permitCode, value);
    }

    public string? TaxStatus
    {
        get => _taxStatus;
        set => SetProperty(ref _taxStatus, value);
    }

    public string SelectedWithholdingTaxOption
    {
        get => _selectedWithholdingTaxOption;
        set => SetProperty(ref _selectedWithholdingTaxOption, value);
    }

    public string? AhvNumber
    {
        get => _ahvNumber;
        set => SetProperty(ref _ahvNumber, value);
    }

    public string? Iban
    {
        get => _iban;
        set => SetProperty(ref _iban, value);
    }

    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public EditableSettingOptionViewModel? SelectedDepartmentOption
    {
        get => _selectedDepartmentOption;
        set => SetProperty(ref _selectedDepartmentOption, value);
    }

    public EditableSettingOptionViewModel? SelectedEmploymentCategoryOption
    {
        get => _selectedEmploymentCategoryOption;
        set => SetProperty(ref _selectedEmploymentCategoryOption, value);
    }

    public EditableSettingOptionViewModel? SelectedEmploymentLocationOption
    {
        get => _selectedEmploymentLocationOption;
        set => SetProperty(ref _selectedEmploymentLocationOption, value);
    }

    public string SelectedWageType
    {
        get => _selectedWageType;
        set => SetProperty(ref _selectedWageType, value);
    }

    public EditableSettingOptionViewModel? SelectedSettingsDepartment
    {
        get => _selectedSettingsDepartment;
        set
        {
            if (SetProperty(ref _selectedSettingsDepartment, value))
            {
                RaisePropertyChanged(nameof(CanRemoveDepartmentOption));
                RemoveDepartmentOptionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public EditableSettingOptionViewModel? SelectedSettingsEmploymentCategory
    {
        get => _selectedSettingsEmploymentCategory;
        set
        {
            if (SetProperty(ref _selectedSettingsEmploymentCategory, value))
            {
                RaisePropertyChanged(nameof(CanRemoveEmploymentCategoryOption));
                RemoveEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public EditableSettingOptionViewModel? SelectedSettingsEmploymentLocation
    {
        get => _selectedSettingsEmploymentLocation;
        set
        {
            if (SetProperty(ref _selectedSettingsEmploymentLocation, value))
            {
                RaisePropertyChanged(nameof(CanRemoveEmploymentLocationOption));
                RemoveEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewDepartmentName
    {
        get => _newDepartmentName;
        set => SetProperty(ref _newDepartmentName, value);
    }

    public string NewEmploymentCategoryName
    {
        get => _newEmploymentCategoryName;
        set => SetProperty(ref _newEmploymentCategoryName, value);
    }

    public string NewEmploymentLocationName
    {
        get => _newEmploymentLocationName;
        set => SetProperty(ref _newEmploymentLocationName, value);
    }

    public DateTimeOffset? ContractValidFrom
    {
        get => _contractValidFrom;
        set => SetProperty(ref _contractValidFrom, value);
    }

    public DateTimeOffset? ContractValidTo
    {
        get => _contractValidTo;
        set => SetProperty(ref _contractValidTo, value);
    }

    public string HourlyRateChf
    {
        get => _hourlyRateChf;
        set => SetProperty(ref _hourlyRateChf, value);
    }

    public string MonthlyBvgDeductionChf
    {
        get => _monthlyBvgDeductionChf;
        set => SetProperty(ref _monthlyBvgDeductionChf, value);
    }

    public string SpecialSupplementRateChf
    {
        get => _specialSupplementRateChf;
        set => SetProperty(ref _specialSupplementRateChf, value);
    }

    public DateTimeOffset? NewContractVersionValidFrom
    {
        get => _newContractVersionValidFrom;
        set
        {
            if (SetProperty(ref _newContractVersionValidFrom, value))
            {
                RaisePropertyChanged(nameof(ContractVersionDialogSummary));
                RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
                ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTimeOffset? NewContractVersionValidTo
    {
        get => _newContractVersionValidTo;
        set
        {
            if (SetProperty(ref _newContractVersionValidTo, value))
            {
                RaisePropertyChanged(nameof(ContractVersionDialogSummary));
            }
        }
    }

    public EmploymentContractHistoryItemViewModel? SelectedContractHistoryEntry
    {
        get => _selectedContractHistoryEntry;
        set => SetProperty(ref _selectedContractHistoryEntry, value);
    }

    public string ContractVersionDialogTitle => ShowContractVersionCreateSection
        ? "Neuen Vertragsstand anlegen"
        : "Vertragshistorie";

    public string ContractVersionDialogDescription => ShowContractVersionCreateSection
        ? "Neue Vertragsstaende werden getrennt vom normalen Speichern angelegt. Der bisher aktive Stand bleibt nachvollziehbar erhalten."
        : "Die Vertragshistorie wird getrennt von der Hauptmaske angezeigt. Der aktuelle Vertragsstand bleibt in der Mitarbeitendenmaske bearbeitbar.";

    public string ContractVersionDialogSummary => BuildContractVersionDialogSummary();

    public DateTimeOffset? SettingsGeneralValidFrom
    {
        get => _settingsGeneralValidFrom;
        set => SetProperty(ref _settingsGeneralValidFrom, value);
    }

    public DateTimeOffset? SettingsGeneralValidTo
    {
        get => _settingsGeneralValidTo;
        set => SetProperty(ref _settingsGeneralValidTo, value);
    }

    public DateTimeOffset? SettingsHourlyValidFrom
    {
        get => _settingsHourlyValidFrom;
        set => SetProperty(ref _settingsHourlyValidFrom, value);
    }

    public DateTimeOffset? SettingsHourlyValidTo
    {
        get => _settingsHourlyValidTo;
        set => SetProperty(ref _settingsHourlyValidTo, value);
    }

    public DateTimeOffset? SettingsMonthlySalaryValidFrom
    {
        get => _settingsMonthlySalaryValidFrom;
        set => SetProperty(ref _settingsMonthlySalaryValidFrom, value);
    }

    public DateTimeOffset? SettingsMonthlySalaryValidTo
    {
        get => _settingsMonthlySalaryValidTo;
        set => SetProperty(ref _settingsMonthlySalaryValidTo, value);
    }

    public DateTimeOffset? NewSettingsVersionValidFrom
    {
        get => _newSettingsVersionValidFrom;
        set
        {
            if (SetProperty(ref _newSettingsVersionValidFrom, value))
            {
                RaisePropertyChanged(nameof(SettingsVersionDialogSummary));
                RaisePropertyChanged(nameof(CanConfirmSettingsVersionDialog));
                ConfirmSettingsVersionDialogCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTimeOffset? NewSettingsVersionValidTo
    {
        get => _newSettingsVersionValidTo;
        set
        {
            if (SetProperty(ref _newSettingsVersionValidTo, value))
            {
                RaisePropertyChanged(nameof(SettingsVersionDialogSummary));
            }
        }
    }

    public PayrollCalculationSettingsVersionItemViewModel? SelectedSettingsVersionHistoryEntry
    {
        get => _selectedSettingsVersionHistoryEntry;
        set => SetProperty(ref _selectedSettingsVersionHistoryEntry, value);
    }

    public string SettingsVersionDialogTitle => ShowSettingsVersionCreateSection
        ? $"Neuen Satzstand fuer {SelectedSettingsVersionAreaLabel} anlegen"
        : $"{SelectedSettingsVersionAreaLabel}-Historie";

    public string SettingsVersionDialogDescription => ShowSettingsVersionCreateSection
        ? $"Neue Satzstaende fuer {SelectedSettingsVersionAreaLabel} werden getrennt vom normalen Speichern angelegt."
        : $"Die Historie fuer {SelectedSettingsVersionAreaLabel} wird getrennt vom normalen Speichern angezeigt.";

    public string SelectedSettingsVersionAreaLabel => _selectedSettingsVersionArea;

    public string SettingsVersionDialogSummary => BuildSettingsVersionDialogSummary();

    public string? SettingsNightSupplementRate
    {
        get => _settingsNightSupplementRate;
        set => SetProperty(ref _settingsNightSupplementRate, value);
    }

    public string? SettingsSundaySupplementRate
    {
        get => _settingsSundaySupplementRate;
        set => SetProperty(ref _settingsSundaySupplementRate, value);
    }

    public string? SettingsHolidaySupplementRate
    {
        get => _settingsHolidaySupplementRate;
        set => SetProperty(ref _settingsHolidaySupplementRate, value);
    }

    public string SettingsAhvIvEoRate
    {
        get => _settingsAhvIvEoRate;
        set => SetProperty(ref _settingsAhvIvEoRate, value);
    }

    public string SettingsAlvRate
    {
        get => _settingsAlvRate;
        set => SetProperty(ref _settingsAlvRate, value);
    }

    public string SettingsSicknessAccidentInsuranceRate
    {
        get => _settingsSicknessAccidentInsuranceRate;
        set => SetProperty(ref _settingsSicknessAccidentInsuranceRate, value);
    }

    public string SettingsTrainingAndHolidayRate
    {
        get => _settingsTrainingAndHolidayRate;
        set => SetProperty(ref _settingsTrainingAndHolidayRate, value);
    }

    public string SettingsVacationCompensationRate
    {
        get => _settingsVacationCompensationRate;
        set => SetProperty(ref _settingsVacationCompensationRate, value);
    }

    public string SettingsVacationCompensationRateAge50Plus
    {
        get => _settingsVacationCompensationRateAge50Plus;
        set => SetProperty(ref _settingsVacationCompensationRateAge50Plus, value);
    }

    public string SettingsVehiclePauschalzone1RateChf
    {
        get => _settingsVehiclePauschalzone1RateChf;
        set => SetProperty(ref _settingsVehiclePauschalzone1RateChf, value);
    }

    public string SettingsVehiclePauschalzone2RateChf
    {
        get => _settingsVehiclePauschalzone2RateChf;
        set => SetProperty(ref _settingsVehiclePauschalzone2RateChf, value);
    }

    public string SettingsVehicleRegiezone1RateChf
    {
        get => _settingsVehicleRegiezone1RateChf;
        set => SetProperty(ref _settingsVehicleRegiezone1RateChf, value);
    }

    public string SettingsCompanyAddress
    {
        get => _settingsCompanyAddress;
        set => SetProperty(ref _settingsCompanyAddress, value);
    }

    public string SettingsAppFontFamily
    {
        get => _settingsAppFontFamily;
        set => SetProperty(ref _settingsAppFontFamily, value);
    }

    public string SettingsAppFontSize
    {
        get => _settingsAppFontSize;
        set => SetProperty(ref _settingsAppFontSize, value);
    }

    public string SettingsAppTextColorHex
    {
        get => _settingsAppTextColorHex;
        set => SetProperty(ref _settingsAppTextColorHex, value);
    }

    public string SettingsAppMutedTextColorHex
    {
        get => _settingsAppMutedTextColorHex;
        set => SetProperty(ref _settingsAppMutedTextColorHex, value);
    }

    public string SettingsAppBackgroundColorHex
    {
        get => _settingsAppBackgroundColorHex;
        set => SetProperty(ref _settingsAppBackgroundColorHex, value);
    }

    public string SettingsAppAccentColorHex
    {
        get => _settingsAppAccentColorHex;
        set => SetProperty(ref _settingsAppAccentColorHex, value);
    }

    public string SettingsAppLogoText
    {
        get => _settingsAppLogoText;
        set => SetProperty(ref _settingsAppLogoText, value);
    }

    public string SettingsAppLogoPath
    {
        get => _settingsAppLogoPath;
        set => SetProperty(ref _settingsAppLogoPath, value);
    }

    public string SettingsPrintFontFamily
    {
        get => _settingsPrintFontFamily;
        set => SetProperty(ref _settingsPrintFontFamily, value);
    }

    public string SettingsPrintFontSize
    {
        get => _settingsPrintFontSize;
        set => SetProperty(ref _settingsPrintFontSize, value);
    }

    public string SettingsPrintTextColorHex
    {
        get => _settingsPrintTextColorHex;
        set => SetProperty(ref _settingsPrintTextColorHex, value);
    }

    public string SettingsPrintMutedTextColorHex
    {
        get => _settingsPrintMutedTextColorHex;
        set => SetProperty(ref _settingsPrintMutedTextColorHex, value);
    }

    public string SettingsPrintAccentColorHex
    {
        get => _settingsPrintAccentColorHex;
        set => SetProperty(ref _settingsPrintAccentColorHex, value);
    }

    public string SettingsPrintLogoText
    {
        get => _settingsPrintLogoText;
        set => SetProperty(ref _settingsPrintLogoText, value);
    }

    public string SettingsPrintLogoPath
    {
        get => _settingsPrintLogoPath;
        set => SetProperty(ref _settingsPrintLogoPath, value);
    }

    public string SettingsPrintTemplate
    {
        get => _settingsPrintTemplate;
        set => SetProperty(ref _settingsPrintTemplate, value);
    }

    public string SettingsDecimalSeparator
    {
        get => _settingsDecimalSeparator;
        set => SetProperty(ref _settingsDecimalSeparator, NumericFormatManager.NormalizeDecimalSeparator(value));
    }

    public string SettingsThousandsSeparator
    {
        get => _settingsThousandsSeparator;
        set => SetProperty(ref _settingsThousandsSeparator, ToThousandsSeparatorLabel(value));
    }

    public string SettingsCurrencyCode
    {
        get => _settingsCurrencyCode;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? Payroll.Domain.Settings.PayrollSettings.DefaultCurrencyCode
                : value.Trim().ToUpperInvariant();
            if (SetProperty(ref _settingsCurrencyCode, normalized))
            {
                RaisePropertyChanged(nameof(CurrencyPrefix));
            }
        }
    }

    public string CurrencyPrefix => $"{SettingsCurrencyCode} ";

    public string PersonImportCsvFilePath
    {
        get => _personImportCsvFilePath;
        set
        {
            if (SetProperty(ref _personImportCsvFilePath, value))
            {
                RaiseImportCommandState();
            }
        }
    }

    public string SelectedPersonImportDelimiter
    {
        get => _selectedPersonImportDelimiter;
        set
        {
            if (SetProperty(ref _selectedPersonImportDelimiter, string.IsNullOrWhiteSpace(value) ? "Semikolon (;)" : value))
            {
                RaiseImportCommandState();
            }
        }
    }

    public bool PersonImportFieldsEnclosed
    {
        get => _personImportFieldsEnclosed;
        set => SetProperty(ref _personImportFieldsEnclosed, value);
    }

    public string SelectedPersonImportTextQualifier
    {
        get => _selectedPersonImportTextQualifier;
        set => SetProperty(ref _selectedPersonImportTextQualifier, value);
    }

    public string PersonImportConfigurationName
    {
        get => _personImportConfigurationName;
        set
        {
            if (SetProperty(ref _personImportConfigurationName, value))
            {
                RaisePropertyChanged(nameof(CanSavePersonImportConfiguration));
                SavePersonImportConfigurationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ImportConfigurationItemViewModel? SelectedPersonImportConfiguration
    {
        get => _selectedPersonImportConfiguration;
        set
        {
            if (SetProperty(ref _selectedPersonImportConfiguration, value))
            {
                RaisePropertyChanged(nameof(CanLoadPersonImportConfiguration));
                LoadPersonImportConfigurationCommand.RaiseCanExecuteChanged();

                if (value is not null && IsSettingsWorkspace && !IsBusy)
                {
                    _ = LoadSelectedPersonImportConfigurationAsync();
                }
            }
        }
    }

    public string PersonImportStatusMessage
    {
        get => _personImportStatusMessage;
        set => SetProperty(ref _personImportStatusMessage, value);
    }

    public string TimeImportCsvFilePath
    {
        get => _timeImportCsvFilePath;
        set
        {
            if (SetProperty(ref _timeImportCsvFilePath, value))
            {
                RaiseImportCommandState();
            }
        }
    }

    public string SelectedTimeImportDelimiter
    {
        get => _selectedTimeImportDelimiter;
        set
        {
            if (SetProperty(ref _selectedTimeImportDelimiter, string.IsNullOrWhiteSpace(value) ? "Semikolon (;)" : value))
            {
                RaiseImportCommandState();
            }
        }
    }

    public bool TimeImportFieldsEnclosed
    {
        get => _timeImportFieldsEnclosed;
        set => SetProperty(ref _timeImportFieldsEnclosed, value);
    }

    public string SelectedTimeImportTextQualifier
    {
        get => _selectedTimeImportTextQualifier;
        set => SetProperty(ref _selectedTimeImportTextQualifier, value);
    }

    public string TimeImportConfigurationName
    {
        get => _timeImportConfigurationName;
        set
        {
            if (SetProperty(ref _timeImportConfigurationName, value))
            {
                RaisePropertyChanged(nameof(CanSaveTimeImportConfiguration));
                SaveTimeImportConfigurationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ImportConfigurationItemViewModel? SelectedTimeImportConfiguration
    {
        get => _selectedTimeImportConfiguration;
        set
        {
            if (SetProperty(ref _selectedTimeImportConfiguration, value))
            {
                RaisePropertyChanged(nameof(CanLoadTimeImportConfiguration));
                LoadTimeImportConfigurationCommand.RaiseCanExecuteChanged();

                if (value is not null && IsSettingsWorkspace && !IsBusy)
                {
                    _ = LoadSelectedTimeImportConfigurationAsync();
                }
            }
        }
    }

    public DateTimeOffset? TimeImportMonth
    {
        get => _timeImportMonth;
        set
        {
            var normalized = value.HasValue
                ? new DateTimeOffset(new DateTime(value.Value.Year, value.Value.Month, 1))
                : (DateTimeOffset?)null;
            if (SetProperty(ref _timeImportMonth, normalized))
            {
                RaiseImportCommandState();
            }
        }
    }

    public ImportedMonthStatusItemViewModel? SelectedImportedTimeMonth
    {
        get => _selectedImportedTimeMonth;
        set
        {
            if (SetProperty(ref _selectedImportedTimeMonth, value))
            {
                RaisePropertyChanged(nameof(CanDeleteImportedTimeMonth));
                DeleteImportedTimeMonthCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string TimeImportStatusMessage
    {
        get => _timeImportStatusMessage;
        set => SetProperty(ref _timeImportStatusMessage, value);
    }

    public bool PersonImportHasCsvHeaders => PersonImportFieldMappings.Any(row => row.AvailableCsvColumns.Count > 1);
    public bool TimeImportHasCsvHeaders => TimeImportFieldMappings.Any(row => row.AvailableCsvColumns.Count > 1);
    public bool HasPersonImportPreviewItems => PersonImportPreviewItems.Count > 0;

    public IReadOnlyCollection<string> PersonImportValidationErrors => _importService.ValidateMappings(
        Payroll.Domain.Imports.ImportConfigurationType.PersonData,
        PersonImportFieldMappings.SelectMany(row => row.AvailableCsvColumns).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        BuildPersonImportFieldMappings()).Errors;

    public IReadOnlyCollection<string> TimeImportValidationErrors => _importService.ValidateMappings(
        Payroll.Domain.Imports.ImportConfigurationType.TimeData,
        TimeImportFieldMappings.SelectMany(row => row.AvailableCsvColumns).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        BuildTimeImportFieldMappings()).Errors;

    public string BackupDirectoryPath
    {
        get => _backupDirectoryPath;
        set
        {
            if (SetProperty(ref _backupDirectoryPath, value))
            {
                RaisePropertyChanged(nameof(CanCreateBackup));
                CreateBackupCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string BackupFileName
    {
        get => _backupFileName;
        set
        {
            if (SetProperty(ref _backupFileName, value))
            {
                RaisePropertyChanged(nameof(CanCreateBackup));
                CreateBackupCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedBackupContentType
    {
        get => _selectedBackupContentType;
        set => SetProperty(ref _selectedBackupContentType, value);
    }

    public string RestoreFilePath
    {
        get => _restoreFilePath;
        set
        {
            if (SetProperty(ref _restoreFilePath, value))
            {
                RaisePropertyChanged(nameof(CanRestoreBackup));
                RestoreBackupCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedRestoreContentType
    {
        get => _selectedRestoreContentType;
        set => SetProperty(ref _selectedRestoreContentType, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(CanSearchEmployees));
                RaisePropertyChanged(nameof(CanEditFields));
                RaisePropertyChanged(nameof(CanBrowseEmployees));
                RaisePropertyChanged(nameof(CanStartCreate));
                RaisePropertyChanged(nameof(CanStartEdit));
                RaisePropertyChanged(nameof(CanSave));
                RaisePropertyChanged(nameof(CanCancel));
                RaisePropertyChanged(nameof(CanRequestDelete));
                RaisePropertyChanged(nameof(CanConfirmDelete));
                RaisePropertyChanged(nameof(CanDismissDelete));
                RaisePropertyChanged(nameof(CanSaveSettings));
                RaisePropertyChanged(nameof(CanLoadPersonImportCsv));
                RaisePropertyChanged(nameof(CanSavePersonImportConfiguration));
                RaisePropertyChanged(nameof(CanLoadPersonImportConfiguration));
                RaisePropertyChanged(nameof(CanImportPersonData));
                RaisePropertyChanged(nameof(CanCreateBackup));
                RaisePropertyChanged(nameof(CanRestoreBackup));
                RaisePropertyChanged(nameof(CanCreatePayrollPdf));
                RaisePropertyChanged(nameof(CanManageSettingsOptions));
                RaisePropertyChanged(nameof(CanRemoveDepartmentOption));
                RaisePropertyChanged(nameof(CanRemoveEmploymentCategoryOption));
                RaisePropertyChanged(nameof(CanRemoveEmploymentLocationOption));
                RaiseActionStateChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        await SqlExplorer.InitializeAsync();
        await LoadSettingsAsync();
        await LoadImportConfigurationsAsync();
        await LoadImportedTimeMonthsAsync();
        await RefreshAsync();
        await LoadMonthCaptureOverviewAsync();

        if (Employees.Count > 0 && _currentEmployeeId is null)
        {
            SelectedEmployee = Employees[0];
        }
    }

    private async Task RefreshAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var selectedEmployeeId = _currentEmployeeId;
            await ReloadEmployeesAsync();
            await RestoreSelectionAfterReloadAsync(selectedEmployeeId, selectFirstIfMissing: !_isEditing);
            await LoadMonthCaptureOverviewAsync();
            StatusMessage = $"{Employees.Count} Mitarbeitende geladen.";
        });
    }

    private void BeginCreateEmployee()
    {
        SwitchToEmployeesWorkspace();
        DismissDeleteConfirmation();
        DismissContractVersionDialog();
        DismissSettingsVersionDialog();
        _returnEmployeeId = _currentEmployeeId;
        SelectedEmployee = null;
        _currentEmployeeId = null;
        MonthlyRecord.Reset();
        ResetFormForNewDraft();
        SetInteractionState(isEditing: true, isCreatingNew: true);
        StatusMessage = "Neuer Mitarbeitender. Daten erfassen und speichern.";
    }

    private void BeginEditEmployee()
    {
        if (!_currentEmployeeId.HasValue)
        {
            return;
        }

        SwitchToEmployeesWorkspace();
        DismissDeleteConfirmation();
        DismissContractVersionDialog();
        DismissSettingsVersionDialog();
        _returnEmployeeId = _currentEmployeeId;
        SetInteractionState(isEditing: true, isCreatingNew: false);
        StatusMessage = "Bearbeitungsmodus aktiv. Aenderungen mit Speichern uebernehmen oder mit Abbrechen verwerfen.";
    }

    private async Task LoadEmployeeAsync(Guid employeeId)
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!await LoadEmployeeIntoViewAsync(employeeId))
            {
                ClearFormForEmptyState();
                StatusMessage = "Mitarbeitender wurde nicht gefunden.";
                return;
            }

            StatusMessage = $"Mitarbeitender {PersonnelNumber} geladen.";
        });
    }

    private async Task SaveAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!EntryDate.HasValue)
            {
                throw new InvalidOperationException("Eintrittsdatum ist erforderlich.");
            }

            if (!ContractValidFrom.HasValue)
            {
                throw new InvalidOperationException("Gueltig ab ist erforderlich.");
            }

            var command = new SaveEmployeeCommand(
                _currentEmployeeId,
                PersonnelNumber,
                FirstName,
                LastName,
                BirthDate.HasValue ? DateOnly.FromDateTime(BirthDate.Value.Date) : null,
                DateOnly.FromDateTime(EntryDate.Value.Date),
                ExitDate.HasValue ? DateOnly.FromDateTime(ExitDate.Value.Date) : null,
                IsActiveEmployee,
                Street,
                HouseNumber,
                AddressLine2,
                PostalCode,
                City,
                Country,
                ResidenceCountry,
                Nationality,
                PermitCode,
                TaxStatus,
                ParseOptionalBoolean(SelectedWithholdingTaxOption),
                AhvNumber,
                Iban,
                PhoneNumber,
                Email,
                SelectedDepartmentOption?.OptionId,
                SelectedEmploymentCategoryOption?.OptionId,
                SelectedEmploymentLocationOption?.OptionId,
                MapSelectedWageType(),
                _loadedCurrentContractId,
                DateOnly.FromDateTime(ContractValidFrom.Value.Date),
                ContractValidTo.HasValue ? DateOnly.FromDateTime(ContractValidTo.Value.Date) : null,
                ParseRequiredDecimal(HourlyRateChf, nameof(HourlyRateChf)),
                ParseRequiredDecimal(MonthlyBvgDeductionChf, nameof(MonthlyBvgDeductionChf)),
                ParseRequiredDecimal(SpecialSupplementRateChf, nameof(SpecialSupplementRateChf)));

            var saved = await _employeeService.SaveAsync(command);
            _currentEmployeeId = saved.EmployeeId;
            _returnEmployeeId = saved.EmployeeId;
            SetInteractionState(isEditing: false, isCreatingNew: false);
            await ReloadEmployeesAsync();
            await RestoreSelectionAfterReloadAsync(saved.EmployeeId, selectFirstIfMissing: true);
            StatusMessage = $"Mitarbeitender {saved.PersonnelNumber} gespeichert.";
        });
    }

    private async Task CancelAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            DismissDeleteConfirmation();
            DismissContractVersionDialog();
            DismissSettingsVersionDialog();

            if (_isCreatingNew)
            {
                await ReloadEmployeesAsync();
                await RestoreSelectionAfterReloadAsync(_returnEmployeeId, selectFirstIfMissing: true);
                if (_currentEmployeeId is null)
                {
                    ClearFormForEmptyState();
                }

                SetInteractionState(isEditing: false, isCreatingNew: false);
                StatusMessage = "Neueingabe verworfen.";
                return;
            }

            if (_currentEmployeeId.HasValue)
            {
                var employee = await _employeeService.GetByIdAsync(_currentEmployeeId.Value);
                if (employee is not null)
                {
                    PopulateForm(employee);
                }
            }

            SetInteractionState(isEditing: false, isCreatingNew: false);
            StatusMessage = "Bearbeitung abgebrochen.";
        });
    }

    private void RequestDelete()
    {
        ShowDeleteConfirmation = true;
        StatusMessage = "Sicherheitsabfrage aktiv. Die Loeschaktion archiviert den Datensatz.";
    }

    private void DismissDeleteConfirmation()
    {
        ShowDeleteConfirmation = false;
    }

    private void DismissContractVersionDialog()
    {
        ShowContractVersionDialog = false;
        _showContractVersionCreateSection = false;
        NewContractVersionValidFrom = null;
        NewContractVersionValidTo = null;
        SelectedContractHistoryEntry = null;
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
    }

    private void OpenContractVersionDialogFromButton()
    {
        if (!_currentEmployeeId.HasValue)
        {
            return;
        }

        DismissDeleteConfirmation();
        _showContractVersionCreateSection = false;
        SelectedContractHistoryEntry = ContractHistory.FirstOrDefault(item => item.IsCurrent) ?? ContractHistory.FirstOrDefault();
        ShowContractVersionDialog = true;
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = "Vertragshistorie geoeffnet.";
    }

    private void OpenNewContractVersionDialogFromButton()
    {
        if (!_currentEmployeeId.HasValue || !_isEditing)
        {
            return;
        }

        DismissDeleteConfirmation();
        _showContractVersionCreateSection = true;
        SelectedContractHistoryEntry = null;
        NewContractVersionValidFrom = DetermineSuggestedNewContractVersionValidFrom();
        NewContractVersionValidTo = null;
        ShowContractVersionDialog = true;
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = "Neuer Vertragsstand wird vorbereitet. Gueltigkeit bestaetigen und anschliessend speichern.";
    }

    private async Task ConfirmContractVersionDialogAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!_currentEmployeeId.HasValue)
            {
                return;
            }

            if (!NewContractVersionValidFrom.HasValue)
            {
                throw new InvalidOperationException("Gueltig ab fuer den neuen Vertragsstand ist erforderlich.");
            }

            if (NewContractVersionValidTo.HasValue && NewContractVersionValidTo.Value.Date < NewContractVersionValidFrom.Value.Date)
            {
                throw new InvalidOperationException("Gueltig bis darf nicht vor Gueltig ab liegen.");
            }

            var command = new SaveEmployeeCommand(
                _currentEmployeeId,
                PersonnelNumber,
                FirstName,
                LastName,
                BirthDate.HasValue ? DateOnly.FromDateTime(BirthDate.Value.Date) : null,
                DateOnly.FromDateTime(EntryDate!.Value.Date),
                ExitDate.HasValue ? DateOnly.FromDateTime(ExitDate.Value.Date) : null,
                IsActiveEmployee,
                Street,
                HouseNumber,
                AddressLine2,
                PostalCode,
                City,
                Country,
                ResidenceCountry,
                Nationality,
                PermitCode,
                TaxStatus,
                ParseOptionalBoolean(SelectedWithholdingTaxOption),
                AhvNumber,
                Iban,
                PhoneNumber,
                Email,
                SelectedDepartmentOption?.OptionId,
                SelectedEmploymentCategoryOption?.OptionId,
                SelectedEmploymentLocationOption?.OptionId,
                MapSelectedWageType(),
                null,
                DateOnly.FromDateTime(NewContractVersionValidFrom.Value.Date),
                NewContractVersionValidTo.HasValue ? DateOnly.FromDateTime(NewContractVersionValidTo.Value.Date) : null,
                ParseRequiredDecimal(HourlyRateChf, nameof(HourlyRateChf)),
                ParseRequiredDecimal(MonthlyBvgDeductionChf, nameof(MonthlyBvgDeductionChf)),
                ParseRequiredDecimal(SpecialSupplementRateChf, nameof(SpecialSupplementRateChf)));

            var saved = await _employeeService.SaveAsync(command);
            _currentEmployeeId = saved.EmployeeId;
            _returnEmployeeId = saved.EmployeeId;
            DismissContractVersionDialog();
            SetInteractionState(isEditing: false, isCreatingNew: false);
            await ReloadEmployeesAsync();
            await RestoreSelectionAfterReloadAsync(saved.EmployeeId, selectFirstIfMissing: true);
            StatusMessage = $"Mitarbeitender {saved.PersonnelNumber} gespeichert.";
        });
    }

    private void DismissSettingsVersionDialog()
    {
        ShowSettingsVersionDialog = false;
        _showSettingsVersionCreateSection = false;
        NewSettingsVersionValidFrom = null;
        NewSettingsVersionValidTo = null;
        SelectedSettingsVersionHistoryEntry = null;
        SettingsVersionHistory.Clear();
        RaisePropertyChanged(nameof(ShowSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmSettingsVersionDialog));
        RaisePropertyChanged(nameof(SettingsVersionDialogTitle));
        RaisePropertyChanged(nameof(SettingsVersionDialogDescription));
        RaisePropertyChanged(nameof(SettingsVersionDialogSummary));
        ConfirmSettingsVersionDialogCommand.RaiseCanExecuteChanged();
    }

    private void OpenGeneralSettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaGeneral, createMode: false);
    private void OpenNewGeneralSettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaGeneral, createMode: true);
    private void OpenHourlySettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaHourly, createMode: false);
    private void OpenNewHourlySettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaHourly, createMode: true);
    private void OpenMonthlySalarySettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaMonthlySalary, createMode: false);
    private void OpenNewMonthlySalarySettingsVersionDialogFromButton() => OpenSettingsVersionDialog(SettingsVersionAreaMonthlySalary, createMode: true);

    private void OpenSettingsVersionDialog(string area, bool createMode)
    {
        if (!IsSettingsWorkspace)
        {
            return;
        }

        DismissDeleteConfirmation();
        _selectedSettingsVersionArea = area;
        _showSettingsVersionCreateSection = createMode;
        NewSettingsVersionValidFrom = createMode ? DetermineSuggestedNewSettingsVersionValidFrom(area) : null;
        NewSettingsVersionValidTo = null;
        ApplySettingsVersionHistory(area);
        SelectedSettingsVersionHistoryEntry = SettingsVersionHistory.FirstOrDefault(item => item.IsCurrent) ?? SettingsVersionHistory.FirstOrDefault();
        ShowSettingsVersionDialog = true;
        RaisePropertyChanged(nameof(ShowSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmSettingsVersionDialog));
        RaisePropertyChanged(nameof(SettingsVersionDialogTitle));
        RaisePropertyChanged(nameof(SettingsVersionDialogDescription));
        RaisePropertyChanged(nameof(SelectedSettingsVersionAreaLabel));
        RaisePropertyChanged(nameof(SettingsVersionDialogSummary));
        ConfirmSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = createMode
            ? $"Neuer Satzstand fuer {area} wird vorbereitet."
            : $"{area}-Historie geoeffnet.";
    }

    private async Task ConfirmSettingsVersionDialogAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!NewSettingsVersionValidFrom.HasValue)
            {
                throw new InvalidOperationException("Gueltig ab fuer den neuen Satzstand ist erforderlich.");
            }

            if (NewSettingsVersionValidTo.HasValue && NewSettingsVersionValidTo.Value.Date < NewSettingsVersionValidFrom.Value.Date)
            {
                throw new InvalidOperationException("Gueltig bis darf nicht vor Gueltig ab liegen.");
            }

            var saved = await _payrollSettingsService.SaveAsync(BuildSavePayrollSettingsCommand(
                editingGeneralSettingsVersionId: _selectedSettingsVersionArea == SettingsVersionAreaGeneral ? null : _loadedCurrentGeneralSettingsVersionId,
                generalSettingsValidFromOverride: _selectedSettingsVersionArea == SettingsVersionAreaGeneral ? NewSettingsVersionValidFrom : SettingsGeneralValidFrom,
                generalSettingsValidToOverride: _selectedSettingsVersionArea == SettingsVersionAreaGeneral ? NewSettingsVersionValidTo : SettingsGeneralValidTo,
                editingHourlySettingsVersionId: _selectedSettingsVersionArea == SettingsVersionAreaHourly ? null : _loadedCurrentHourlySettingsVersionId,
                hourlySettingsValidFromOverride: _selectedSettingsVersionArea == SettingsVersionAreaHourly ? NewSettingsVersionValidFrom : SettingsHourlyValidFrom,
                hourlySettingsValidToOverride: _selectedSettingsVersionArea == SettingsVersionAreaHourly ? NewSettingsVersionValidTo : SettingsHourlyValidTo,
                editingMonthlySalarySettingsVersionId: _selectedSettingsVersionArea == SettingsVersionAreaMonthlySalary ? null : _loadedCurrentMonthlySalarySettingsVersionId,
                monthlySalarySettingsValidFromOverride: _selectedSettingsVersionArea == SettingsVersionAreaMonthlySalary ? NewSettingsVersionValidFrom : SettingsMonthlySalaryValidFrom,
                monthlySalarySettingsValidToOverride: _selectedSettingsVersionArea == SettingsVersionAreaMonthlySalary ? NewSettingsVersionValidTo : SettingsMonthlySalaryValidTo));

            ApplySettings(saved);
            DismissSettingsVersionDialog();
            await MonthlyRecord.ReloadCurrentMonthAsync();
            StatusMessage = "Einstellungen gespeichert.";
        });
    }

    private void ClearExitDate()
    {
        ExitDate = null;
    }

    private void AddDepartmentOption()
    {
        AddSettingOption(DepartmentOptions, ref _selectedDepartmentOption, ref _selectedSettingsDepartment, nameof(SelectedDepartmentOption), nameof(SelectedSettingsDepartment), ref _newDepartmentName, nameof(NewDepartmentName));
        RaisePropertyChanged(nameof(CanRemoveDepartmentOption));
        RemoveDepartmentOptionCommand.RaiseCanExecuteChanged();
    }

    private void RemoveDepartmentOption()
    {
        RemoveSettingOption(DepartmentOptions, SelectedSettingsDepartment, ref _selectedSettingsDepartment, nameof(SelectedSettingsDepartment), ref _selectedDepartmentOption, nameof(SelectedDepartmentOption));
        RaisePropertyChanged(nameof(CanRemoveDepartmentOption));
        RemoveDepartmentOptionCommand.RaiseCanExecuteChanged();
    }

    private void AddEmploymentCategoryOption()
    {
        AddSettingOption(EmploymentCategoryOptions, ref _selectedEmploymentCategoryOption, ref _selectedSettingsEmploymentCategory, nameof(SelectedEmploymentCategoryOption), nameof(SelectedSettingsEmploymentCategory), ref _newEmploymentCategoryName, nameof(NewEmploymentCategoryName));
        RaisePropertyChanged(nameof(CanRemoveEmploymentCategoryOption));
        RemoveEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
    }

    private void RemoveEmploymentCategoryOption()
    {
        RemoveSettingOption(EmploymentCategoryOptions, SelectedSettingsEmploymentCategory, ref _selectedSettingsEmploymentCategory, nameof(SelectedSettingsEmploymentCategory), ref _selectedEmploymentCategoryOption, nameof(SelectedEmploymentCategoryOption));
        RaisePropertyChanged(nameof(CanRemoveEmploymentCategoryOption));
        RemoveEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
    }

    private void AddEmploymentLocationOption()
    {
        AddSettingOption(EmploymentLocationOptions, ref _selectedEmploymentLocationOption, ref _selectedSettingsEmploymentLocation, nameof(SelectedEmploymentLocationOption), nameof(SelectedSettingsEmploymentLocation), ref _newEmploymentLocationName, nameof(NewEmploymentLocationName));
        RaisePropertyChanged(nameof(CanRemoveEmploymentLocationOption));
        RemoveEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
    }

    private void RemoveEmploymentLocationOption()
    {
        RemoveSettingOption(EmploymentLocationOptions, SelectedSettingsEmploymentLocation, ref _selectedSettingsEmploymentLocation, nameof(SelectedSettingsEmploymentLocation), ref _selectedEmploymentLocationOption, nameof(SelectedEmploymentLocationOption));
        RaisePropertyChanged(nameof(CanRemoveEmploymentLocationOption));
        RemoveEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
    }

    private async Task ConfirmDeleteAsync()
    {
        if (!_currentEmployeeId.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var archivedEmployeeId = _currentEmployeeId.Value;
            await _employeeService.ArchiveAsync(archivedEmployeeId);
            DismissDeleteConfirmation();
            await ReloadEmployeesAsync();
            await RestoreSelectionAfterReloadAsync(archivedEmployeeId, selectFirstIfMissing: true);
            StatusMessage = "Mitarbeitender archiviert und auf inaktiv gesetzt.";
        });
    }

    private async Task SaveSettingsAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var saved = await _payrollSettingsService.SaveAsync(BuildSavePayrollSettingsCommand());

            ApplySettings(saved);
            await MonthlyRecord.ReloadCurrentMonthAsync();
            StatusMessage = "Einstellungen gespeichert.";
        });
    }

    private SavePayrollSettingsCommand BuildSavePayrollSettingsCommand(
        Guid? editingGeneralSettingsVersionId = null,
        DateTimeOffset? generalSettingsValidFromOverride = null,
        DateTimeOffset? generalSettingsValidToOverride = null,
        Guid? editingHourlySettingsVersionId = null,
        DateTimeOffset? hourlySettingsValidFromOverride = null,
        DateTimeOffset? hourlySettingsValidToOverride = null,
        Guid? editingMonthlySalarySettingsVersionId = null,
        DateTimeOffset? monthlySalarySettingsValidFromOverride = null,
        DateTimeOffset? monthlySalarySettingsValidToOverride = null)
    {
        var generalValidFrom = generalSettingsValidFromOverride ?? SettingsGeneralValidFrom ?? StartOfCurrentMonth();
        var hourlyValidFrom = hourlySettingsValidFromOverride ?? SettingsHourlyValidFrom ?? StartOfCurrentMonth();
        var monthlySalaryValidFrom = monthlySalarySettingsValidFromOverride ?? SettingsMonthlySalaryValidFrom ?? StartOfCurrentMonth();

        return new SavePayrollSettingsCommand(
            SettingsCompanyAddress,
            SettingsAppFontFamily,
            ParseRequiredDecimal(SettingsAppFontSize, nameof(SettingsAppFontSize)),
            SettingsAppTextColorHex,
            SettingsAppMutedTextColorHex,
            SettingsAppBackgroundColorHex,
            SettingsAppAccentColorHex,
            SettingsAppLogoText,
            SettingsAppLogoPath,
            SettingsPrintFontFamily,
            ParseRequiredDecimal(SettingsPrintFontSize, nameof(SettingsPrintFontSize)),
            SettingsPrintTextColorHex,
            SettingsPrintMutedTextColorHex,
            SettingsPrintAccentColorHex,
            SettingsPrintLogoText,
            SettingsPrintLogoPath,
            SettingsPrintTemplate,
            SettingsDecimalSeparator,
            ToThousandsSeparatorValue(SettingsThousandsSeparator),
            SettingsCurrencyCode,
            ParseOptionalPercentage(SettingsNightSupplementRate),
            ParseOptionalPercentage(SettingsSundaySupplementRate),
            ParseOptionalPercentage(SettingsHolidaySupplementRate),
            ParseRequiredPercentage(SettingsAhvIvEoRate, nameof(SettingsAhvIvEoRate)),
            ParseRequiredPercentage(SettingsAlvRate, nameof(SettingsAlvRate)),
            ParseRequiredPercentage(SettingsSicknessAccidentInsuranceRate, nameof(SettingsSicknessAccidentInsuranceRate)),
            ParseRequiredPercentage(SettingsTrainingAndHolidayRate, nameof(SettingsTrainingAndHolidayRate)),
            ParseRequiredPercentage(SettingsVacationCompensationRate, nameof(SettingsVacationCompensationRate)),
            ParseRequiredPercentage(SettingsVacationCompensationRateAge50Plus, nameof(SettingsVacationCompensationRateAge50Plus)),
            ParseRequiredDecimal(SettingsVehiclePauschalzone1RateChf, nameof(SettingsVehiclePauschalzone1RateChf)),
            ParseRequiredDecimal(SettingsVehiclePauschalzone2RateChf, nameof(SettingsVehiclePauschalzone2RateChf)),
            ParseRequiredDecimal(SettingsVehicleRegiezone1RateChf, nameof(SettingsVehicleRegiezone1RateChf)),
            BuildPayrollPreviewHelpOptionDtos(PayrollPreviewHelpOptions),
            BuildSettingOptionDtos(DepartmentOptions),
            BuildSettingOptionDtos(EmploymentCategoryOptions),
            BuildSettingOptionDtos(EmploymentLocationOptions),
            editingGeneralSettingsVersionId ?? _loadedCurrentGeneralSettingsVersionId,
            DateOnly.FromDateTime(generalValidFrom.Date),
            generalSettingsValidToOverride.HasValue ? DateOnly.FromDateTime(generalSettingsValidToOverride.Value.Date) : SettingsGeneralValidTo.HasValue && generalSettingsValidToOverride is null ? DateOnly.FromDateTime(SettingsGeneralValidTo.Value.Date) : null,
            editingHourlySettingsVersionId ?? _loadedCurrentHourlySettingsVersionId,
            DateOnly.FromDateTime(hourlyValidFrom.Date),
            hourlySettingsValidToOverride.HasValue ? DateOnly.FromDateTime(hourlySettingsValidToOverride.Value.Date) : SettingsHourlyValidTo.HasValue && hourlySettingsValidToOverride is null ? DateOnly.FromDateTime(SettingsHourlyValidTo.Value.Date) : null,
            editingMonthlySalarySettingsVersionId ?? _loadedCurrentMonthlySalarySettingsVersionId,
            DateOnly.FromDateTime(monthlySalaryValidFrom.Date),
            monthlySalarySettingsValidToOverride.HasValue ? DateOnly.FromDateTime(monthlySalarySettingsValidToOverride.Value.Date) : SettingsMonthlySalaryValidTo.HasValue && monthlySalarySettingsValidToOverride is null ? DateOnly.FromDateTime(SettingsMonthlySalaryValidTo.Value.Date) : null);
    }

    private async Task LoadPersonImportCsvAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var document = await _importService.ReadCsvDocumentAsync(new ReadCsvImportDocumentCommand(
                PersonImportCsvFilePath,
                ResolvePersonImportDelimiter(),
                PersonImportFieldsEnclosed,
                ResolvePersonImportTextQualifier()));

            ApplyPersonImportCsvHeaders(document.Headers);
            ClearPersonImportPreview();
            PersonImportStatusMessage = $"Datei geladen: {document.Headers.Count} Spalten erkannt, {document.Rows.Count} Datenzeilen.";
            StatusMessage = PersonImportStatusMessage;
            RaiseImportCommandState();
        });
    }

    private async Task SavePersonImportConfigurationAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var saved = await _importService.SaveConfigurationAsync(new SaveImportConfigurationCommand(
                SelectedPersonImportConfiguration?.ConfigurationId,
                Payroll.Domain.Imports.ImportConfigurationType.PersonData,
                PersonImportConfigurationName,
                ResolvePersonImportDelimiter(),
                PersonImportFieldsEnclosed,
                ResolvePersonImportTextQualifier(),
                BuildPersonImportFieldMappings()));

            await LoadImportConfigurationsAsync();
            SelectedPersonImportConfiguration = PersonImportConfigurations.FirstOrDefault(item => item.ConfigurationId == saved.ConfigurationId);
            PersonImportConfigurationName = saved.Name;
            PersonImportStatusMessage = $"Mapping `{saved.Name}` gespeichert.";
            StatusMessage = PersonImportStatusMessage;
        });
    }

    private async Task LoadSelectedPersonImportConfigurationAsync()
    {
        if (SelectedPersonImportConfiguration is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var configuration = await _importService.GetConfigurationAsync(SelectedPersonImportConfiguration.ConfigurationId);
            if (configuration is null)
            {
                PersonImportStatusMessage = "Mapping-Konfiguration wurde nicht gefunden.";
                return;
            }

            ApplyPersonImportConfiguration(configuration);
            ClearPersonImportPreview();
            PersonImportStatusMessage = $"Mapping `{configuration.Name}` geladen.";
            StatusMessage = PersonImportStatusMessage;
        });
    }

    public async Task<bool> PreparePersonImportPreviewAsync()
    {
        var prepared = false;
        await ExecuteBusyAsync(async () =>
        {
            await RefreshPersonImportPreviewAsync();
            if (PersonImportPreviewItems.Count == 0)
            {
                PersonImportStatusMessage = "Keine importierbaren Personendaten in der CSV gefunden.";
                StatusMessage = PersonImportStatusMessage;
                return;
            }

            prepared = true;
        });

        return prepared;
    }

    public async Task ImportSelectedPersonDataAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var selectedRowNumbers = PersonImportPreviewItems
                .Where(item => item.IsSelected)
                .Select(item => item.RowNumber)
                .ToArray();

            var result = await _importService.ImportPersonDataAsync(new ImportPersonDataCommand(
                PersonImportCsvFilePath,
                ResolvePersonImportDelimiter(),
                PersonImportFieldsEnclosed,
                ResolvePersonImportTextQualifier(),
                BuildPersonImportFieldMappings(),
                selectedRowNumbers));

            var summary = $"{result.CreatedCount} neu, {result.UpdatedCount} aktualisiert";
            PersonImportStatusMessage = result.ErrorCount > 0
                ? BuildPersonImportStatusMessage(summary, result)
                : $"{summary}.";
            StatusMessage = PersonImportStatusMessage;

            if (result.CreatedCount > 0 || result.UpdatedCount > 0)
            {
                await ReloadEmployeesAsync();
            }

            ClearPersonImportPreview();
        });
    }

    private async Task LoadTimeImportCsvAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var document = await _importService.ReadCsvDocumentAsync(new ReadCsvImportDocumentCommand(
                TimeImportCsvFilePath,
                ResolveTimeImportDelimiter(),
                TimeImportFieldsEnclosed,
                ResolveTimeImportTextQualifier()));

            ApplyTimeImportCsvHeaders(document.Headers);
            TimeImportStatusMessage = $"Datei geladen: {document.Headers.Count} Spalten erkannt, {document.Rows.Count} Datenzeilen.";
            StatusMessage = TimeImportStatusMessage;
            RaiseImportCommandState();
        });
    }

    private async Task SaveTimeImportConfigurationAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var saved = await _importService.SaveConfigurationAsync(new SaveImportConfigurationCommand(
                SelectedTimeImportConfiguration?.ConfigurationId,
                Payroll.Domain.Imports.ImportConfigurationType.TimeData,
                TimeImportConfigurationName,
                ResolveTimeImportDelimiter(),
                TimeImportFieldsEnclosed,
                ResolveTimeImportTextQualifier(),
                BuildTimeImportFieldMappings()));

            await LoadImportConfigurationsAsync();
            SelectedTimeImportConfiguration = TimeImportConfigurations.FirstOrDefault(item => item.ConfigurationId == saved.ConfigurationId);
            TimeImportConfigurationName = saved.Name;
            TimeImportStatusMessage = $"Mapping `{saved.Name}` gespeichert.";
            StatusMessage = TimeImportStatusMessage;
        });
    }

    private async Task LoadSelectedTimeImportConfigurationAsync()
    {
        if (SelectedTimeImportConfiguration is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var configuration = await _importService.GetConfigurationAsync(SelectedTimeImportConfiguration.ConfigurationId);
            if (configuration is null)
            {
                TimeImportStatusMessage = "Mapping-Konfiguration wurde nicht gefunden.";
                return;
            }

            ApplyTimeImportConfiguration(configuration);
            TimeImportStatusMessage = $"Mapping `{configuration.Name}` geladen.";
            StatusMessage = TimeImportStatusMessage;
        });
    }

    public async Task ImportTimeDataAsync(bool overwriteExistingMonth = false)
    {
        if (!TimeImportMonth.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var month = TimeImportMonth.Value;
            var result = await _importService.ImportTimeDataAsync(new ImportTimeDataCommand(
                TimeImportCsvFilePath,
                ResolveTimeImportDelimiter(),
                TimeImportFieldsEnclosed,
                ResolveTimeImportTextQualifier(),
                month.Year,
                month.Month,
                overwriteExistingMonth,
                BuildTimeImportFieldMappings()));

            TimeImportStatusMessage = result.ErrorCount > 0
                ? BuildTimeImportStatusMessage(result)
                : $"{result.ImportedCount} importiert.";
            StatusMessage = TimeImportStatusMessage;
            await LoadImportedTimeMonthsAsync();
            await LoadMonthCaptureOverviewAsync();
        });
    }

    public async Task<bool> IsSelectedTimeImportMonthAlreadyImportedAsync()
    {
        if (!TimeImportMonth.HasValue)
        {
            return false;
        }

        var month = TimeImportMonth.Value;
        return await _importService.IsMonthImportedAsync(Payroll.Domain.Imports.ImportConfigurationType.TimeData, month.Year, month.Month);
    }

    public async Task DeleteImportedTimeMonthAsync()
    {
        if (SelectedImportedTimeMonth is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await _importService.DeleteImportedTimeMonthAsync(SelectedImportedTimeMonth.Year, SelectedImportedTimeMonth.Month);
            TimeImportStatusMessage = $"Importierter Monat {SelectedImportedTimeMonth.DisplayName} geloescht.";
            StatusMessage = TimeImportStatusMessage;
            await LoadImportedTimeMonthsAsync();
            await LoadMonthCaptureOverviewAsync();
        });
    }

    private async Task CreatePayrollPdfAsync()
    {
        if (!_currentEmployeeId.HasValue || !MonthlyRecord.SelectedMonth.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var selectedMonth = MonthlyRecord.SelectedMonth.Value;
            var exportPath = await _reportingService.CreatePayrollStatementPdfAsync(
                _currentEmployeeId.Value,
                selectedMonth.Year,
                selectedMonth.Month);

            StatusMessage = $"PDF erstellt: {exportPath}";
        });
    }

    private async Task CreateBackupAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var backup = await _backupRestoreService.CreateBackupAsync(
                new CreateBackupCommand(
                    BackupDirectoryPath,
                    BackupFileName,
                    ParseBackupContentType(SelectedBackupContentType)));

            BackupFileName = _backupRestoreService.CreateDefaultFileName(DateTimeOffset.Now);
            StatusMessage = $"Backup erstellt: {backup.FilePath}";
        });
    }

    private async Task RestoreBackupAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var restoreType = ParseBackupContentType(SelectedRestoreContentType);
            var currentEmployeeId = _currentEmployeeId;
            var result = await _backupRestoreService.RestoreBackupAsync(
                new RestoreBackupCommand(
                    RestoreFilePath,
                    restoreType));

            if (restoreType is BackupContentType.Configuration or BackupContentType.Both)
            {
                await LoadSettingsAsync();
            }

            if (restoreType is BackupContentType.UserData or BackupContentType.Both)
            {
                await ReloadEmployeesAsync();
                await RestoreSelectionAfterReloadAsync(currentEmployeeId, selectFirstIfMissing: true);
            }

            StatusMessage = $"Restore abgeschlossen: {result.BackupFilePath}";
        });
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }

        if (_pendingEmployeeId.HasValue && !_isEditing)
        {
            var pendingEmployeeId = _pendingEmployeeId.Value;
            _pendingEmployeeId = null;

            if (_currentEmployeeId != pendingEmployeeId)
            {
                await LoadEmployeeAsync(pendingEmployeeId);
            }
        }
    }

    private async Task ReloadEmployeesAsync()
    {
        var employees = await _employeeService.ListAsync(new EmployeeListQuery(SearchText, ParseActivityFilter(SelectedActivityFilter)));
        Employees.Clear();

        foreach (var employee in employees)
        {
            Employees.Add(new EmployeeListItemViewModel
            {
                EmployeeId = employee.EmployeeId,
                PersonnelNumber = employee.PersonnelNumber,
                FullName = employee.FullName,
                StatusSummary = employee.IsActive ? "Aktiv" : "Inaktiv",
                ContactSummary = BuildContactSummary(employee.City, employee.Country, employee.Email),
                ContractSummary = $"{employee.HourlyRateChf:0.00} CHF/h | BVG {employee.MonthlyBvgDeductionChf:0.00} CHF"
            });
        }

        EmployeeCountSummary = Employees.Count == 1
            ? "1 Mitarbeitender in der aktuellen Ansicht."
            : $"{Employees.Count} Mitarbeitende in der aktuellen Ansicht.";
    }

    private async Task RestoreSelectionAfterReloadAsync(Guid? preferredEmployeeId, bool selectFirstIfMissing)
    {
        var selectedListItem = preferredEmployeeId.HasValue
            ? Employees.FirstOrDefault(item => item.EmployeeId == preferredEmployeeId.Value)
            : null;

        if (selectedListItem is not null)
        {
            SetSelectedEmployeeWithoutReload(selectedListItem);
            await LoadEmployeeIntoViewAsync(selectedListItem.EmployeeId);
            return;
        }

        if (selectFirstIfMissing && Employees.Count > 0)
        {
            SetSelectedEmployeeWithoutReload(Employees[0]);
            await LoadEmployeeIntoViewAsync(Employees[0].EmployeeId);
            return;
        }

            SetSelectedEmployeeWithoutReload(null);
            _currentEmployeeId = null;
            MonthlyRecord.Reset();
            if (!_isEditing)
            {
                ClearFormForEmptyState();
        }
    }

    private void PopulateForm(EmployeeDetailsDto employee)
    {
        _loadedCurrentContractId = employee.CurrentContractId;
        PersonnelNumber = employee.PersonnelNumber;
        FirstName = employee.FirstName;
        LastName = employee.LastName;
        BirthDate = employee.BirthDate.HasValue
            ? new DateTimeOffset(employee.BirthDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        EntryDate = new DateTimeOffset(employee.EntryDate.ToDateTime(TimeOnly.MinValue));
        ExitDate = employee.ExitDate.HasValue
            ? new DateTimeOffset(employee.ExitDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        IsActiveEmployee = employee.IsActive;
        Street = employee.Street;
        HouseNumber = employee.HouseNumber;
        AddressLine2 = employee.AddressLine2;
        PostalCode = employee.PostalCode;
        City = employee.City;
        Country = employee.Country;
        ResidenceCountry = employee.ResidenceCountry;
        Nationality = employee.Nationality;
        PermitCode = employee.PermitCode;
        TaxStatus = employee.TaxStatus;
        SelectedWithholdingTaxOption = employee.IsSubjectToWithholdingTax switch
        {
            true => WithholdingTaxYes,
            false => WithholdingTaxNo,
            _ => WithholdingTaxUnknown
        };
        AhvNumber = employee.AhvNumber;
        Iban = employee.Iban;
        PhoneNumber = employee.PhoneNumber;
        Email = employee.Email;
        SelectedDepartmentOption = FindOptionById(DepartmentOptions, employee.DepartmentOptionId);
        SelectedEmploymentCategoryOption = FindOptionById(EmploymentCategoryOptions, employee.EmploymentCategoryOptionId);
        SelectedEmploymentLocationOption = FindOptionById(EmploymentLocationOptions, employee.EmploymentLocationOptionId);
        SelectedWageType = MapWageTypeToLabel(employee.WageType);
        ContractValidFrom = employee.ContractValidFrom == default
            ? null
            : new DateTimeOffset(employee.ContractValidFrom.ToDateTime(TimeOnly.MinValue));
        ContractValidTo = employee.ContractValidTo.HasValue
            ? new DateTimeOffset(employee.ContractValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        HourlyRateChf = NumericFormatManager.FormatDecimal(employee.HourlyRateChf, "0.00");
        MonthlyBvgDeductionChf = NumericFormatManager.FormatDecimal(employee.MonthlyBvgDeductionChf, "0.00");
        SpecialSupplementRateChf = NumericFormatManager.FormatDecimal(employee.SpecialSupplementRateChf, "0.00");
        ApplyContractHistory(employee.ContractHistory);
    }

    private void ResetFormForNewDraft()
    {
        PersonnelNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        BirthDate = null;
        EntryDate = new DateTimeOffset(DateTime.Today);
        ExitDate = null;
        IsActiveEmployee = true;
        Street = string.Empty;
        HouseNumber = null;
        AddressLine2 = null;
        PostalCode = string.Empty;
        City = string.Empty;
        Country = "Schweiz";
        ResidenceCountry = null;
        Nationality = null;
        PermitCode = null;
        TaxStatus = null;
        SelectedWithholdingTaxOption = WithholdingTaxUnknown;
        AhvNumber = null;
        Iban = null;
        PhoneNumber = null;
        Email = null;
        SelectedDepartmentOption = DepartmentOptions.FirstOrDefault();
        SelectedEmploymentCategoryOption = EmploymentCategoryOptions.FirstOrDefault();
        SelectedEmploymentLocationOption = EmploymentLocationOptions.FirstOrDefault();
        SelectedWageType = WageTypeHourlyLabel;
        _loadedCurrentContractId = null;
        ContractValidFrom = new DateTimeOffset(DateTime.Today);
        ContractValidTo = null;
        HourlyRateChf = NumericFormatManager.FormatDecimal(0m, "0");
        MonthlyBvgDeductionChf = NumericFormatManager.FormatDecimal(0m, "0");
        SpecialSupplementRateChf = NumericFormatManager.FormatDecimal(3m, "0.00");
        ContractHistory.Clear();
    }

    private void ClearFormForEmptyState()
    {
        _currentEmployeeId = null;
        PersonnelNumber = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        BirthDate = null;
        EntryDate = null;
        ExitDate = null;
        IsActiveEmployee = true;
        Street = string.Empty;
        HouseNumber = null;
        AddressLine2 = null;
        PostalCode = string.Empty;
        City = string.Empty;
        Country = string.Empty;
        ResidenceCountry = null;
        Nationality = null;
        PermitCode = null;
        TaxStatus = null;
        SelectedWithholdingTaxOption = WithholdingTaxUnknown;
        AhvNumber = null;
        Iban = null;
        PhoneNumber = null;
        Email = null;
        SelectedDepartmentOption = null;
        SelectedEmploymentCategoryOption = null;
        SelectedEmploymentLocationOption = null;
        SelectedWageType = WageTypeHourlyLabel;
        _loadedCurrentContractId = null;
        ContractValidFrom = null;
        ContractValidTo = null;
        HourlyRateChf = string.Empty;
        MonthlyBvgDeductionChf = string.Empty;
        SpecialSupplementRateChf = string.Empty;
        ContractHistory.Clear();
        SetInteractionState(isEditing: false, isCreatingNew: false);
    }

    private void ApplyContractHistory(IReadOnlyCollection<EmploymentContractVersionDto> history)
    {
        ContractHistory.Clear();

        foreach (var entry in history
                     .OrderByDescending(item => item.ValidFrom)
                     .ThenByDescending(item => item.ContractId))
        {
            ContractHistory.Add(new EmploymentContractHistoryItemViewModel
            {
                ContractId = entry.ContractId,
                ValidFrom = new DateTimeOffset(entry.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                ValidTo = entry.ValidTo.HasValue ? new DateTimeOffset(entry.ValidTo.Value.ToDateTime(TimeOnly.MinValue)) : null,
                HourlyRateDisplay = $"{NumericFormatManager.FormatDecimal(entry.HourlyRateChf, "0.00")} CHF/h",
                MonthlyBvgDisplay = $"BVG {NumericFormatManager.FormatDecimal(entry.MonthlyBvgDeductionChf, "0.00")} CHF",
                SpecialSupplementDisplay = $"Spezial {NumericFormatManager.FormatDecimal(entry.SpecialSupplementRateChf, "0.00")} CHF",
                IsCurrent = entry.IsCurrent
            });
        }

        SelectedContractHistoryEntry = ContractHistory.FirstOrDefault(item => item.IsCurrent) ?? ContractHistory.FirstOrDefault();
    }

    private DateTimeOffset DetermineSuggestedNewContractVersionValidFrom()
    {
        if (ContractValidFrom.HasValue)
        {
            var date = ContractValidFrom.Value.Date.AddMonths(1);
            return new DateTimeOffset(new DateTime(date.Year, date.Month, 1));
        }

        return new DateTimeOffset(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
    }

    private string BuildContractVersionDialogSummary()
    {
        if (!ShowContractVersionCreateSection || !NewContractVersionValidFrom.HasValue)
        {
            return "Die Historie ist vom normalen Bearbeitungsflow getrennt. Neue Versionen werden nur ueber den expliziten Schritt angelegt.";
        }

        var previousEnd = NewContractVersionValidFrom.Value.AddDays(-1);
        var newRangeEnd = NewContractVersionValidTo.HasValue
            ? FormatDate(NewContractVersionValidTo)
            : "offen";

        return $"Der bisher aktive Stand endet automatisch am {FormatDate(previousEnd)}. Der neue Stand gilt von {FormatDate(NewContractVersionValidFrom)} bis {newRangeEnd}.";
    }

    private void ApplySettingsVersionHistory(string area)
    {
        SettingsVersionHistory.Clear();

        switch (area)
        {
            case SettingsVersionAreaGeneral:
                foreach (var entry in _generalSettingsHistory)
                {
                    SettingsVersionHistory.Add(new PayrollCalculationSettingsVersionItemViewModel
                    {
                        VersionId = entry.VersionId,
                        ValidFrom = new DateTimeOffset(entry.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                        ValidTo = entry.ValidTo.HasValue ? new DateTimeOffset(entry.ValidTo.Value.ToDateTime(TimeOnly.MinValue)) : null,
                        Summary = $"AHV {FormatPercentage(entry.AhvIvEoRate, "0.###")} | ALV {FormatPercentage(entry.AlvRate, "0.###")} | UVG {FormatPercentage(entry.SicknessAccidentInsuranceRate, "0.###")} | Aus-/Weiterbildung {FormatPercentage(entry.TrainingAndHolidayRate, "0.###")}",
                        IsCurrent = entry.IsCurrent
                    });
                }

                break;

            case SettingsVersionAreaHourly:
                foreach (var entry in _hourlySettingsHistory)
                {
                    SettingsVersionHistory.Add(new PayrollCalculationSettingsVersionItemViewModel
                    {
                        VersionId = entry.VersionId,
                        ValidFrom = new DateTimeOffset(entry.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                        ValidTo = entry.ValidTo.HasValue ? new DateTimeOffset(entry.ValidTo.Value.ToDateTime(TimeOnly.MinValue)) : null,
                        Summary = $"N {FormatOptionalPercentage(entry.NightSupplementRate)} | S {FormatOptionalPercentage(entry.SundaySupplementRate)} | F {FormatOptionalPercentage(entry.HolidaySupplementRate)} | Ferien {FormatPercentage(entry.VacationCompensationRate, "0.##")}",
                        IsCurrent = entry.IsCurrent
                    });
                }

                break;

            case SettingsVersionAreaMonthlySalary:
                foreach (var entry in _monthlySalarySettingsHistory)
                {
                    SettingsVersionHistory.Add(new PayrollCalculationSettingsVersionItemViewModel
                    {
                        VersionId = entry.VersionId,
                        ValidFrom = new DateTimeOffset(entry.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                        ValidTo = entry.ValidTo.HasValue ? new DateTimeOffset(entry.ValidTo.Value.ToDateTime(TimeOnly.MinValue)) : null,
                        Summary = "Bereich vorbereitet fuer kuenftige Monatslohn-Parameter.",
                        IsCurrent = entry.IsCurrent
                    });
                }

                break;
        }
    }

    private DateTimeOffset DetermineSuggestedNewSettingsVersionValidFrom(string area)
    {
        var currentValidFrom = area switch
        {
            SettingsVersionAreaGeneral => SettingsGeneralValidFrom,
            SettingsVersionAreaHourly => SettingsHourlyValidFrom,
            SettingsVersionAreaMonthlySalary => SettingsMonthlySalaryValidFrom,
            _ => StartOfCurrentMonth()
        };

        if (currentValidFrom.HasValue)
        {
            var date = currentValidFrom.Value.Date.AddMonths(1);
            return new DateTimeOffset(new DateTime(date.Year, date.Month, 1));
        }

        return StartOfCurrentMonth();
    }

    private string BuildSettingsVersionDialogSummary()
    {
        if (!ShowSettingsVersionCreateSection || !NewSettingsVersionValidFrom.HasValue)
        {
            return "Neue Satzstaende werden nur ueber den expliziten Neuanlage-Schritt angelegt.";
        }

        var previousEnd = NewSettingsVersionValidFrom.Value.AddDays(-1);
        var newRangeEnd = NewSettingsVersionValidTo.HasValue
            ? FormatDate(NewSettingsVersionValidTo)
            : "offen";

        return $"Der bisher aktive Stand endet automatisch am {FormatDate(previousEnd)}. Der neue Stand gilt von {FormatDate(NewSettingsVersionValidFrom)} bis {newRangeEnd}.";
    }

    private static string FormatOptionalPercentage(decimal? value)
    {
        return value.HasValue
            ? FormatPercentage(value.Value, "0.##")
            : "-";
    }

    private static DateTimeOffset StartOfCurrentMonth()
    {
        return new DateTimeOffset(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value?.ToString("dd.MM.yyyy") ?? "offen";
    }

    private void SetInteractionState(bool isEditing, bool isCreatingNew)
    {
        _isEditing = isEditing;
        _isCreatingNew = isCreatingNew;
        MonthlyRecord.IsLocked = isEditing;
        if (!_isEditing)
        {
            DismissDeleteConfirmation();
        }

        RaisePropertyChanged(nameof(FormTitle));
        RaisePropertyChanged(nameof(FormDescription));
        RaisePropertyChanged(nameof(AreFieldsReadOnly));
        RaisePropertyChanged(nameof(CanEditFields));
        RaisePropertyChanged(nameof(CanSearchEmployees));
        RaisePropertyChanged(nameof(CanSelectEmployees));
        RaisePropertyChanged(nameof(CanBrowseEmployees));
        RaisePropertyChanged(nameof(CanStartCreate));
        RaisePropertyChanged(nameof(CanStartEdit));
        RaisePropertyChanged(nameof(CanSave));
        RaisePropertyChanged(nameof(CanCancel));
        RaisePropertyChanged(nameof(CanRequestDelete));
        RaisePropertyChanged(nameof(CanOpenContractVersionDialog));
        RaisePropertyChanged(nameof(CanOpenNewContractVersionDialog));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(CanDismissContractVersionDialog));
        RaisePropertyChanged(nameof(CanOpenGeneralSettingsVersionDialog));
        RaisePropertyChanged(nameof(CanOpenNewGeneralSettingsVersionDialog));
        RaisePropertyChanged(nameof(CanOpenHourlySettingsVersionDialog));
        RaisePropertyChanged(nameof(CanOpenNewHourlySettingsVersionDialog));
        RaisePropertyChanged(nameof(CanOpenMonthlySalarySettingsVersionDialog));
        RaisePropertyChanged(nameof(CanOpenNewMonthlySalarySettingsVersionDialog));
        RaisePropertyChanged(nameof(CanConfirmSettingsVersionDialog));
        RaisePropertyChanged(nameof(CanDismissSettingsVersionDialog));
        RaisePropertyChanged(nameof(ShowViewActions));
        RaisePropertyChanged(nameof(ShowEditActions));
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(ShowSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanClearExitDate));
        RaiseActionStateChanged();
    }

    private void RaiseActionStateChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SearchCommand.RaiseCanExecuteChanged();
        NewEmployeeCommand.RaiseCanExecuteChanged();
        EditEmployeeCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ConfirmDeleteCommand.RaiseCanExecuteChanged();
        DismissDeleteCommand.RaiseCanExecuteChanged();
        OpenContractVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewContractVersionDialogCommand.RaiseCanExecuteChanged();
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        DismissContractVersionDialogCommand.RaiseCanExecuteChanged();
        OpenGeneralSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewGeneralSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenHourlySettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewHourlySettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenMonthlySalarySettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewMonthlySalarySettingsVersionDialogCommand.RaiseCanExecuteChanged();
        ConfirmSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        DismissSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        ClearExitDateCommand.RaiseCanExecuteChanged();
        ShowTimeAndExpensesCommand.RaiseCanExecuteChanged();
        ShowPayrollRunsCommand.RaiseCanExecuteChanged();
        ShowReportingCommand.RaiseCanExecuteChanged();
        ShowEmployeesCommand.RaiseCanExecuteChanged();
        ShowSettingsCommand.RaiseCanExecuteChanged();
        SaveSettingsCommand.RaiseCanExecuteChanged();
        LoadPersonImportCsvCommand.RaiseCanExecuteChanged();
        SavePersonImportConfigurationCommand.RaiseCanExecuteChanged();
        LoadPersonImportConfigurationCommand.RaiseCanExecuteChanged();
        ImportPersonDataCommand.RaiseCanExecuteChanged();
        LoadTimeImportCsvCommand.RaiseCanExecuteChanged();
        SaveTimeImportConfigurationCommand.RaiseCanExecuteChanged();
        LoadTimeImportConfigurationCommand.RaiseCanExecuteChanged();
        ImportTimeDataCommand.RaiseCanExecuteChanged();
        DeleteImportedTimeMonthCommand.RaiseCanExecuteChanged();
        CreateBackupCommand.RaiseCanExecuteChanged();
        RestoreBackupCommand.RaiseCanExecuteChanged();
        CreatePayrollPdfCommand.RaiseCanExecuteChanged();
        AddDepartmentOptionCommand.RaiseCanExecuteChanged();
        RemoveDepartmentOptionCommand.RaiseCanExecuteChanged();
        AddEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
        RemoveEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
        AddEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
        RemoveEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadImportConfigurationsAsync()
    {
        var configurations = await _importService.ListConfigurationsAsync(Payroll.Domain.Imports.ImportConfigurationType.PersonData);
        PersonImportConfigurations.Clear();

        foreach (var configuration in configurations)
        {
            PersonImportConfigurations.Add(new ImportConfigurationItemViewModel
            {
                ConfigurationId = configuration.ConfigurationId,
                Name = configuration.Name
            });
        }

        var timeConfigurations = await _importService.ListConfigurationsAsync(Payroll.Domain.Imports.ImportConfigurationType.TimeData);
        TimeImportConfigurations.Clear();

        foreach (var configuration in timeConfigurations)
        {
            TimeImportConfigurations.Add(new ImportConfigurationItemViewModel
            {
                ConfigurationId = configuration.ConfigurationId,
                Name = configuration.Name
            });
        }
    }

    private async Task LoadImportedTimeMonthsAsync()
    {
        var importedMonths = await _importService.ListImportedMonthsAsync(Payroll.Domain.Imports.ImportConfigurationType.TimeData);
        ImportedTimeMonths.Clear();

        foreach (var month in importedMonths)
        {
            ImportedTimeMonths.Add(new ImportedMonthStatusItemViewModel
            {
                Year = month.Year,
                Month = month.Month,
                ImportedAtUtc = month.ImportedAtUtc
            });
        }

        if (SelectedImportedTimeMonth is not null)
        {
            SelectedImportedTimeMonth = ImportedTimeMonths.FirstOrDefault(item =>
                item.Year == SelectedImportedTimeMonth.Year
                && item.Month == SelectedImportedTimeMonth.Month);
        }
    }

    private async void OnMonthlyRecordTimeCaptureChanged(object? sender, EventArgs e)
    {
        await LoadMonthCaptureOverviewAsync();
    }

    private async void OnMonthlyRecordPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MonthlyRecordViewModel.SelectedMonth) or nameof(MonthlyRecordViewModel.SelectedMonthText))
        {
            RaisePropertyChanged(nameof(MonthCaptureMonthLabel));
            await LoadMonthCaptureOverviewAsync();
        }
    }

    private async Task LoadMonthCaptureOverviewAsync()
    {
        if (!MonthlyRecord.SelectedMonth.HasValue)
        {
            _allMonthCaptureOverviewRows = [];
            ApplyMonthCaptureOverviewFilter();
            return;
        }

        try
        {
            _allMonthCaptureOverviewRows = await _monthlyRecordService.ListTimeCaptureOverviewAsync(
                new MonthlyTimeCaptureOverviewQuery(
                    MonthlyRecord.SelectedMonth.Value.Year,
                    MonthlyRecord.SelectedMonth.Value.Month));
            ApplyMonthCaptureOverviewFilter();
        }
        catch
        {
            MonthCaptureOverviewRows.Clear();
            MonthCaptureSummary = "Stundenerfassungen konnten nicht geladen werden.";
        }
    }

    private void ApplyMonthCaptureOverviewFilter()
    {
        IEnumerable<MonthlyTimeCaptureOverviewRowDto> filteredRows = _allMonthCaptureOverviewRows;

        filteredRows = SelectedMonthCaptureFilter switch
        {
            MonthCaptureFilterWithoutMonth => filteredRows.Where(row => !row.HasMonthCapture),
            MonthCaptureFilterWithMonth => filteredRows.Where(row => row.HasMonthCapture),
            _ => filteredRows
        };

        var rows = filteredRows
            .OrderBy(row => row.LastName)
            .ThenBy(row => row.FirstName)
            .ThenBy(row => row.PersonnelNumber)
            .ToArray();

        MonthCaptureOverviewRows.Clear();
        foreach (var row in rows)
        {
            MonthCaptureOverviewRows.Add(row);
        }

        MonthCaptureSummary = rows.Length == 1
            ? $"1 Mitarbeitender fuer {MonthCaptureMonthLabel} in der aktuellen Ansicht."
            : $"{rows.Length} Mitarbeitende fuer {MonthCaptureMonthLabel} in der aktuellen Ansicht.";
    }

    private async Task<bool> LoadEmployeeIntoViewAsync(Guid employeeId)
    {
        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee is null)
        {
            return false;
        }

        PopulateForm(employee);
        _currentEmployeeId = employee.EmployeeId;
        _returnEmployeeId = employee.EmployeeId;
        await MonthlyRecord.SetEmployeeAsync(employee.EmployeeId, employee.FirstName + " " + employee.LastName);
        SetInteractionState(isEditing: false, isCreatingNew: false);
        return true;
    }

    private void SetSelectedEmployeeWithoutReload(EmployeeListItemViewModel? employee)
    {
        if (EqualityComparer<EmployeeListItemViewModel?>.Default.Equals(_selectedEmployee, employee))
        {
            return;
        }

        _selectedEmployee = employee;
        RaisePropertyChanged(nameof(SelectedEmployee));
    }

    private static IReadOnlyCollection<SettingOptionDto> BuildSettingOptionDtos(IEnumerable<EditableSettingOptionViewModel> options)
    {
        return options
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new SettingOptionDto(item.OptionId, item.Name.Trim()))
            .ToArray();
    }

    private void ApplyOptions(
        ObservableCollection<EditableSettingOptionViewModel> target,
        IReadOnlyCollection<SettingOptionDto> source,
        ref EditableSettingOptionViewModel? selectedEmployeeTarget,
        string selectedEmployeePropertyName,
        ref EditableSettingOptionViewModel? selectedSettingsTarget,
        string selectedSettingsPropertyName)
    {
        var previousEmployeeSelectionId = selectedEmployeeTarget?.OptionId;

        target.Clear();
        foreach (var item in source.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            target.Add(new EditableSettingOptionViewModel
            {
                OptionId = item.OptionId,
                Name = item.Name
            });
        }

        selectedEmployeeTarget = FindOptionById(target, previousEmployeeSelectionId) ?? target.FirstOrDefault();
        selectedSettingsTarget = null;
        RaisePropertyChanged(selectedEmployeePropertyName);
        RaisePropertyChanged(selectedSettingsPropertyName);
    }

    private static EditableSettingOptionViewModel? FindOptionById(
        IEnumerable<EditableSettingOptionViewModel> options,
        Guid? optionId)
    {
        return optionId.HasValue
            ? options.FirstOrDefault(item => item.OptionId == optionId.Value)
            : null;
    }

    private void AddSettingOption(
        ObservableCollection<EditableSettingOptionViewModel> collection,
        ref EditableSettingOptionViewModel? selectedEmployeeValue,
        ref EditableSettingOptionViewModel? selectedSettingsValue,
        string selectedEmployeePropertyName,
        string selectedSettingsPropertyName,
        ref string newValue,
        string newValuePropertyName)
    {
        var trimmedValue = newValue.Trim();
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            return;
        }

        if (collection.Any(item => string.Equals(item.Name, trimmedValue, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Eintrag existiert bereits.");
        }

        var option = new EditableSettingOptionViewModel
        {
            OptionId = Guid.NewGuid(),
            Name = trimmedValue
        };

        collection.Add(option);
        selectedEmployeeValue = option;
        selectedSettingsValue = option;
        newValue = string.Empty;
        RaisePropertyChanged(selectedEmployeePropertyName);
        RaisePropertyChanged(selectedSettingsPropertyName);
        RaisePropertyChanged(newValuePropertyName);
    }

    private void RemoveSettingOption(
        ObservableCollection<EditableSettingOptionViewModel> collection,
        EditableSettingOptionViewModel? selectedSettingsValue,
        ref EditableSettingOptionViewModel? selectedSettingsTarget,
        string selectedSettingsPropertyName,
        ref EditableSettingOptionViewModel? selectedEmployeeTarget,
        string selectedEmployeePropertyName)
    {
        if (selectedSettingsValue is null)
        {
            return;
        }

        collection.Remove(selectedSettingsValue);
        if (ReferenceEquals(selectedEmployeeTarget, selectedSettingsValue))
        {
            selectedEmployeeTarget = collection.FirstOrDefault();
            RaisePropertyChanged(selectedEmployeePropertyName);
        }

        selectedSettingsTarget = null;
        RaisePropertyChanged(selectedSettingsPropertyName);
    }

    private static decimal ParseRequiredDecimal(string value, string fieldName)
    {
        if (!NumericFormatManager.TryParseDecimal(value, out var parsedValue))
        {
            throw new InvalidOperationException($"{fieldName} muss eine gueltige Zahl sein.");
        }

        return parsedValue;
    }

    private static decimal? ParseOptionalDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!NumericFormatManager.TryParseDecimal(value, out var parsedValue))
        {
            throw new InvalidOperationException("Einstellungswerte muessen gueltige Zahlen sein.");
        }

        return parsedValue;
    }

    private static decimal ParseRequiredPercentage(string value, string fieldName)
    {
        return ParseRequiredDecimal(value, fieldName) / 100m;
    }

    private static decimal? ParseOptionalPercentage(string? value)
    {
        var parsedValue = ParseOptionalDecimal(value);
        return parsedValue.HasValue ? parsedValue.Value / 100m : null;
    }

    private static BackupContentType ParseBackupContentType(string label)
    {
        return label switch
        {
            BackupTypeConfigurationLabel => BackupContentType.Configuration,
            BackupTypeUserDataLabel => BackupContentType.UserData,
            _ => BackupContentType.Both
        };
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _payrollSettingsService.GetAsync();
        ApplySettings(settings);
    }

    private void InitializeImportFieldMappings()
    {
        foreach (var field in _importService.GetFieldDefinitions(Payroll.Domain.Imports.ImportConfigurationType.PersonData))
        {
            var row = new ImportFieldMappingRowViewModel
            {
                FieldKey = field.FieldKey,
                FieldLabel = field.Label,
                IsRequired = field.IsRequired,
                AllowEmpty = false
            };
            row.PropertyChanged += OnImportFieldMappingRowPropertyChanged;
            PersonImportFieldMappings.Add(row);
        }

        foreach (var field in _importService.GetFieldDefinitions(Payroll.Domain.Imports.ImportConfigurationType.TimeData))
        {
            var row = new ImportFieldMappingRowViewModel
            {
                FieldKey = field.FieldKey,
                FieldLabel = field.Label,
                IsRequired = field.IsRequired,
                AllowEmpty = false
            };
            row.PropertyChanged += OnImportFieldMappingRowPropertyChanged;
            TimeImportFieldMappings.Add(row);
        }
    }

    private void ApplyPersonImportCsvHeaders(IReadOnlyCollection<string> headers)
    {
        foreach (var row in PersonImportFieldMappings)
        {
            row.ApplyAvailableCsvColumns(headers);
        }

        RaiseImportCommandState();
    }

    private void ApplyPersonImportConfiguration(ImportConfigurationDto configuration)
    {
        PersonImportConfigurationName = configuration.Name;
        SelectedPersonImportDelimiter = ToPersonImportDelimiterOption(configuration.Delimiter);
        PersonImportFieldsEnclosed = configuration.FieldsEnclosed;
        SelectedPersonImportTextQualifier = configuration.TextQualifier == "'"
            ? "Einfache Anfuehrungszeichen (')"
            : "Doppelte Anfuehrungszeichen (\")";

        var mappingsByField = configuration.Mappings.ToDictionary(item => item.FieldKey, item => item.CsvColumnName, StringComparer.OrdinalIgnoreCase);
        var allowEmptyByField = configuration.Mappings.ToDictionary(item => item.FieldKey, item => item.AllowEmpty, StringComparer.OrdinalIgnoreCase);
        foreach (var row in PersonImportFieldMappings)
        {
            row.SelectedCsvColumn = mappingsByField.TryGetValue(row.FieldKey, out var mappedColumn)
                ? mappedColumn
                : string.Empty;
            row.AllowEmpty = allowEmptyByField.TryGetValue(row.FieldKey, out var allowEmpty) && allowEmpty;
            row.SetSearchTextFromSelection(row.SelectedCsvColumn);
        }

        RaiseImportCommandState();
    }

    private void ApplyTimeImportCsvHeaders(IReadOnlyCollection<string> headers)
    {
        foreach (var row in TimeImportFieldMappings)
        {
            row.ApplyAvailableCsvColumns(headers);
        }

        RaiseImportCommandState();
    }

    private void ApplyTimeImportConfiguration(ImportConfigurationDto configuration)
    {
        TimeImportConfigurationName = configuration.Name;
        SelectedTimeImportDelimiter = ToPersonImportDelimiterOption(configuration.Delimiter);
        TimeImportFieldsEnclosed = configuration.FieldsEnclosed;
        SelectedTimeImportTextQualifier = configuration.TextQualifier == "'"
            ? "Einfache Anfuehrungszeichen (')"
            : "Doppelte Anfuehrungszeichen (\")";

        var mappingsByField = configuration.Mappings.ToDictionary(item => item.FieldKey, item => item.CsvColumnName, StringComparer.OrdinalIgnoreCase);
        var allowEmptyByField = configuration.Mappings.ToDictionary(item => item.FieldKey, item => item.AllowEmpty, StringComparer.OrdinalIgnoreCase);
        foreach (var row in TimeImportFieldMappings)
        {
            row.SelectedCsvColumn = mappingsByField.TryGetValue(row.FieldKey, out var mappedColumn)
                ? mappedColumn
                : string.Empty;
            row.AllowEmpty = allowEmptyByField.TryGetValue(row.FieldKey, out var allowEmpty) && allowEmpty;
            row.SetSearchTextFromSelection(row.SelectedCsvColumn);
        }

        RaiseImportCommandState();
    }

    private IReadOnlyCollection<ImportFieldMappingDto> BuildPersonImportFieldMappings()
    {
        return PersonImportFieldMappings
            .Select(row => new ImportFieldMappingDto(row.FieldKey, row.SelectedCsvColumn ?? string.Empty, row.AllowEmpty))
            .ToArray();
    }

    private IReadOnlyCollection<ImportFieldMappingDto> BuildTimeImportFieldMappings()
    {
        return TimeImportFieldMappings
            .Select(row => new ImportFieldMappingDto(row.FieldKey, row.SelectedCsvColumn ?? string.Empty, row.AllowEmpty))
            .ToArray();
    }

    private void OnImportFieldMappingRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImportFieldMappingRowViewModel.SelectedCsvColumn) or nameof(ImportFieldMappingRowViewModel.AllowEmpty))
        {
            RaiseImportCommandState();
            if (sender is ImportFieldMappingRowViewModel row && PersonImportFieldMappings.Contains(row))
            {
                ClearPersonImportPreview();
            }
        }
    }

    private void RaiseImportCommandState()
    {
        RaisePropertyChanged(nameof(CanLoadPersonImportCsv));
        RaisePropertyChanged(nameof(CanSavePersonImportConfiguration));
        RaisePropertyChanged(nameof(CanLoadPersonImportConfiguration));
        RaisePropertyChanged(nameof(CanImportPersonData));
        RaisePropertyChanged(nameof(PersonImportHasCsvHeaders));
        RaisePropertyChanged(nameof(HasPersonImportPreviewItems));
        RaisePropertyChanged(nameof(PersonImportValidationErrors));
        RaisePropertyChanged(nameof(CanLoadTimeImportCsv));
        RaisePropertyChanged(nameof(CanSaveTimeImportConfiguration));
        RaisePropertyChanged(nameof(CanLoadTimeImportConfiguration));
        RaisePropertyChanged(nameof(CanImportTimeData));
        RaisePropertyChanged(nameof(CanDeleteImportedTimeMonth));
        RaisePropertyChanged(nameof(TimeImportHasCsvHeaders));
        RaisePropertyChanged(nameof(TimeImportValidationErrors));
        LoadPersonImportCsvCommand.RaiseCanExecuteChanged();
        SavePersonImportConfigurationCommand.RaiseCanExecuteChanged();
        LoadPersonImportConfigurationCommand.RaiseCanExecuteChanged();
        ImportPersonDataCommand.RaiseCanExecuteChanged();
        LoadTimeImportCsvCommand.RaiseCanExecuteChanged();
        SaveTimeImportConfigurationCommand.RaiseCanExecuteChanged();
        LoadTimeImportConfigurationCommand.RaiseCanExecuteChanged();
        ImportTimeDataCommand.RaiseCanExecuteChanged();
        DeleteImportedTimeMonthCommand.RaiseCanExecuteChanged();
    }

    private async Task RefreshPersonImportPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(PersonImportCsvFilePath) || PersonImportValidationErrors.Count > 0)
        {
            ClearPersonImportPreview();
            RaiseImportCommandState();
            return;
        }

        var preview = await _importService.PreviewPersonDataAsync(new PreviewPersonDataCommand(
            PersonImportCsvFilePath,
            ResolvePersonImportDelimiter(),
            PersonImportFieldsEnclosed,
            ResolvePersonImportTextQualifier(),
            BuildPersonImportFieldMappings()));

        var previousSelections = PersonImportPreviewItems.ToDictionary(item => item.RowNumber, item => item.IsSelected);
        PersonImportPreviewItems.Clear();
        foreach (var item in preview)
        {
            var viewModel = new PersonImportPreviewItemViewModel
            {
                RowNumber = item.RowNumber,
                PersonnelNumber = item.PersonnelNumber,
                FullName = item.FullName,
                AlreadyExists = item.AlreadyExists,
                IsSelected = previousSelections.TryGetValue(item.RowNumber, out var isSelected) ? isSelected : true
            };
            viewModel.PropertyChanged += OnPersonImportPreviewItemPropertyChanged;
            PersonImportPreviewItems.Add(viewModel);
        }

        RaiseImportCommandState();
    }

    private void ClearPersonImportPreview()
    {
        foreach (var item in PersonImportPreviewItems)
        {
            item.PropertyChanged -= OnPersonImportPreviewItemPropertyChanged;
        }

        PersonImportPreviewItems.Clear();
        RaiseImportCommandState();
    }

    private void OnPersonImportPreviewItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonImportPreviewItemViewModel.IsSelected))
        {
            RaiseImportCommandState();
        }
    }

    private string ResolvePersonImportTextQualifier()
    {
        return SelectedPersonImportTextQualifier.StartsWith("Einfache", StringComparison.Ordinal)
            ? "'"
            : "\"";
    }

    private string ResolveTimeImportTextQualifier()
    {
        return SelectedTimeImportTextQualifier.StartsWith("Einfache", StringComparison.Ordinal)
            ? "'"
            : "\"";
    }

    private string ResolvePersonImportDelimiter()
    {
        return SelectedPersonImportDelimiter switch
        {
            "Komma (,)" => ",",
            "Tabulator" => "\t",
            "Pipe (|)" => "|",
            _ => ";"
        };
    }

    private string ResolveTimeImportDelimiter()
    {
        return SelectedTimeImportDelimiter switch
        {
            "Komma (,)" => ",",
            "Tabulator" => "\t",
            "Pipe (|)" => "|",
            _ => ";"
        };
    }

    private static string ToPersonImportDelimiterOption(string delimiter)
    {
        return delimiter switch
        {
            "," => "Komma (,)",
            "\t" => "Tabulator",
            "|" => "Pipe (|)",
            _ => "Semikolon (;)"
        };
    }

    private static string BuildPersonImportStatusMessage(string summary, PersonDataImportResultDto result)
    {
        var offset = (result.CreatedCount > 0 || result.UpdatedCount > 0) ? 1 : 0;
        var details = result.Messages
            .Skip(offset)
            .Take(3)
            .ToArray();

        return details.Length == 0
            ? $"{summary}. Fehler: {result.ErrorCount}."
            : $"{summary}. Fehler: {result.ErrorCount}.{Environment.NewLine}{string.Join(Environment.NewLine, details)}";
    }

    private static string BuildTimeImportStatusMessage(TimeDataImportResultDto result)
    {
        var details = result.Messages
            .Skip(1)
            .Take(3)
            .ToArray();

        return details.Length == 0
            ? $"{result.ImportedCount} importiert. Fehler: {result.ErrorCount}."
            : $"{result.ImportedCount} importiert. Fehler: {result.ErrorCount}.{Environment.NewLine}{string.Join(Environment.NewLine, details)}";
    }

    private void ApplySettings(PayrollSettingsDto settings)
    {
        ThemeSettingsApplier.Apply(settings);
        _loadedCurrentGeneralSettingsVersionId = settings.CurrentGeneralSettingsVersionId;
        _loadedCurrentHourlySettingsVersionId = settings.CurrentHourlySettingsVersionId;
        _loadedCurrentMonthlySalarySettingsVersionId = settings.CurrentMonthlySalarySettingsVersionId;
        _generalSettingsHistory = settings.GeneralSettingsHistory ?? [];
        _hourlySettingsHistory = settings.HourlySettingsHistory ?? [];
        _monthlySalarySettingsHistory = settings.MonthlySalarySettingsHistory ?? [];
        SettingsGeneralValidFrom = settings.GeneralSettingsValidFrom == default
            ? StartOfCurrentMonth()
            : new DateTimeOffset(settings.GeneralSettingsValidFrom.ToDateTime(TimeOnly.MinValue));
        SettingsGeneralValidTo = settings.GeneralSettingsValidTo.HasValue
            ? new DateTimeOffset(settings.GeneralSettingsValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        SettingsHourlyValidFrom = settings.HourlySettingsValidFrom == default
            ? StartOfCurrentMonth()
            : new DateTimeOffset(settings.HourlySettingsValidFrom.ToDateTime(TimeOnly.MinValue));
        SettingsHourlyValidTo = settings.HourlySettingsValidTo.HasValue
            ? new DateTimeOffset(settings.HourlySettingsValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        SettingsMonthlySalaryValidFrom = settings.MonthlySalarySettingsValidFrom == default
            ? StartOfCurrentMonth()
            : new DateTimeOffset(settings.MonthlySalarySettingsValidFrom.ToDateTime(TimeOnly.MinValue));
        SettingsMonthlySalaryValidTo = settings.MonthlySalarySettingsValidTo.HasValue
            ? new DateTimeOffset(settings.MonthlySalarySettingsValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        SettingsCompanyAddress = settings.CompanyAddress;
        SettingsAppFontFamily = settings.AppFontFamily;
        SettingsAppFontSize = NumericFormatManager.FormatDecimal(settings.AppFontSize, "0.##");
        SettingsAppTextColorHex = settings.AppTextColorHex;
        SettingsAppMutedTextColorHex = settings.AppMutedTextColorHex;
        SettingsAppBackgroundColorHex = settings.AppBackgroundColorHex;
        SettingsAppAccentColorHex = settings.AppAccentColorHex;
        SettingsAppLogoText = settings.AppLogoText;
        SettingsAppLogoPath = settings.AppLogoPath;
        SettingsPrintFontFamily = settings.PrintFontFamily;
        SettingsPrintFontSize = NumericFormatManager.FormatDecimal(settings.PrintFontSize, "0.##");
        SettingsPrintTextColorHex = settings.PrintTextColorHex;
        SettingsPrintMutedTextColorHex = settings.PrintMutedTextColorHex;
        SettingsPrintAccentColorHex = settings.PrintAccentColorHex;
        SettingsPrintLogoText = settings.PrintLogoText;
        SettingsPrintLogoPath = settings.PrintLogoPath;
        SettingsPrintTemplate = settings.PrintTemplate;
        SettingsDecimalSeparator = settings.DecimalSeparator;
        SettingsThousandsSeparator = settings.ThousandsSeparator;
        SettingsCurrencyCode = settings.CurrencyCode;
        SettingsNightSupplementRate = FormatNullablePercentage(settings.NightSupplementRate, "0.##");
        SettingsSundaySupplementRate = FormatNullablePercentage(settings.SundaySupplementRate, "0.##");
        SettingsHolidaySupplementRate = FormatNullablePercentage(settings.HolidaySupplementRate, "0.##");
        SettingsAhvIvEoRate = FormatPercentage(settings.AhvIvEoRate, "0.###");
        SettingsAlvRate = FormatPercentage(settings.AlvRate, "0.###");
        SettingsSicknessAccidentInsuranceRate = FormatPercentage(settings.SicknessAccidentInsuranceRate, "0.###");
        SettingsTrainingAndHolidayRate = FormatPercentage(settings.TrainingAndHolidayRate, "0.###");
        SettingsVacationCompensationRate = FormatPercentage(settings.VacationCompensationRate, "0.##");
        SettingsVacationCompensationRateAge50Plus = FormatPercentage(settings.VacationCompensationRateAge50Plus, "0.##");
        SettingsVehiclePauschalzone1RateChf = NumericFormatManager.FormatDecimal(settings.VehiclePauschalzone1RateChf, "0.##");
        SettingsVehiclePauschalzone2RateChf = NumericFormatManager.FormatDecimal(settings.VehiclePauschalzone2RateChf, "0.##");
        SettingsVehicleRegiezone1RateChf = NumericFormatManager.FormatDecimal(settings.VehicleRegiezone1RateChf, "0.##");
        ApplyPayrollPreviewHelpOptions(settings.PayrollPreviewHelpOptions);
        AppLogoText = settings.AppLogoText;
        AppLogoImage = TryLoadLogo(settings.AppLogoPath);
        ApplyOptions(DepartmentOptions, settings.Departments, ref _selectedDepartmentOption, nameof(SelectedDepartmentOption), ref _selectedSettingsDepartment, nameof(SelectedSettingsDepartment));
        ApplyOptions(EmploymentCategoryOptions, settings.EmploymentCategories, ref _selectedEmploymentCategoryOption, nameof(SelectedEmploymentCategoryOption), ref _selectedSettingsEmploymentCategory, nameof(SelectedSettingsEmploymentCategory));
        ApplyOptions(EmploymentLocationOptions, settings.EmploymentLocations, ref _selectedEmploymentLocationOption, nameof(SelectedEmploymentLocationOption), ref _selectedSettingsEmploymentLocation, nameof(SelectedSettingsEmploymentLocation));
        RaisePropertyChanged(nameof(CanRemoveDepartmentOption));
        RaisePropertyChanged(nameof(CanRemoveEmploymentCategoryOption));
        RaisePropertyChanged(nameof(CanRemoveEmploymentLocationOption));
        RemoveDepartmentOptionCommand.RaiseCanExecuteChanged();
        RemoveEmploymentCategoryOptionCommand.RaiseCanExecuteChanged();
        RemoveEmploymentLocationOptionCommand.RaiseCanExecuteChanged();
    }

    private static string FormatPercentage(decimal value, string format)
    {
        return NumericFormatManager.FormatDecimal(value * 100m, format);
    }

    private static string ToThousandsSeparatorValue(string? label)
    {
        return label == ThousandsSeparatorSpaceLabel ? " " : Payroll.Domain.Settings.PayrollSettings.DefaultThousandsSeparator;
    }

    private static string ToThousandsSeparatorLabel(string? value)
    {
        return value == " " || value == ThousandsSeparatorSpaceLabel
            ? ThousandsSeparatorSpaceLabel
            : ThousandsSeparatorApostropheLabel;
    }

    private EmployeeWageType MapSelectedWageType()
    {
        return SelectedWageType == WageTypeMonthlyLabel
            ? EmployeeWageType.Monthly
            : EmployeeWageType.Hourly;
    }

    private static string MapWageTypeToLabel(EmployeeWageType wageType)
    {
        return wageType == EmployeeWageType.Monthly
            ? WageTypeMonthlyLabel
            : WageTypeHourlyLabel;
    }

    private static IReadOnlyCollection<PayrollPreviewHelpOptionDto> BuildPayrollPreviewHelpOptionDtos(
        IEnumerable<PayrollPreviewHelpToggleViewModel> options)
    {
        return options
            .Select(option => new PayrollPreviewHelpOptionDto(option.Code, option.Label, option.IsEnabled, option.HelpText))
            .ToArray();
    }

    private void ApplyPayrollPreviewHelpOptions(IReadOnlyCollection<PayrollPreviewHelpOptionDto> options)
    {
        foreach (var existingOption in PayrollPreviewHelpOptions)
        {
            existingOption.PropertyChanged -= OnPayrollPreviewHelpOptionPropertyChanged;
        }

        PayrollPreviewHelpOptions.Clear();

        foreach (var option in options)
        {
            var viewModel = new PayrollPreviewHelpToggleViewModel
            {
                Code = option.Code,
                Label = option.Label,
                IsEnabled = option.IsEnabled,
                HelpText = option.HelpText
            };
            viewModel.PropertyChanged += OnPayrollPreviewHelpOptionPropertyChanged;
            PayrollPreviewHelpOptions.Add(viewModel);
        }

        MonthlyRecord.ApplyPayrollPreviewHelpOptions(BuildPayrollPreviewHelpOptionDtos(PayrollPreviewHelpOptions));
    }

    private void OnPayrollPreviewHelpOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(PayrollPreviewHelpToggleViewModel.IsEnabled)
            and not nameof(PayrollPreviewHelpToggleViewModel.HelpText))
        {
            return;
        }

        MonthlyRecord.ApplyPayrollPreviewHelpOptions(BuildPayrollPreviewHelpOptionDtos(PayrollPreviewHelpOptions));
    }

    private static string? FormatNullablePercentage(decimal? value, string format)
    {
        return value.HasValue ? FormatPercentage(value.Value, format) : null;
    }

    private static Bitmap? TryLoadLogo(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static bool? ParseOptionalBoolean(string option)
    {
        return option switch
        {
            WithholdingTaxYes => true,
            WithholdingTaxNo => false,
            _ => null
        };
    }

    private static bool? ParseActivityFilter(string filter)
    {
        return filter switch
        {
            ActivityFilterActive => true,
            ActivityFilterInactive => false,
            _ => null
        };
    }

    private static string BuildContactSummary(string? city, string? country, string? email)
    {
        var location = string.Join(", ", new[] { city, country }.Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(location) && !string.IsNullOrWhiteSpace(email))
        {
            return $"{location} | {email}";
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            return location;
        }

        return string.IsNullOrWhiteSpace(email) ? "Keine Kontaktdaten" : email;
    }

    private void SwitchToTimeAndExpensesWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.TimeAndExpenses);
    }

    private void SwitchToEmployeesWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.Employees);
    }

    private void SwitchToPayrollRunsWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.PayrollRuns);
    }

    private void SwitchToReportingWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.Reporting);
    }

    private void SwitchToSettingsWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.Settings);
    }

    private void SwitchToHelpWorkspace()
    {
        SetWorkspaceSection(WorkspaceSection.Help);
    }

    private void SetWorkspaceSection(WorkspaceSection section)
    {
        if (_currentSection == section)
        {
            return;
        }

        _currentSection = section;
        RaisePropertyChanged(nameof(IsTimeAndExpensesWorkspace));
        RaisePropertyChanged(nameof(IsPayrollRunsWorkspace));
        RaisePropertyChanged(nameof(IsReportingWorkspace));
        RaisePropertyChanged(nameof(IsEmployeeWorkspace));
        RaisePropertyChanged(nameof(IsSettingsWorkspace));
        RaisePropertyChanged(nameof(IsHelpWorkspace));
        RaisePropertyChanged(nameof(ShowTimeAndExpensesWorkspace));
        RaisePropertyChanged(nameof(ShowPayrollRunsWorkspace));
        RaisePropertyChanged(nameof(ShowReportingWorkspace));
        RaisePropertyChanged(nameof(ShowEmployeeWorkspace));
        RaisePropertyChanged(nameof(ShowSettingsWorkspace));
        RaisePropertyChanged(nameof(ShowHelpWorkspace));
        RaisePropertyChanged(nameof(ShowEmployeeSelectionArea));
        RaisePropertyChanged(nameof(ShowPrimaryWorkspaceArea));
        RaisePropertyChanged(nameof(ShowPrimaryWorkspaceHeader));
        RaisePropertyChanged(nameof(CanSaveSettings));
        RaisePropertyChanged(nameof(CanCreatePayrollPdf));
        RaiseActionStateChanged();
    }
}
