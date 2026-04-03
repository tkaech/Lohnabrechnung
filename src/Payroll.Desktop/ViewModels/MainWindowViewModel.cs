using System.Collections.ObjectModel;
using Payroll.Application.Employees;

namespace Payroll.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string ActivityFilterAll = "Alle";
    private const string ActivityFilterActive = "Aktiv";
    private const string ActivityFilterInactive = "Inaktiv";
    private const string WithholdingTaxUnknown = "Ungeklaert";
    private const string WithholdingTaxYes = "Ja";
    private const string WithholdingTaxNo = "Nein";

    private readonly EmployeeService _employeeService;
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
    private DateTimeOffset? _contractValidFrom;
    private DateTimeOffset? _contractValidTo;
    private string _hourlyRateChf = string.Empty;
    private string _monthlyBvgDeductionChf = string.Empty;
    private string? _nightSupplementRate;
    private string? _sundaySupplementRate;
    private string? _holidaySupplementRate;
    private string _statusMessage = "Mitarbeitende koennen links ausgewaehlt werden.";
    private bool _isBusy;
    private bool _isEditing;
    private bool _isCreatingNew;
    private bool _showDeleteConfirmation;
    private string _employeeCountSummary = "Keine Mitarbeitenden geladen.";

    public MainWindowViewModel(EmployeeService employeeService, string workspaceLabel)
    {
        _employeeService = employeeService;
        WorkspaceLabel = workspaceLabel;
        Employees = [];
        ActivityFilters = [ActivityFilterAll, ActivityFilterActive, ActivityFilterInactive];
        WithholdingTaxOptions = [WithholdingTaxUnknown, WithholdingTaxYes, WithholdingTaxNo];
        RefreshCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        SearchCommand = new DelegateCommand(RefreshAsync, () => CanSearchEmployees);
        NewEmployeeCommand = new DelegateCommand(BeginCreateEmployee, () => CanStartCreate);
        EditEmployeeCommand = new DelegateCommand(BeginEditEmployee, () => CanStartEdit);
        SaveCommand = new DelegateCommand(SaveAsync, () => CanSave);
        CancelCommand = new DelegateCommand(CancelAsync, () => CanCancel);
        DeleteCommand = new DelegateCommand(RequestDelete, () => CanRequestDelete);
        ConfirmDeleteCommand = new DelegateCommand(ConfirmDeleteAsync, () => CanConfirmDelete);
        DismissDeleteCommand = new DelegateCommand(DismissDeleteConfirmation, () => CanDismissDelete);

        ClearFormForEmptyState();
    }

    public string Title => "PayrollApp - Mitarbeitende";
    public string WorkspaceLabel { get; }
    public ObservableCollection<EmployeeListItemViewModel> Employees { get; }
    public IReadOnlyList<string> ActivityFilters { get; }
    public IReadOnlyList<string> WithholdingTaxOptions { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand NewEmployeeCommand { get; }
    public DelegateCommand EditEmployeeCommand { get; }
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand ConfirmDeleteCommand { get; }
    public DelegateCommand DismissDeleteCommand { get; }

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
        set => SetProperty(ref _exitDate, value);
    }

    public bool IsActiveEmployee
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public string? NightSupplementRate
    {
        get => _nightSupplementRate;
        set => SetProperty(ref _nightSupplementRate, value);
    }

    public string? SundaySupplementRate
    {
        get => _sundaySupplementRate;
        set => SetProperty(ref _sundaySupplementRate, value);
    }

    public string? HolidaySupplementRate
    {
        get => _holidaySupplementRate;
        set => SetProperty(ref _holidaySupplementRate, value);
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
                RaiseActionStateChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshAsync();

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
            StatusMessage = $"{Employees.Count} Mitarbeitende geladen.";
        });
    }

    private void BeginCreateEmployee()
    {
        DismissDeleteConfirmation();
        _returnEmployeeId = _currentEmployeeId;
        SelectedEmployee = null;
        _currentEmployeeId = null;
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
                DateOnly.FromDateTime(ContractValidFrom.Value.Date),
                ContractValidTo.HasValue ? DateOnly.FromDateTime(ContractValidTo.Value.Date) : null,
                ParseRequiredDecimal(HourlyRateChf, nameof(HourlyRateChf)),
                ParseRequiredDecimal(MonthlyBvgDeductionChf, nameof(MonthlyBvgDeductionChf)),
                ParseOptionalDecimal(NightSupplementRate),
                ParseOptionalDecimal(SundaySupplementRate),
                ParseOptionalDecimal(HolidaySupplementRate));

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
        ContractValidFrom = employee.ContractValidFrom == default
            ? null
            : new DateTimeOffset(employee.ContractValidFrom.ToDateTime(TimeOnly.MinValue));
        ContractValidTo = employee.ContractValidTo.HasValue
            ? new DateTimeOffset(employee.ContractValidTo.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        HourlyRateChf = employee.HourlyRateChf.ToString("0.00");
        MonthlyBvgDeductionChf = employee.MonthlyBvgDeductionChf.ToString("0.00");
        NightSupplementRate = employee.NightSupplementRate?.ToString("0.####");
        SundaySupplementRate = employee.SundaySupplementRate?.ToString("0.####");
        HolidaySupplementRate = employee.HolidaySupplementRate?.ToString("0.####");
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
        ContractValidFrom = new DateTimeOffset(DateTime.Today);
        ContractValidTo = null;
        HourlyRateChf = "0";
        MonthlyBvgDeductionChf = "0";
        NightSupplementRate = null;
        SundaySupplementRate = null;
        HolidaySupplementRate = null;
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
        ContractValidFrom = null;
        ContractValidTo = null;
        HourlyRateChf = string.Empty;
        MonthlyBvgDeductionChf = string.Empty;
        NightSupplementRate = null;
        SundaySupplementRate = null;
        HolidaySupplementRate = null;
        SetInteractionState(isEditing: false, isCreatingNew: false);
    }

    private void SetInteractionState(bool isEditing, bool isCreatingNew)
    {
        _isEditing = isEditing;
        _isCreatingNew = isCreatingNew;
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

    private static decimal ParseRequiredDecimal(string value, string fieldName)
    {
        if (!decimal.TryParse(value, out var parsedValue))
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

        if (!decimal.TryParse(value, out var parsedValue))
        {
            throw new InvalidOperationException("Zuschlagswerte muessen gueltige Zahlen sein.");
        }

        return parsedValue;
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
}
