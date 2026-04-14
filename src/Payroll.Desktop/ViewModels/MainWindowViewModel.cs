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
    private static readonly string StartupArgumentsHelpText =
        "--db-path=/voller/pfad/zur/datei.db" + Environment.NewLine +
        "--environment=Development|Production|Test";

    private readonly EmployeeService _employeeService;
    private readonly ImportService _importService;
    private readonly IBackupRestoreService _backupRestoreService;
    private readonly PayrollSettingsService _payrollSettingsService;
    private readonly ReportingService _reportingService;
    private readonly MonthlyRecordService _monthlyRecordService;
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
    private DateTimeOffset? _settingsCalculationValidFrom;
    private DateTimeOffset? _settingsCalculationValidTo;
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
    private bool _showCalculationSettingsVersionDialog;
    private bool _showContractVersionCreateSection;
    private bool _showCalculationSettingsVersionCreateSection;
    private DateTimeOffset? _newContractVersionValidFrom;
    private DateTimeOffset? _newContractVersionValidTo;
    private Guid? _editingContractVersionId;
    private Guid? _editingCalculationSettingsVersionId;
    private Guid? _loadedCurrentContractId;
    private Guid? _loadedCurrentCalculationSettingsVersionId;
    private DateOnly? _loadedContractCurrentValidFrom;
    private DateOnly? _loadedCalculationSettingsCurrentValidFrom;
    private DateOnly? _loadedCalculationSettingsCurrentValidTo;
    private DateOnly? _loadedContractCurrentValidTo;
    private decimal? _loadedHourlyRateChf;
    private decimal? _loadedMonthlyBvgDeductionChf;
    private decimal? _loadedSpecialSupplementRateChf;
    private decimal? _loadedSettingsNightSupplementRate;
    private decimal? _loadedSettingsSundaySupplementRate;
    private decimal? _loadedSettingsHolidaySupplementRate;
    private decimal? _loadedSettingsAhvIvEoRate;
    private decimal? _loadedSettingsAlvRate;
    private decimal? _loadedSettingsSicknessAccidentInsuranceRate;
    private decimal? _loadedSettingsTrainingAndHolidayRate;
    private decimal? _loadedSettingsVacationCompensationRate;
    private decimal? _loadedSettingsVacationCompensationRateAge50Plus;
    private decimal? _loadedSettingsVehiclePauschalzone1RateChf;
    private decimal? _loadedSettingsVehiclePauschalzone2RateChf;
    private decimal? _loadedSettingsVehicleRegiezone1RateChf;
    private string _employeeCountSummary = "Keine Mitarbeitenden geladen.";
    private string _selectedMonthCaptureFilter = MonthCaptureFilterAll;
    private string _monthCaptureSummary = "Keine Stundenerfassungen geladen.";
    private IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto> _allMonthCaptureOverviewRows = [];
    private IReadOnlyCollection<PayrollCalculationSettingsVersionDto> _currentSettingsVersionSource = [];
    private IReadOnlyCollection<EmploymentContractVersionDto> _currentContractHistorySource = [];
    private EmploymentContractHistoryItemViewModel? _selectedContractHistoryEntry;
    private PayrollCalculationSettingsVersionItemViewModel? _selectedCalculationSettingsVersion;
    private WorkspaceSection _currentSection = WorkspaceSection.TimeAndExpenses;

    public MainWindowViewModel(EmployeeService employeeService, ImportService importService, IBackupRestoreService backupRestoreService, PayrollSettingsService payrollSettingsService, ReportingService reportingService, MonthlyRecordService monthlyRecordService, MonthlyRecordViewModel monthlyRecord, string workspaceLabel, string? databasePath = null, string? environmentName = null)
    {
        _employeeService = employeeService;
        _importService = importService;
        _backupRestoreService = backupRestoreService;
        _payrollSettingsService = payrollSettingsService;
        _reportingService = reportingService;
        _monthlyRecordService = monthlyRecordService;
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
        CalculationSettingsVersions = [];
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
        ConfirmContractVersionDialogCommand = new DelegateCommand(ConfirmContractVersionDialogAsync, () => CanConfirmContractVersionDialog);
        DismissContractVersionDialogCommand = new DelegateCommand(DismissContractVersionDialog, () => CanDismissContractVersionDialog);
        OpenContractVersionDialogCommand = new DelegateCommand(OpenContractVersionDialogFromButton, () => CanOpenContractVersionDialog);
        OpenNewContractVersionDialogCommand = new DelegateCommand(OpenNewContractVersionDialogFromButton, () => CanOpenNewContractVersionDialog);
        DeleteSelectedContractVersionCommand = new DelegateCommand(DeleteSelectedContractVersionAsync, () => CanDeleteSelectedContractVersion);
        OpenCalculationSettingsVersionDialogCommand = new DelegateCommand(OpenCalculationSettingsVersionDialog, () => CanOpenCalculationSettingsVersionDialog);
        OpenNewCalculationSettingsVersionDialogCommand = new DelegateCommand(OpenNewCalculationSettingsVersionDialog, () => CanOpenNewCalculationSettingsVersionDialog);
        ConfirmCalculationSettingsVersionDialogCommand = new DelegateCommand(ConfirmCalculationSettingsVersionDialogAsync, () => CanConfirmCalculationSettingsVersionDialog);
        DismissCalculationSettingsVersionDialogCommand = new DelegateCommand(DismissCalculationSettingsVersionDialog, () => CanDismissCalculationSettingsVersionDialog);
        DeleteSelectedCalculationSettingsVersionCommand = new DelegateCommand(DeleteSelectedCalculationSettingsVersionAsync, () => CanDeleteSelectedCalculationSettingsVersion);
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
    public ObservableCollection<PayrollCalculationSettingsVersionItemViewModel> CalculationSettingsVersions { get; }
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
    public DelegateCommand ConfirmContractVersionDialogCommand { get; }
    public DelegateCommand DismissContractVersionDialogCommand { get; }
    public DelegateCommand OpenContractVersionDialogCommand { get; }
    public DelegateCommand OpenNewContractVersionDialogCommand { get; }
    public DelegateCommand DeleteSelectedContractVersionCommand { get; }
    public DelegateCommand OpenCalculationSettingsVersionDialogCommand { get; }
    public DelegateCommand OpenNewCalculationSettingsVersionDialogCommand { get; }
    public DelegateCommand ConfirmCalculationSettingsVersionDialogCommand { get; }
    public DelegateCommand DismissCalculationSettingsVersionDialogCommand { get; }
    public DelegateCommand DeleteSelectedCalculationSettingsVersionCommand { get; }
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
    public bool CanConfirmContractVersionDialog => !IsBusy && ShowContractVersionDialog && ShowContractVersionCreateSection && NewContractVersionValidFrom.HasValue;
    public bool CanDismissContractVersionDialog => !IsBusy && ShowContractVersionDialog;
    public bool CanOpenContractVersionDialog => !IsBusy && _currentEmployeeId.HasValue;
    public bool CanOpenNewContractVersionDialog => !IsBusy && _isEditing && _currentEmployeeId.HasValue;
    public bool CanDeleteSelectedContractVersion => !IsBusy && ShowContractVersionDialog && SelectedContractHistoryEntry is not null;
    public bool CanOpenCalculationSettingsVersionDialog => !IsBusy && IsSettingsWorkspace && CalculationSettingsVersions.Count > 0;
    public bool CanOpenNewCalculationSettingsVersionDialog => !IsBusy && IsSettingsWorkspace && CalculationSettingsVersions.Count > 0;
    public bool CanConfirmCalculationSettingsVersionDialog => !IsBusy && ShowCalculationSettingsVersionDialog && ShowCalculationSettingsVersionCreateSection && SettingsCalculationValidFrom.HasValue;
    public bool CanDismissCalculationSettingsVersionDialog => !IsBusy && ShowCalculationSettingsVersionDialog;
    public bool CanDeleteSelectedCalculationSettingsVersion => !IsBusy && ShowCalculationSettingsVersionDialog && SelectedCalculationSettingsVersion is not null;
    public bool CanClearExitDate => CanEditFields && ExitDate.HasValue;
    public bool CanSaveSettings => !IsBusy && IsSettingsWorkspace && !ShowCalculationSettingsVersionDialog;
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
    public bool ShowContractHistoryOverlapWarning => ContractHistory.Any(item => item.HasOverlapWarning);
    public bool ShowContractVersionCreateSection => _showContractVersionCreateSection;
    public bool ShowCalculationSettingsVersionCreateSection => _showCalculationSettingsVersionCreateSection;
    public bool ShowCalculationSettingsOverlapWarning => CalculationSettingsVersions.Any(item => item.HasOverlapWarning);
    public bool ShowViewActions => !_isEditing;
    public bool ShowEditActions => _isEditing;
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

    public bool ShowCalculationSettingsVersionDialog
    {
        get => _showCalculationSettingsVersionDialog;
        private set
        {
            if (SetProperty(ref _showCalculationSettingsVersionDialog, value))
            {
                RaisePropertyChanged(nameof(CanSaveSettings));
                RaisePropertyChanged(nameof(CanConfirmCalculationSettingsVersionDialog));
                RaisePropertyChanged(nameof(CanDismissCalculationSettingsVersionDialog));
                RaisePropertyChanged(nameof(CanDeleteSelectedCalculationSettingsVersion));
                ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
                DismissCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
                DeleteSelectedCalculationSettingsVersionCommand.RaiseCanExecuteChanged();
            }
        }
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

    public bool IsEditingContractVersion => _editingContractVersionId.HasValue;
    public bool IsEditingCalculationSettingsVersion => _editingCalculationSettingsVersionId.HasValue;
    public string ContractVersionDialogTitle => !ShowContractVersionCreateSection && !IsEditingContractVersion
        ? "Vertragshistorie"
        : IsEditingContractVersion ? "Vertragsstand bearbeiten" : "Neuen Vertragsstand anlegen";
    public string ContractVersionDialogDescription => !ShowContractVersionCreateSection && !IsEditingContractVersion
        ? "Die Vertragshistorie wird getrennt von der Hauptmaske angezeigt. Der aktuelle Vertragsstand bleibt in der Mitarbeitendenmaske bearbeitbar."
        : IsEditingContractVersion
        ? "Der ausgewaehlte Vertragsstand wird direkt bearbeitet. Gueltig ab und Gueltig bis koennen innerhalb der Historie angepasst werden."
        : "Die Vertragshistorie wird separat angezeigt. Neue Vertragsstaende werden nur ueber den expliziten Schritt 'Neuer Vertragsstand ab Monat' angelegt.";
    public string ContractVersionDialogSummary => BuildContractVersionDialogSummary();
    public string ConfirmContractVersionDialogButtonText => IsEditingContractVersion ? "Stand speichern" : "Neuen Stand speichern";

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
        set
        {
            if (SetProperty(ref _contractValidFrom, value) && ShowContractVersionDialog && ShowContractVersionCreateSection && value.HasValue)
            {
                NewContractVersionValidFrom = value;
            }
        }
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

    public DateTimeOffset? SettingsCalculationValidFrom
    {
        get => _settingsCalculationValidFrom;
        set
        {
            if (SetProperty(ref _settingsCalculationValidFrom, value))
            {
                RaisePropertyChanged(nameof(CalculationSettingsVersionDialogSummary));
                RaisePropertyChanged(nameof(CanConfirmCalculationSettingsVersionDialog));
                ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTimeOffset? SettingsCalculationValidTo
    {
        get => _settingsCalculationValidTo;
        set
        {
            if (SetProperty(ref _settingsCalculationValidTo, value))
            {
                RaisePropertyChanged(nameof(CalculationSettingsVersionDialogSummary));
            }
        }
    }

    public string CalculationSettingsVersionDialogTitle => !ShowCalculationSettingsVersionCreateSection && !IsEditingCalculationSettingsVersion
        ? "Satzstandhistorie"
        : IsEditingCalculationSettingsVersion ? "Satzstand bearbeiten" : "Neuen Satzstand anlegen";
    public string CalculationSettingsVersionDialogDescription => !ShowCalculationSettingsVersionCreateSection && !IsEditingCalculationSettingsVersion
        ? "Die Satzstandhistorie wird getrennt von der Hauptmaske angezeigt. Der aktuell gueltige Stand bleibt im Einstellungen-Bereich bearbeitbar."
        : IsEditingCalculationSettingsVersion
        ? "Der ausgewaehlte Satzstand wird direkt bearbeitet. Gueltig ab und Gueltig bis koennen innerhalb der Historie angepasst werden."
        : "Die Satzstandhistorie wird separat angezeigt. Neue Satzstaende werden nur ueber den expliziten Schritt 'Neuer Satzstand ab Monat' angelegt.";
    public string CalculationSettingsVersionDialogSummary => BuildCalculationSettingsVersionDialogSummary();
    public string ConfirmCalculationSettingsVersionDialogButtonText => IsEditingCalculationSettingsVersion ? "Stand speichern" : "Neuen Stand speichern";

    public string CurrencyPrefix => $"{SettingsCurrencyCode} ";

    public EmploymentContractHistoryItemViewModel? SelectedContractHistoryEntry
    {
        get => _selectedContractHistoryEntry;
        set
        {
            SetProperty(ref _selectedContractHistoryEntry, value);

            RaisePropertyChanged(nameof(CanDeleteSelectedContractVersion));
            DeleteSelectedContractVersionCommand.RaiseCanExecuteChanged();
        }
    }

    public PayrollCalculationSettingsVersionItemViewModel? SelectedCalculationSettingsVersion
    {
        get => _selectedCalculationSettingsVersion;
        set
        {
            SetProperty(ref _selectedCalculationSettingsVersion, value);

            RaisePropertyChanged(nameof(CanDeleteSelectedCalculationSettingsVersion));
            DeleteSelectedCalculationSettingsVersionCommand.RaiseCanExecuteChanged();
        }
    }

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
        DismissCalculationSettingsVersionDialog();
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
        DismissCalculationSettingsVersionDialog();
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

            var hourlyRateChf = ParseRequiredDecimal(HourlyRateChf, nameof(HourlyRateChf));
            var monthlyBvgDeductionChf = ParseRequiredDecimal(MonthlyBvgDeductionChf, nameof(MonthlyBvgDeductionChf));
            var specialSupplementRateChf = ParseRequiredDecimal(SpecialSupplementRateChf, nameof(SpecialSupplementRateChf));

            await SaveEmployeeAsync(
                _loadedCurrentContractId,
                hourlyRateChf,
                monthlyBvgDeductionChf,
                specialSupplementRateChf,
                DateOnly.FromDateTime(ContractValidFrom.Value.Date),
                ContractValidTo.HasValue ? DateOnly.FromDateTime(ContractValidTo.Value.Date) : null);
        });
    }

    private async Task CancelAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            DismissDeleteConfirmation();
            DismissContractVersionDialog();
            DismissCalculationSettingsVersionDialog();

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
        _editingContractVersionId = null;
        NewContractVersionValidFrom = null;
        NewContractVersionValidTo = null;
        SelectedContractHistoryEntry = null;
        RaisePropertyChanged(nameof(IsEditingContractVersion));
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmContractVersionDialogButtonText));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
    }

    private void DismissCalculationSettingsVersionDialog()
    {
        ShowCalculationSettingsVersionDialog = false;
        if (_showCalculationSettingsVersionCreateSection && _loadedCalculationSettingsCurrentValidFrom.HasValue)
        {
            SettingsCalculationValidFrom = new DateTimeOffset(_loadedCalculationSettingsCurrentValidFrom.Value.ToDateTime(TimeOnly.MinValue));
            SettingsCalculationValidTo = _loadedCalculationSettingsCurrentValidTo.HasValue
                ? new DateTimeOffset(_loadedCalculationSettingsCurrentValidTo.Value.ToDateTime(TimeOnly.MinValue))
                : null;
        }

        _showCalculationSettingsVersionCreateSection = false;
        _editingCalculationSettingsVersionId = null;
        SelectedCalculationSettingsVersion = null;
        RaisePropertyChanged(nameof(IsEditingCalculationSettingsVersion));
        RaisePropertyChanged(nameof(ShowCalculationSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmCalculationSettingsVersionDialog));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogTitle));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogDescription));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmCalculationSettingsVersionDialogButtonText));
        ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
    }

    private void OpenContractVersionDialogFromButton()
    {
        if (!_currentEmployeeId.HasValue)
        {
            return;
        }

        DismissDeleteConfirmation();
        _showContractVersionCreateSection = false;
        _editingContractVersionId = null;
        SelectedContractHistoryEntry = ContractHistory.FirstOrDefault(item => item.IsCurrent) ?? ContractHistory.FirstOrDefault();
        ShowContractVersionDialog = true;
        RaisePropertyChanged(nameof(IsEditingContractVersion));
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmContractVersionDialogButtonText));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = "Vertragshistorie geoeffnet.";
    }

    private void OpenNewContractVersionDialogFromButton()
    {
        if (!_currentEmployeeId.HasValue || !_isEditing)
        {
            return;
        }

        OpenContractVersionDialog();
    }

    private void OpenCalculationSettingsVersionDialog()
    {
        OpenCalculationSettingsVersionDialog(showVersionPrompt: false);
    }

    private void OpenCalculationSettingsVersionDialog(bool showVersionPrompt)
    {
        if (CalculationSettingsVersions.Count == 0)
        {
            return;
        }

        DismissDeleteConfirmation();
        _showCalculationSettingsVersionCreateSection = showVersionPrompt;
        _editingCalculationSettingsVersionId = null;
        SelectedCalculationSettingsVersion = showVersionPrompt
            ? null
            : CalculationSettingsVersions.FirstOrDefault(item => item.IsCurrent) ?? CalculationSettingsVersions.FirstOrDefault();
        if (showVersionPrompt)
        {
            SettingsCalculationValidFrom = DetermineSuggestedNewCalculationSettingsValidFrom();
            SettingsCalculationValidTo = null;
        }

        ShowCalculationSettingsVersionDialog = true;
        RaisePropertyChanged(nameof(IsEditingCalculationSettingsVersion));
        RaisePropertyChanged(nameof(ShowCalculationSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmCalculationSettingsVersionDialog));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogTitle));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogDescription));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmCalculationSettingsVersionDialogButtonText));
        ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = showVersionPrompt
            ? "Neuer Satzstand wird vorbereitet. Gueltigkeit bestaetigen und anschliessend speichern."
            : "Satzstandhistorie geoeffnet.";
    }

    private void OpenNewCalculationSettingsVersionDialog()
    {
        OpenCalculationSettingsVersionDialog(showVersionPrompt: true);
    }

    private async Task ConfirmCalculationSettingsVersionDialogAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!SettingsCalculationValidFrom.HasValue)
            {
                throw new InvalidOperationException("Gueltig ab fuer den neuen Satzstand ist erforderlich.");
            }

            var newValidFrom = DateOnly.FromDateTime(SettingsCalculationValidFrom.Value.Date);
            var selectedVersionValidFrom = IsEditingCalculationSettingsVersion && SelectedCalculationSettingsVersion is not null
                ? DateOnly.FromDateTime(SelectedCalculationSettingsVersion.ValidFrom.Date)
                : (DateOnly?)null;

            if (!selectedVersionValidFrom.HasValue
                && _loadedCalculationSettingsCurrentValidFrom.HasValue
                && newValidFrom < _loadedCalculationSettingsCurrentValidFrom.Value)
            {
                throw new InvalidOperationException("Der neue Satzstand darf nicht vor dem Beginn des aktuell aktiven Standes starten.");
            }

            DateOnly? newValidTo = SettingsCalculationValidTo.HasValue
                ? DateOnly.FromDateTime(SettingsCalculationValidTo.Value.Date)
                : null;
            if (newValidTo.HasValue && newValidTo.Value < newValidFrom)
            {
                throw new InvalidOperationException("Gueltig bis darf nicht vor Gueltig ab liegen.");
            }

            await SaveSettingsCoreAsync(closeVersionDialogOnSuccess: true);
        });
    }

    private void ClearExitDate()
    {
        ExitDate = null;
    }

    private async Task ConfirmContractVersionDialogAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!NewContractVersionValidFrom.HasValue)
            {
                throw new InvalidOperationException("Gueltig ab fuer den neuen Vertragsstand ist erforderlich.");
            }

            var newValidFrom = DateOnly.FromDateTime(NewContractVersionValidFrom.Value.Date);
            var selectedContractValidFrom = IsEditingContractVersion && SelectedContractHistoryEntry is not null
                ? DateOnly.FromDateTime(SelectedContractHistoryEntry.ValidFrom.Date)
                : (DateOnly?)null;

            if (!selectedContractValidFrom.HasValue
                && _loadedContractCurrentValidFrom.HasValue
                && newValidFrom <= _loadedContractCurrentValidFrom.Value)
            {
                throw new InvalidOperationException("Der neue Vertragsstand muss nach dem Beginn des aktuell aktiven Standes starten.");
            }

            DateOnly? newValidTo = NewContractVersionValidTo.HasValue
                ? DateOnly.FromDateTime(NewContractVersionValidTo.Value.Date)
                : null;
            if (newValidTo.HasValue && newValidTo.Value < newValidFrom)
            {
                throw new InvalidOperationException("Gueltig bis darf nicht vor Gueltig ab liegen.");
            }

            var hourlyRateChf = ParseRequiredDecimal(HourlyRateChf, nameof(HourlyRateChf));
            var monthlyBvgDeductionChf = ParseRequiredDecimal(MonthlyBvgDeductionChf, nameof(MonthlyBvgDeductionChf));
            var specialSupplementRateChf = ParseRequiredDecimal(SpecialSupplementRateChf, nameof(SpecialSupplementRateChf));

            await SaveEmployeeAsync(
                IsEditingContractVersion ? _editingContractVersionId : null,
                hourlyRateChf,
                monthlyBvgDeductionChf,
                specialSupplementRateChf,
                newValidFrom,
                newValidTo);
        });
    }

    private async Task DeleteSelectedContractVersionAsync()
    {
        if (SelectedContractHistoryEntry is null)
        {
            return;
        }

        await DeleteContractVersionAsync(SelectedContractHistoryEntry.ContractId);
    }

    private async Task DeleteSelectedCalculationSettingsVersionAsync()
    {
        if (SelectedCalculationSettingsVersion is null)
        {
            return;
        }

        await DeleteCalculationSettingsVersionAsync(SelectedCalculationSettingsVersion.VersionId);
    }

    private async Task DeleteContractVersionAsync(Guid contractId)
    {
        await ExecuteBusyAsync(async () =>
        {
            if (!_currentEmployeeId.HasValue)
            {
                return;
            }

            var updated = await _employeeService.DeleteContractVersionAsync(_currentEmployeeId.Value, contractId);
            PopulateForm(updated);
            ShowContractVersionDialog = true;
            StatusMessage = "Vertragsstand geloescht.";
        });
    }

    private async Task DeleteCalculationSettingsVersionAsync(Guid versionId)
    {
        await ExecuteBusyAsync(async () =>
        {
            var updated = await _payrollSettingsService.DeleteCalculationVersionAsync(versionId);
            ApplySettings(updated);
            ShowCalculationSettingsVersionDialog = true;
            StatusMessage = "Satzstand geloescht.";
        });
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
            await SaveSettingsCoreAsync(closeVersionDialogOnSuccess: false);
        });
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
        _loadedContractCurrentValidFrom = employee.ContractValidFrom;
        _loadedContractCurrentValidTo = employee.ContractValidTo;
        _loadedHourlyRateChf = employee.HourlyRateChf;
        _loadedMonthlyBvgDeductionChf = employee.MonthlyBvgDeductionChf;
        _loadedSpecialSupplementRateChf = employee.SpecialSupplementRateChf;
        _loadedCurrentContractId = employee.ContractHistory.FirstOrDefault(item => item.IsCurrent)?.ContractId;
        DismissContractVersionDialog();
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
        ContractValidFrom = new DateTimeOffset(DateTime.Today);
        ContractValidTo = null;
        HourlyRateChf = NumericFormatManager.FormatDecimal(0m, "0");
        MonthlyBvgDeductionChf = NumericFormatManager.FormatDecimal(0m, "0");
        SpecialSupplementRateChf = NumericFormatManager.FormatDecimal(3m, "0.00");
        _loadedContractCurrentValidFrom = null;
        _loadedContractCurrentValidTo = null;
        _loadedHourlyRateChf = null;
        _loadedMonthlyBvgDeductionChf = null;
        _loadedSpecialSupplementRateChf = null;
        _loadedCurrentContractId = null;
        ContractHistory.Clear();
        SelectedContractHistoryEntry = null;
        DismissContractVersionDialog();
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
        ContractValidFrom = null;
        ContractValidTo = null;
        HourlyRateChf = string.Empty;
        MonthlyBvgDeductionChf = string.Empty;
        SpecialSupplementRateChf = string.Empty;
        _loadedContractCurrentValidFrom = null;
        _loadedContractCurrentValidTo = null;
        _loadedHourlyRateChf = null;
        _loadedMonthlyBvgDeductionChf = null;
        _loadedSpecialSupplementRateChf = null;
        _loadedCurrentContractId = null;
        ContractHistory.Clear();
        SelectedContractHistoryEntry = null;
        DismissContractVersionDialog();
        SetInteractionState(isEditing: false, isCreatingNew: false);
    }

    private void SetInteractionState(bool isEditing, bool isCreatingNew)
    {
        _isEditing = isEditing;
        _isCreatingNew = isCreatingNew;
        MonthlyRecord.IsLocked = isEditing && IsEmployeeWorkspace;
        if (!_isEditing)
        {
            DismissDeleteConfirmation();
            DismissContractVersionDialog();
            DismissCalculationSettingsVersionDialog();
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
        RaisePropertyChanged(nameof(ShowViewActions));
        RaisePropertyChanged(nameof(ShowEditActions));
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
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        DismissContractVersionDialogCommand.RaiseCanExecuteChanged();
        OpenContractVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewContractVersionDialogCommand.RaiseCanExecuteChanged();
        DeleteSelectedContractVersionCommand.RaiseCanExecuteChanged();
        OpenCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        OpenNewCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        DismissCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        DeleteSelectedCalculationSettingsVersionCommand.RaiseCanExecuteChanged();
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
        _loadedCalculationSettingsCurrentValidFrom = settings.CalculationValidFrom;
        _loadedCalculationSettingsCurrentValidTo = settings.CalculationValidTo;
        SettingsCalculationValidFrom = new DateTimeOffset(settings.CalculationValidFrom.ToDateTime(TimeOnly.MinValue));
        SettingsCalculationValidTo = settings.CalculationValidTo.HasValue
            ? new DateTimeOffset(settings.CalculationValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
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
        _loadedSettingsNightSupplementRate = settings.NightSupplementRate;
        _loadedSettingsSundaySupplementRate = settings.SundaySupplementRate;
        _loadedSettingsHolidaySupplementRate = settings.HolidaySupplementRate;
        _loadedSettingsAhvIvEoRate = settings.AhvIvEoRate;
        _loadedSettingsAlvRate = settings.AlvRate;
        _loadedSettingsSicknessAccidentInsuranceRate = settings.SicknessAccidentInsuranceRate;
        _loadedSettingsTrainingAndHolidayRate = settings.TrainingAndHolidayRate;
        _loadedSettingsVacationCompensationRate = settings.VacationCompensationRate;
        _loadedSettingsVacationCompensationRateAge50Plus = settings.VacationCompensationRateAge50Plus;
        _loadedSettingsVehiclePauschalzone1RateChf = settings.VehiclePauschalzone1RateChf;
        _loadedSettingsVehiclePauschalzone2RateChf = settings.VehiclePauschalzone2RateChf;
        _loadedSettingsVehicleRegiezone1RateChf = settings.VehicleRegiezone1RateChf;
        ApplyCalculationSettingsVersions(settings.CalculationVersions);
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

    private void ApplyContractHistory(IReadOnlyCollection<EmploymentContractVersionDto> history)
    {
        _currentContractHistorySource = history.ToArray();
        ContractHistory.Clear();
        var overlappingContractIds = DetermineOverlappingContractIds(_currentContractHistorySource);

        foreach (var version in _currentContractHistorySource)
        {
            ContractHistory.Add(new EmploymentContractHistoryItemViewModel
            {
                ContractId = version.ContractId,
                ValidFrom = new DateTimeOffset(version.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                ValidTo = version.ValidTo.HasValue
                    ? new DateTimeOffset(version.ValidTo.Value.ToDateTime(TimeOnly.MinValue))
                    : null,
                HourlyRateDisplay = $"{version.HourlyRateChf:0.00} CHF/h",
                MonthlyBvgDisplay = $"{version.MonthlyBvgDeductionChf:0.00} CHF",
                SpecialSupplementDisplay = $"{version.SpecialSupplementRateChf:0.00} CHF/h",
                Summary = $"{version.ValidFrom:dd.MM.yyyy} bis {FormatDate(version.ValidTo)} | {version.HourlyRateChf:0.00} CHF/h | BVG {version.MonthlyBvgDeductionChf:0.00} CHF | Spezial {version.SpecialSupplementRateChf:0.00} CHF/h",
                WarningText = overlappingContractIds.Contains(version.ContractId)
                    ? "Dieser Vertragsstand ueberschneidet sich mit einem anderen Gueltigkeitszeitraum."
                    : string.Empty,
                LoadToEditorCommand = new DelegateCommand(() => LoadContractHistoryEntryToEditor(version.ContractId, continueEditing: false)),
                ContinueEditingCommand = new DelegateCommand(() => LoadContractHistoryEntryToEditor(version.ContractId, continueEditing: true)),
                DeleteCommand = new DelegateCommand(() => DeleteContractVersionAsync(version.ContractId), () => !IsBusy),
                IsCurrent = version.IsCurrent
            });
        }

        SelectedContractHistoryEntry = ContractHistory.FirstOrDefault(item => item.IsCurrent) ?? ContractHistory.FirstOrDefault();
        RaisePropertyChanged(nameof(ShowContractHistoryOverlapWarning));
    }

    private bool RequiresNewContractVersion(decimal hourlyRateChf, decimal monthlyBvgDeductionChf, decimal specialSupplementRateChf)
    {
        return _currentEmployeeId.HasValue
            && _loadedContractCurrentValidFrom.HasValue
            && (_loadedHourlyRateChf != hourlyRateChf
                || _loadedMonthlyBvgDeductionChf != monthlyBvgDeductionChf
                || _loadedSpecialSupplementRateChf != specialSupplementRateChf);
    }

    private void OpenContractVersionDialog()
    {
        DismissDeleteConfirmation();
        _showContractVersionCreateSection = true;
        _editingContractVersionId = null;
        SelectedContractHistoryEntry = null;
        NewContractVersionValidFrom = DetermineSuggestedNewContractVersionValidFrom();
        NewContractVersionValidTo = null;
        ShowContractVersionDialog = true;
        RaisePropertyChanged(nameof(IsEditingContractVersion));
        RaisePropertyChanged(nameof(ShowContractVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmContractVersionDialog));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmContractVersionDialogButtonText));
        ConfirmContractVersionDialogCommand.RaiseCanExecuteChanged();
        RaisePropertyChanged(nameof(ConfirmContractVersionDialogButtonText));
        StatusMessage = "Historisierte Vertragswerte wurden geaendert. Bitte neuen Vertragsstand bestaetigen.";
    }

    private DateTimeOffset DetermineSuggestedNewContractVersionValidFrom()
    {
        if (ContractValidFrom.HasValue
            && _loadedContractCurrentValidFrom.HasValue
            && DateOnly.FromDateTime(ContractValidFrom.Value.Date) > _loadedContractCurrentValidFrom.Value)
        {
            return ContractValidFrom.Value;
        }

        var fallback = _loadedContractCurrentValidFrom?.AddMonths(1)
            ?? DateOnly.FromDateTime(DateTime.Today);

        return new DateTimeOffset(fallback.ToDateTime(TimeOnly.MinValue));
    }

    private async Task SaveEmployeeAsync(
        Guid? editingContractId,
        decimal hourlyRateChf,
        decimal monthlyBvgDeductionChf,
        decimal specialSupplementRateChf,
        DateOnly contractValidFrom,
        DateOnly? contractValidTo)
    {
        var command = new SaveEmployeeCommand(
            _currentEmployeeId,
            editingContractId,
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
            contractValidFrom,
            contractValidTo,
            hourlyRateChf,
            monthlyBvgDeductionChf,
            specialSupplementRateChf);

        var saved = await _employeeService.SaveAsync(command);
        DismissContractVersionDialog();
        _currentEmployeeId = saved.EmployeeId;
        _returnEmployeeId = saved.EmployeeId;
        SetInteractionState(isEditing: false, isCreatingNew: false);
        await ReloadEmployeesAsync();
        await RestoreSelectionAfterReloadAsync(saved.EmployeeId, selectFirstIfMissing: true);
        StatusMessage = $"Mitarbeitender {saved.PersonnelNumber} gespeichert.";
    }

    private string BuildContractVersionDialogSummary()
    {
        if (!ShowContractVersionCreateSection && !IsEditingContractVersion)
        {
            return "Historische Vertragsstaende werden hier nur angezeigt. Neue Versionen entstehen ausschliesslich ueber 'Neuer Vertragsstand ab Monat'.";
        }

        if (!NewContractVersionValidFrom.HasValue)
        {
            return "Beim Speichern wird ein neuer Vertragsstand angelegt.";
        }

        if (IsEditingContractVersion)
        {
            var rangeEnd = NewContractVersionValidTo.HasValue
                ? FormatDate(NewContractVersionValidTo)
                : "offen";
            return $"Der ausgewaehlte Vertragsstand wird direkt auf {FormatDate(NewContractVersionValidFrom)} bis {rangeEnd} angepasst.";
        }

        if (!_loadedContractCurrentValidFrom.HasValue)
        {
            return "Beim Speichern wird ein neuer Vertragsstand angelegt.";
        }

        var previousEnd = NewContractVersionValidFrom.Value.AddDays(-1);
        var newRangeEnd = NewContractVersionValidTo.HasValue
            ? FormatDate(NewContractVersionValidTo)
            : "offen";

        return $"Der bisher aktive Stand endet automatisch am {FormatDate(previousEnd)}. Der neue Stand gilt von {FormatDate(NewContractVersionValidFrom)} bis {newRangeEnd}.";
    }

    private string BuildCalculationSettingsVersionDialogSummary()
    {
        if (!SettingsCalculationValidFrom.HasValue)
        {
            return "Beim Speichern wird ein neuer Satzstand angelegt.";
        }

        if (IsEditingCalculationSettingsVersion)
        {
            var rangeEnd = SettingsCalculationValidTo.HasValue
                ? FormatDate(SettingsCalculationValidTo)
                : "offen";
            return $"Der ausgewaehlte Satzstand wird direkt auf {FormatDate(SettingsCalculationValidFrom)} bis {rangeEnd} angepasst.";
        }

        if (!_loadedCalculationSettingsCurrentValidFrom.HasValue)
        {
            return "Beim Speichern wird ein neuer Satzstand angelegt.";
        }

        var previousEnd = SettingsCalculationValidFrom.Value.AddDays(-1);
        var newRangeEnd = SettingsCalculationValidTo.HasValue
            ? FormatDate(SettingsCalculationValidTo)
            : "offen";

        return $"Der bisher aktive Stand endet automatisch am {FormatDate(previousEnd)}. Der neue Stand gilt von {FormatDate(SettingsCalculationValidFrom)} bis {newRangeEnd}.";
    }

    private bool RequiresNewCalculationSettingsVersion()
    {
        if (!_loadedCalculationSettingsCurrentValidFrom.HasValue)
        {
            return false;
        }

        return _loadedSettingsNightSupplementRate != ParseOptionalPercentage(SettingsNightSupplementRate)
            || _loadedSettingsSundaySupplementRate != ParseOptionalPercentage(SettingsSundaySupplementRate)
            || _loadedSettingsHolidaySupplementRate != ParseOptionalPercentage(SettingsHolidaySupplementRate)
            || _loadedSettingsAhvIvEoRate != ParseRequiredPercentage(SettingsAhvIvEoRate, nameof(SettingsAhvIvEoRate))
            || _loadedSettingsAlvRate != ParseRequiredPercentage(SettingsAlvRate, nameof(SettingsAlvRate))
            || _loadedSettingsSicknessAccidentInsuranceRate != ParseRequiredPercentage(SettingsSicknessAccidentInsuranceRate, nameof(SettingsSicknessAccidentInsuranceRate))
            || _loadedSettingsTrainingAndHolidayRate != ParseRequiredPercentage(SettingsTrainingAndHolidayRate, nameof(SettingsTrainingAndHolidayRate))
            || _loadedSettingsVacationCompensationRate != ParseRequiredPercentage(SettingsVacationCompensationRate, nameof(SettingsVacationCompensationRate))
            || _loadedSettingsVacationCompensationRateAge50Plus != ParseRequiredPercentage(SettingsVacationCompensationRateAge50Plus, nameof(SettingsVacationCompensationRateAge50Plus))
            || _loadedSettingsVehiclePauschalzone1RateChf != ParseRequiredDecimal(SettingsVehiclePauschalzone1RateChf, nameof(SettingsVehiclePauschalzone1RateChf))
            || _loadedSettingsVehiclePauschalzone2RateChf != ParseRequiredDecimal(SettingsVehiclePauschalzone2RateChf, nameof(SettingsVehiclePauschalzone2RateChf))
            || _loadedSettingsVehicleRegiezone1RateChf != ParseRequiredDecimal(SettingsVehicleRegiezone1RateChf, nameof(SettingsVehicleRegiezone1RateChf));
    }

    private DateTimeOffset DetermineSuggestedNewCalculationSettingsValidFrom()
    {
        if (SettingsCalculationValidFrom.HasValue
            && _loadedCalculationSettingsCurrentValidFrom.HasValue
            && DateOnly.FromDateTime(SettingsCalculationValidFrom.Value.Date) > _loadedCalculationSettingsCurrentValidFrom.Value)
        {
            return SettingsCalculationValidFrom.Value;
        }

        var fallback = _loadedCalculationSettingsCurrentValidFrom?.AddMonths(1)
            ?? DateOnly.FromDateTime(DateTime.Today);

        return new DateTimeOffset(fallback.ToDateTime(TimeOnly.MinValue));
    }

    private void ApplyCalculationSettingsVersions(IReadOnlyCollection<PayrollCalculationSettingsVersionDto> versions)
    {
        _currentSettingsVersionSource = versions.ToArray();
        CalculationSettingsVersions.Clear();
        var overlappingVersionIds = DetermineOverlappingCalculationVersionIds(_currentSettingsVersionSource);

        foreach (var version in _currentSettingsVersionSource)
        {
            CalculationSettingsVersions.Add(new PayrollCalculationSettingsVersionItemViewModel
            {
                VersionId = version.VersionId,
                ValidFrom = new DateTimeOffset(version.ValidFrom.ToDateTime(TimeOnly.MinValue)),
                ValidTo = version.ValidTo.HasValue
                    ? new DateTimeOffset(version.ValidTo.Value.ToDateTime(TimeOnly.MinValue))
                    : null,
                SupplementSummary = $"N {FormatNullablePercentage(version.NightSupplementRate, "0.##")} | S {FormatNullablePercentage(version.SundaySupplementRate, "0.##")} | F {FormatNullablePercentage(version.HolidaySupplementRate, "0.##")}",
                DeductionSummary = $"AHV {FormatPercentage(version.AhvIvEoRate, "0.###")} | ALV {FormatPercentage(version.AlvRate, "0.###")} | UVG {FormatPercentage(version.SicknessAccidentInsuranceRate, "0.###")}",
                VacationSummary = $"Ferien {FormatPercentage(version.VacationCompensationRate, "0.##")} | ab 50 {FormatPercentage(version.VacationCompensationRateAge50Plus, "0.##")}",
                VehicleSummary = $"P1 {NumericFormatManager.FormatDecimal(version.VehiclePauschalzone1RateChf, "0.##")} | P2 {NumericFormatManager.FormatDecimal(version.VehiclePauschalzone2RateChf, "0.##")} | R1 {NumericFormatManager.FormatDecimal(version.VehicleRegiezone1RateChf, "0.##")}",
                Summary = $"{version.ValidFrom:dd.MM.yyyy} bis {FormatDate(version.ValidTo)}",
                WarningText = overlappingVersionIds.Contains(version.VersionId)
                    ? "Dieser Satzstand ueberschneidet sich mit einem anderen Gueltigkeitszeitraum."
                    : string.Empty,
                LoadToEditorCommand = new DelegateCommand(() => LoadCalculationSettingsVersionToEditor(version.VersionId, continueEditing: false)),
                ContinueEditingCommand = new DelegateCommand(() => LoadCalculationSettingsVersionToEditor(version.VersionId, continueEditing: true)),
                DeleteCommand = new DelegateCommand(() => DeleteCalculationSettingsVersionAsync(version.VersionId), () => !IsBusy),
                IsCurrent = version.IsCurrent
            });
        }

        SelectedCalculationSettingsVersion = CalculationSettingsVersions.FirstOrDefault(item => item.IsCurrent) ?? CalculationSettingsVersions.FirstOrDefault();
        _loadedCurrentCalculationSettingsVersionId = SelectedCalculationSettingsVersion?.VersionId;
        RaisePropertyChanged(nameof(ShowCalculationSettingsOverlapWarning));
    }

    private async Task SaveSettingsCoreAsync(bool closeVersionDialogOnSuccess)
    {
        var editingCalculationVersionId = closeVersionDialogOnSuccess
            ? _editingCalculationSettingsVersionId
            : _loadedCurrentCalculationSettingsVersionId;
        var calculationValidFrom = SettingsCalculationValidFrom;
        var calculationValidTo = SettingsCalculationValidTo;

        if (!calculationValidFrom.HasValue)
        {
            throw new InvalidOperationException("Gueltig ab fuer die Berechnungssaetze ist erforderlich.");
        }

        var saved = await _payrollSettingsService.SaveAsync(new SavePayrollSettingsCommand(
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
            editingCalculationVersionId,
            DateOnly.FromDateTime(calculationValidFrom.Value.Date),
            calculationValidTo.HasValue ? DateOnly.FromDateTime(calculationValidTo.Value.Date) : null,
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
            BuildSettingOptionDtos(EmploymentLocationOptions)));

        ApplySettings(saved);
        if (closeVersionDialogOnSuccess)
        {
            DismissCalculationSettingsVersionDialog();
        }

        await MonthlyRecord.ReloadCurrentMonthAsync();
        StatusMessage = "Einstellungen gespeichert.";
    }

    private void LoadContractHistoryEntryToEditor(Guid contractId, bool continueEditing)
    {
        var item = ContractHistory.FirstOrDefault(entry => entry.ContractId == contractId);
        if (item is null)
        {
            return;
        }

        SwitchToEmployeesWorkspace();
        SelectedContractHistoryEntry = item;
        _editingContractVersionId = continueEditing ? item.ContractId : null;
        NewContractVersionValidFrom = item.ValidFrom;
        NewContractVersionValidTo = item.ValidTo;
        ShowContractVersionDialog = true;
        RaisePropertyChanged(nameof(IsEditingContractVersion));
        RaisePropertyChanged(nameof(ContractVersionDialogTitle));
        RaisePropertyChanged(nameof(ContractVersionDialogDescription));
        RaisePropertyChanged(nameof(ContractVersionDialogSummary));

        if (continueEditing && !_isEditing)
        {
            BeginEditEmployee();
        }

        StatusMessage = continueEditing
            ? "Vertragsstand in den Bearbeitungsbereich geladen. Aenderungen koennen jetzt gespeichert werden."
            : "Vertragsstand in den Bearbeitungsbereich geladen.";
    }

    private void LoadCalculationSettingsVersionToEditor(Guid versionId, bool continueEditing)
    {
        var item = CalculationSettingsVersions.FirstOrDefault(entry => entry.VersionId == versionId);
        if (item is null)
        {
            return;
        }

        SwitchToSettingsWorkspace();
        SelectedCalculationSettingsVersion = item;
        _showCalculationSettingsVersionCreateSection = continueEditing;
        _editingCalculationSettingsVersionId = continueEditing ? item.VersionId : null;
        ShowCalculationSettingsVersionDialog = true;
        RaisePropertyChanged(nameof(IsEditingCalculationSettingsVersion));
        RaisePropertyChanged(nameof(ShowCalculationSettingsVersionCreateSection));
        RaisePropertyChanged(nameof(CanConfirmCalculationSettingsVersionDialog));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogTitle));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogDescription));
        RaisePropertyChanged(nameof(CalculationSettingsVersionDialogSummary));
        RaisePropertyChanged(nameof(ConfirmCalculationSettingsVersionDialogButtonText));
        ConfirmCalculationSettingsVersionDialogCommand.RaiseCanExecuteChanged();
        StatusMessage = continueEditing
            ? "Satzstand in den Bearbeitungsbereich geladen. Gueltigkeit bei Bedarf anpassen und speichern."
            : "Satzstand in den Bearbeitungsbereich geladen.";
    }

    private static HashSet<Guid> DetermineOverlappingContractIds(IReadOnlyCollection<EmploymentContractVersionDto> history)
    {
        return DetermineOverlappingIds(
            history,
            item => item.ContractId,
            item => item.ValidFrom,
            item => item.ValidTo);
    }

    private static HashSet<Guid> DetermineOverlappingCalculationVersionIds(IReadOnlyCollection<PayrollCalculationSettingsVersionDto> history)
    {
        return DetermineOverlappingIds(
            history,
            item => item.VersionId,
            item => item.ValidFrom,
            item => item.ValidTo);
    }

    private static HashSet<Guid> DetermineOverlappingIds<TItem>(
        IReadOnlyCollection<TItem> history,
        Func<TItem, Guid> idSelector,
        Func<TItem, DateOnly> validFromSelector,
        Func<TItem, DateOnly?> validToSelector)
    {
        var items = history.ToArray();
        var overlappingIds = new HashSet<Guid>();

        for (var index = 0; index < items.Length; index++)
        {
            for (var compareIndex = index + 1; compareIndex < items.Length; compareIndex++)
            {
                if (!PeriodsOverlap(
                        validFromSelector(items[index]),
                        validToSelector(items[index]),
                        validFromSelector(items[compareIndex]),
                        validToSelector(items[compareIndex])))
                {
                    continue;
                }

                overlappingIds.Add(idSelector(items[index]));
                overlappingIds.Add(idSelector(items[compareIndex]));
            }
        }

        return overlappingIds;
    }

    private static bool PeriodsOverlap(DateOnly firstFrom, DateOnly? firstTo, DateOnly secondFrom, DateOnly? secondTo)
    {
        var normalizedFirstTo = firstTo ?? DateOnly.MaxValue;
        var normalizedSecondTo = secondTo ?? DateOnly.MaxValue;
        return firstFrom <= normalizedSecondTo && secondFrom <= normalizedFirstTo;
    }

    private static string FormatPercentage(decimal value, string format)
    {
        return NumericFormatManager.FormatDecimal(value * 100m, format);
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value.HasValue
            ? value.Value.ToString("dd.MM.yyyy")
            : "offen";
    }

    private static string FormatDate(DateOnly? value)
    {
        return value.HasValue
            ? value.Value.ToString("dd.MM.yyyy")
            : "offen";
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
        MonthlyRecord.IsLocked = _isEditing && section == WorkspaceSection.Employees;
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
