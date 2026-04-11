using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Media.Imaging;
using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
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
    private string _backupDirectoryPath = string.Empty;
    private string _backupFileName = string.Empty;
    private string _selectedBackupContentType = BackupTypeBothLabel;
    private string _restoreFilePath = string.Empty;
    private string _selectedRestoreContentType = BackupTypeBothLabel;
    private string _appLogoText = ThemeTokens.BrandLogoText;
    private Bitmap? _appLogoImage;
    private string _statusMessage = "Mitarbeitende koennen links ausgewaehlt werden.";
    private bool _isBusy;
    private bool _isEditing;
    private bool _isCreatingNew;
    private bool _showDeleteConfirmation;
    private string _employeeCountSummary = "Keine Mitarbeitenden geladen.";
    private string _selectedMonthCaptureFilter = MonthCaptureFilterAll;
    private string _monthCaptureSummary = "Keine Stundenerfassungen geladen.";
    private IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto> _allMonthCaptureOverviewRows = [];
    private WorkspaceSection _currentSection = WorkspaceSection.TimeAndExpenses;

    public MainWindowViewModel(EmployeeService employeeService, IBackupRestoreService backupRestoreService, PayrollSettingsService payrollSettingsService, ReportingService reportingService, MonthlyRecordService monthlyRecordService, MonthlyRecordViewModel monthlyRecord, string workspaceLabel, string? databasePath = null, string? environmentName = null)
    {
        _employeeService = employeeService;
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
        MonthCaptureOverviewRows = [];
        ActivityFilters = [ActivityFilterAll, ActivityFilterActive, ActivityFilterInactive];
        MonthCaptureFilters = [MonthCaptureFilterAll, MonthCaptureFilterWithoutMonth, MonthCaptureFilterWithMonth];
        WithholdingTaxOptions = [WithholdingTaxUnknown, WithholdingTaxYes, WithholdingTaxNo];
        WageTypeOptions = [WageTypeHourlyLabel, WageTypeMonthlyLabel];
        BackupContentTypeOptions = [BackupTypeConfigurationLabel, BackupTypeUserDataLabel, BackupTypeBothLabel];
        DecimalSeparatorOptions = [",", "."];
        ThousandsSeparatorOptions = [ThousandsSeparatorApostropheLabel, ThousandsSeparatorSpaceLabel];
        RefreshCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        SearchCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        NewEmployeeCommand = new DelegateCommand(BeginCreateEmployee, () => CanStartCreate);
        EditEmployeeCommand = new DelegateCommand(BeginEditEmployee, () => CanStartEdit);
        SaveCommand = new DelegateCommand(SaveAsync, () => CanSave);
        CancelCommand = new DelegateCommand(CancelAsync, () => CanCancel);
        DeleteCommand = new DelegateCommand(RequestDelete, () => CanRequestDelete);
        ConfirmDeleteCommand = new DelegateCommand(ConfirmDeleteAsync, () => CanConfirmDelete);
        DismissDeleteCommand = new DelegateCommand(DismissDeleteConfirmation, () => CanDismissDelete);
        ClearExitDateCommand = new DelegateCommand(ClearExitDate, () => CanClearExitDate);
        ShowTimeAndExpensesCommand = new DelegateCommand(SwitchToTimeAndExpensesWorkspace, () => !IsTimeAndExpensesWorkspace);
        ShowPayrollRunsCommand = new DelegateCommand(SwitchToPayrollRunsWorkspace, () => !IsPayrollRunsWorkspace);
        ShowReportingCommand = new DelegateCommand(SwitchToReportingWorkspace, () => !IsReportingWorkspace);
        ShowEmployeesCommand = new DelegateCommand(SwitchToEmployeesWorkspace, () => !IsEmployeeWorkspace);
        ShowSettingsCommand = new DelegateCommand(SwitchToSettingsWorkspace, () => !IsSettingsWorkspace);
        ShowHelpCommand = new DelegateCommand(SwitchToHelpWorkspace, () => !IsHelpWorkspace);
        SaveSettingsCommand = new DelegateCommand(SaveSettingsAsync, () => CanSaveSettings);
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
    public ObservableCollection<MonthlyTimeCaptureOverviewRowDto> MonthCaptureOverviewRows { get; }
    public IReadOnlyList<string> ActivityFilters { get; }
    public IReadOnlyList<string> MonthCaptureFilters { get; }
    public IReadOnlyList<string> WithholdingTaxOptions { get; }
    public IReadOnlyList<string> WageTypeOptions { get; }
    public IReadOnlyList<string> BackupContentTypeOptions { get; }
    public IReadOnlyList<string> DecimalSeparatorOptions { get; }
    public IReadOnlyList<string> ThousandsSeparatorOptions { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand NewEmployeeCommand { get; }
    public DelegateCommand EditEmployeeCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand ConfirmDeleteCommand { get; }
    public DelegateCommand DismissDeleteCommand { get; }
    public DelegateCommand ClearExitDateCommand { get; }
    public DelegateCommand ShowTimeAndExpensesCommand { get; }
    public DelegateCommand ShowPayrollRunsCommand { get; }
    public DelegateCommand ShowReportingCommand { get; }
    public DelegateCommand ShowEmployeesCommand { get; }
    public DelegateCommand ShowSettingsCommand { get; }
    public DelegateCommand ShowHelpCommand { get; }
    public DelegateCommand SaveSettingsCommand { get; }
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
    public bool CanSave => !IsBusy && _isEditing;
    public bool CanCancel => !IsBusy && _isEditing;
    public bool CanRequestDelete => !IsBusy && _isEditing && _currentEmployeeId.HasValue && IsActiveEmployee;
    public bool CanConfirmDelete => !IsBusy && _showDeleteConfirmation && _currentEmployeeId.HasValue;
    public bool CanDismissDelete => !IsBusy && _showDeleteConfirmation;
    public bool CanClearExitDate => CanEditFields && ExitDate.HasValue;
    public bool CanSaveSettings => !IsBusy && IsSettingsWorkspace;
    public bool CanCreateBackup => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(BackupDirectoryPath) && !string.IsNullOrWhiteSpace(BackupFileName);
    public bool CanRestoreBackup => !IsBusy && IsSettingsWorkspace && !string.IsNullOrWhiteSpace(RestoreFilePath);
    public bool CanCreatePayrollPdf => !IsBusy && IsPayrollRunsWorkspace && _currentEmployeeId.HasValue && MonthlyRecord.SelectedMonth.HasValue;
    public bool CanManageSettingsOptions => !IsBusy && IsSettingsWorkspace;
    public bool CanRemoveDepartmentOption => CanManageSettingsOptions && SelectedSettingsDepartment is not null;
    public bool CanRemoveEmploymentCategoryOption => CanManageSettingsOptions && SelectedSettingsEmploymentCategory is not null;
    public bool CanRemoveEmploymentLocationOption => CanManageSettingsOptions && SelectedSettingsEmploymentLocation is not null;
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
            await MonthlyRecord.ReloadCurrentMonthAsync();
            StatusMessage = "Einstellungen gespeichert.";
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
        SetInteractionState(isEditing: false, isCreatingNew: false);
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
        ClearExitDateCommand.RaiseCanExecuteChanged();
        ShowTimeAndExpensesCommand.RaiseCanExecuteChanged();
        ShowPayrollRunsCommand.RaiseCanExecuteChanged();
        ShowReportingCommand.RaiseCanExecuteChanged();
        ShowEmployeesCommand.RaiseCanExecuteChanged();
        ShowSettingsCommand.RaiseCanExecuteChanged();
        SaveSettingsCommand.RaiseCanExecuteChanged();
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
