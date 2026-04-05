using System.Collections.ObjectModel;
using System.Globalization;
using Payroll.Application.MonthlyRecords;

namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyRecordViewModel : ViewModelBase
{
    private readonly MonthlyRecordService _monthlyRecordService;
    private Guid? _currentEmployeeId;
    private Guid? _currentMonthlyRecordId;
    private bool _isBusy;
    private bool _isLocked;
    private DateTimeOffset? _selectedMonth = new DateTimeOffset(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, TimeSpan.Zero);
    private string _contextTitle = "Keine Monatserfassung geladen.";
    private string _contextDescription = "Zuerst eine Person auswaehlen, dann den Monat laden.";
    private string _statusSummary = "Kein Monatskontext aktiv.";
    private string _contractSummary = "Kein Vertragsstand geladen.";
    private string _totalsSummary = "Noch keine Monatssummen vorhanden.";
    private string _actionMessage = "Noch keine Aktion ausgefuehrt.";
    private string _timeDate = DateTime.Today.ToString("yyyy-MM-dd");
    private string _hoursWorked = "0";
    private string _nightHours = "0";
    private string _sundayHours = "0";
    private string _holidayHours = "0";
    private string? _timeNote;
    private MonthlyTimeEntryItemViewModel? _selectedTimeEntry;
    private string _expenseDate = DateTime.Today.ToString("yyyy-MM-dd");
    private string _expenseAmount = "0";
    private MonthlyExpenseEntryItemViewModel? _selectedExpenseEntry;
    private string _vehicleCompensationDate = DateTime.Today.ToString("yyyy-MM-dd");
    private string _vehicleCompensationAmount = "0";
    private string _vehicleCompensationDescription = string.Empty;
    private MonthlyVehicleCompensationItemViewModel? _selectedVehicleCompensation;
    private string _previewSummary = "Monatsvorschau wird nach dem Laden des Monats angezeigt.";
    private string _previewTotals = "Noch keine verdichteten Monatswerte vorhanden.";
    private string _previewEntryCounts = "Noch keine Eintraege im aktuellen Monat vorhanden.";

    public MonthlyRecordViewModel(MonthlyRecordService monthlyRecordService)
    {
        _monthlyRecordService = monthlyRecordService;
        TimeEntries = [];
        ExpenseEntries = [];
        VehicleCompensations = [];
        PreviewRows = [];
        PreviewNotes = [];
        LoadMonthlyRecordCommand = new DelegateCommand(LoadAsync, () => CanManageRecord);
        NewTimeEntryCommand = new DelegateCommand(PrepareNewTimeEntry, () => CanManageRecord);
        SaveTimeEntryCommand = new DelegateCommand(SaveTimeEntryAsync, () => CanSaveTimeEntry);
        DeleteTimeEntryCommand = new DelegateCommand(DeleteTimeEntryAsync, () => CanDeleteTimeEntry);
        NewExpenseEntryCommand = new DelegateCommand(PrepareNewExpenseEntry, () => CanManageRecord);
        SaveExpenseEntryCommand = new DelegateCommand(SaveExpenseEntryAsync, () => CanSaveExpenseEntry);
        DeleteExpenseEntryCommand = new DelegateCommand(DeleteExpenseEntryAsync, () => CanDeleteExpenseEntry);
        NewVehicleCompensationCommand = new DelegateCommand(PrepareNewVehicleCompensation, () => CanManageRecord);
        SaveVehicleCompensationCommand = new DelegateCommand(SaveVehicleCompensationAsync, () => CanSaveVehicleCompensation);
        DeleteVehicleCompensationCommand = new DelegateCommand(DeleteVehicleCompensationAsync, () => CanDeleteVehicleCompensation);
    }

    public ObservableCollection<MonthlyTimeEntryItemViewModel> TimeEntries { get; }
    public ObservableCollection<MonthlyExpenseEntryItemViewModel> ExpenseEntries { get; }
    public ObservableCollection<MonthlyVehicleCompensationItemViewModel> VehicleCompensations { get; }
    public ObservableCollection<MonthlyPreviewRowViewModel> PreviewRows { get; }
    public ObservableCollection<string> PreviewNotes { get; }
    public DelegateCommand LoadMonthlyRecordCommand { get; }
    public DelegateCommand NewTimeEntryCommand { get; }
    public DelegateCommand SaveTimeEntryCommand { get; }
    public DelegateCommand DeleteTimeEntryCommand { get; }
    public DelegateCommand NewExpenseEntryCommand { get; }
    public DelegateCommand SaveExpenseEntryCommand { get; }
    public DelegateCommand DeleteExpenseEntryCommand { get; }
    public DelegateCommand NewVehicleCompensationCommand { get; }
    public DelegateCommand SaveVehicleCompensationCommand { get; }
    public DelegateCommand DeleteVehicleCompensationCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(CanManageRecord));
                RaisePropertyChanged(nameof(CanSaveTimeEntry));
                RaisePropertyChanged(nameof(CanDeleteTimeEntry));
                RaisePropertyChanged(nameof(CanSaveExpenseEntry));
                RaisePropertyChanged(nameof(CanDeleteExpenseEntry));
                RaisePropertyChanged(nameof(CanSaveVehicleCompensation));
                RaisePropertyChanged(nameof(CanDeleteVehicleCompensation));
                RaiseActionStateChanged();
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetProperty(ref _isLocked, value))
            {
                RaisePropertyChanged(nameof(CanManageRecord));
                RaisePropertyChanged(nameof(CanSaveTimeEntry));
                RaisePropertyChanged(nameof(CanDeleteTimeEntry));
                RaisePropertyChanged(nameof(CanSaveExpenseEntry));
                RaisePropertyChanged(nameof(CanDeleteExpenseEntry));
                RaisePropertyChanged(nameof(CanSaveVehicleCompensation));
                RaisePropertyChanged(nameof(CanDeleteVehicleCompensation));
                RaiseActionStateChanged();
            }
        }
    }

    public bool CanManageRecord => !IsBusy && !IsLocked && _currentEmployeeId.HasValue;
    public bool CanSaveTimeEntry => CanManageRecord && _currentMonthlyRecordId.HasValue;
    public bool CanDeleteTimeEntry => CanManageRecord && _currentMonthlyRecordId.HasValue && SelectedTimeEntry is not null;
    public bool CanSaveExpenseEntry => CanManageRecord && _currentMonthlyRecordId.HasValue;
    public bool CanDeleteExpenseEntry => CanManageRecord && _currentMonthlyRecordId.HasValue && SelectedExpenseEntry is not null;
    public bool CanSaveVehicleCompensation => CanManageRecord && _currentMonthlyRecordId.HasValue;
    public bool CanDeleteVehicleCompensation => CanManageRecord && _currentMonthlyRecordId.HasValue && SelectedVehicleCompensation is not null;

    public DateTimeOffset? SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            var normalizedMonth = new DateTimeOffset(value.Value.Year, value.Value.Month, 1, 0, 0, 0, value.Value.Offset);
            if (SetProperty(ref _selectedMonth, normalizedMonth))
            {
                RaisePropertyChanged(nameof(ExpensePayrollMonth));
                PrepareNewTimeEntry();
                PrepareNewExpenseEntry();
                PrepareNewVehicleCompensation();
                ResetLoadedRecordState();

                if (_currentEmployeeId.HasValue)
                {
                    _ = LoadAsync();
                }
            }
        }
    }

    public string ExpensePayrollMonth => SelectedMonth.HasValue
        ? $"{SelectedMonth.Value:MM/yyyy}"
        : "-";

    public string ContextTitle
    {
        get => _contextTitle;
        private set => SetProperty(ref _contextTitle, value);
    }

    public string ContextDescription
    {
        get => _contextDescription;
        private set => SetProperty(ref _contextDescription, value);
    }

    public string StatusSummary
    {
        get => _statusSummary;
        private set => SetProperty(ref _statusSummary, value);
    }

    public string ContractSummary
    {
        get => _contractSummary;
        private set => SetProperty(ref _contractSummary, value);
    }

    public string TotalsSummary
    {
        get => _totalsSummary;
        private set => SetProperty(ref _totalsSummary, value);
    }

    public string ActionMessage
    {
        get => _actionMessage;
        private set => SetProperty(ref _actionMessage, value);
    }

    public string PreviewSummary
    {
        get => _previewSummary;
        private set => SetProperty(ref _previewSummary, value);
    }

    public string PreviewTotals
    {
        get => _previewTotals;
        private set => SetProperty(ref _previewTotals, value);
    }

    public string PreviewEntryCounts
    {
        get => _previewEntryCounts;
        private set => SetProperty(ref _previewEntryCounts, value);
    }

    public string TimeDate
    {
        get => _timeDate;
        set => SetProperty(ref _timeDate, value);
    }

    public string HoursWorked
    {
        get => _hoursWorked;
        set => SetProperty(ref _hoursWorked, value);
    }

    public string NightHours
    {
        get => _nightHours;
        set => SetProperty(ref _nightHours, value);
    }

    public string SundayHours
    {
        get => _sundayHours;
        set => SetProperty(ref _sundayHours, value);
    }

    public string HolidayHours
    {
        get => _holidayHours;
        set => SetProperty(ref _holidayHours, value);
    }

    public string? TimeNote
    {
        get => _timeNote;
        set => SetProperty(ref _timeNote, value);
    }

    public MonthlyTimeEntryItemViewModel? SelectedTimeEntry
    {
        get => _selectedTimeEntry;
        set
        {
            if (SetProperty(ref _selectedTimeEntry, value))
            {
                RaisePropertyChanged(nameof(CanDeleteTimeEntry));
                DeleteTimeEntryCommand.RaiseCanExecuteChanged();

                if (value is not null)
                {
                    PopulateTimeEntryForm(value);
                }
            }
        }
    }

    public string ExpenseDate
    {
        get => _expenseDate;
        set => SetProperty(ref _expenseDate, value);
    }

    public string ExpenseAmount
    {
        get => _expenseAmount;
        set => SetProperty(ref _expenseAmount, value);
    }

    public MonthlyExpenseEntryItemViewModel? SelectedExpenseEntry
    {
        get => _selectedExpenseEntry;
        set
        {
            if (SetProperty(ref _selectedExpenseEntry, value))
            {
                RaisePropertyChanged(nameof(CanDeleteExpenseEntry));
                DeleteExpenseEntryCommand.RaiseCanExecuteChanged();

                if (value is not null)
                {
                    PopulateExpenseEntryForm(value);
                }
            }
        }
    }

    public string VehicleCompensationDate
    {
        get => _vehicleCompensationDate;
        set => SetProperty(ref _vehicleCompensationDate, value);
    }

    public string VehicleCompensationAmount
    {
        get => _vehicleCompensationAmount;
        set => SetProperty(ref _vehicleCompensationAmount, value);
    }

    public string VehicleCompensationDescription
    {
        get => _vehicleCompensationDescription;
        set => SetProperty(ref _vehicleCompensationDescription, value);
    }

    public MonthlyVehicleCompensationItemViewModel? SelectedVehicleCompensation
    {
        get => _selectedVehicleCompensation;
        set
        {
            if (SetProperty(ref _selectedVehicleCompensation, value))
            {
                RaisePropertyChanged(nameof(CanDeleteVehicleCompensation));
                DeleteVehicleCompensationCommand.RaiseCanExecuteChanged();

                if (value is not null)
                {
                    PopulateVehicleCompensationForm(value);
                }
            }
        }
    }

    public async Task SetEmployeeAsync(Guid? employeeId, string? employeeName)
    {
        _currentEmployeeId = employeeId;

        if (!employeeId.HasValue)
        {
            Reset();
            return;
        }

        ContextTitle = $"Monatserfassung fuer {employeeName ?? "ausgewaehlte Person"}";
        ContextDescription = SelectedMonth.HasValue
            ? $"{SelectedMonth.Value:MM/yyyy} | Monatserfassung fuer Zeiten und Spesen."
            : "Monatserfassung fuer Zeiten und Spesen.";
        ResetLoadedRecordState();
        await LoadAsync();
    }

    public void Reset()
    {
        _currentEmployeeId = null;
        _currentMonthlyRecordId = null;
        ContextTitle = "Keine Monatserfassung geladen.";
        ContextDescription = "Zuerst Monat und Mitarbeitenden waehlen.";
        StatusSummary = "Kein Monatskontext aktiv.";
        ContractSummary = "Kein Vertragsstand geladen.";
        TotalsSummary = "Noch keine Monatssummen vorhanden.";
        ActionMessage = "Noch keine Aktion ausgefuehrt.";
        PreviewSummary = "Monatsvorschau wird nach dem Laden des Monats angezeigt.";
        PreviewTotals = "Noch keine verdichteten Monatswerte vorhanden.";
        PreviewEntryCounts = "Noch keine Eintraege im aktuellen Monat vorhanden.";
        TimeEntries.Clear();
        ExpenseEntries.Clear();
        VehicleCompensations.Clear();
        PreviewRows.Clear();
        PreviewNotes.Clear();
        PrepareNewTimeEntry();
        PrepareNewExpenseEntry();
        PrepareNewVehicleCompensation();
    }

    private async Task LoadAsync()
    {
        if (!_currentEmployeeId.HasValue || !SelectedMonth.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await LoadCurrentRecordAsync();
        });
    }

    private void PrepareNewTimeEntry()
    {
        SelectedTimeEntry = null;
        TimeDate = SelectedMonth.HasValue
            ? new DateOnly(SelectedMonth.Value.Year, SelectedMonth.Value.Month, 1).ToString("yyyy-MM-dd")
            : DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        HoursWorked = "0";
        NightHours = "0";
        SundayHours = "0";
        HolidayHours = "0";
        TimeNote = null;
    }

    private async Task SaveTimeEntryAsync()
    {
        await EnsureCurrentRecordLoadedAsync();

        if (!_currentMonthlyRecordId.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var details = await _monthlyRecordService.SaveTimeEntryAsync(
                new SaveMonthlyTimeEntryCommand(
                    _currentMonthlyRecordId.Value,
                    SelectedTimeEntry?.TimeEntryId,
                    ParseRequiredDate(TimeDate, nameof(TimeDate)),
                    ParseRequiredDecimal(HoursWorked, nameof(HoursWorked)),
                    ParseRequiredDecimal(NightHours, nameof(NightHours)),
                    ParseRequiredDecimal(SundayHours, nameof(SundayHours)),
                    ParseRequiredDecimal(HolidayHours, nameof(HolidayHours)),
                    TimeNote));

            ApplyDetails(details);
            ActionMessage = SelectedTimeEntry is null
                ? "Zeiteintrag gespeichert."
                : "Zeiteintrag aktualisiert.";
            PrepareNewTimeEntry();
        });
    }

    private async Task DeleteTimeEntryAsync()
    {
        if (!_currentMonthlyRecordId.HasValue || SelectedTimeEntry is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await _monthlyRecordService.DeleteTimeEntryAsync(_currentMonthlyRecordId.Value, SelectedTimeEntry.TimeEntryId);
            var details = await _monthlyRecordService.GetOrCreateAsync(
                new MonthlyRecordQuery(_currentEmployeeId!.Value, SelectedMonth!.Value.Year, SelectedMonth.Value.Month));

            ApplyDetails(details);
            ActionMessage = "Zeiteintrag geloescht.";
            PrepareNewTimeEntry();
        });
    }

    private void PrepareNewExpenseEntry()
    {
        SelectedExpenseEntry = null;
        ExpenseDate = SelectedMonth.HasValue
            ? new DateOnly(SelectedMonth.Value.Year, SelectedMonth.Value.Month, 1).ToString("yyyy-MM-dd")
            : DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        ExpenseAmount = "0";
    }

    private async Task SaveExpenseEntryAsync()
    {
        await EnsureCurrentRecordLoadedAsync();

        if (!_currentMonthlyRecordId.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var details = await _monthlyRecordService.SaveExpenseEntryAsync(
                new SaveMonthlyExpenseEntryCommand(
                    _currentMonthlyRecordId.Value,
                    SelectedExpenseEntry?.ExpenseEntryId,
                    ParseRequiredDate(ExpenseDate, nameof(ExpenseDate)),
                    ParseRequiredDecimal(ExpenseAmount, nameof(ExpenseAmount))));

            ApplyDetails(details);
            ActionMessage = SelectedExpenseEntry is null
                ? "Spese gespeichert."
                : "Spese aktualisiert.";
            PrepareNewExpenseEntry();
        });
    }

    private async Task DeleteExpenseEntryAsync()
    {
        if (!_currentMonthlyRecordId.HasValue || SelectedExpenseEntry is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await _monthlyRecordService.DeleteExpenseEntryAsync(_currentMonthlyRecordId.Value, SelectedExpenseEntry.ExpenseEntryId);
            var details = await _monthlyRecordService.GetOrCreateAsync(
                new MonthlyRecordQuery(_currentEmployeeId!.Value, SelectedMonth!.Value.Year, SelectedMonth.Value.Month));

            ApplyDetails(details);
            ActionMessage = "Spese geloescht.";
            PrepareNewExpenseEntry();
        });
    }

    private void PrepareNewVehicleCompensation()
    {
        SelectedVehicleCompensation = null;
        VehicleCompensationDate = SelectedMonth.HasValue
            ? new DateOnly(SelectedMonth.Value.Year, SelectedMonth.Value.Month, 1).ToString("yyyy-MM-dd")
            : DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        VehicleCompensationAmount = "0";
        VehicleCompensationDescription = string.Empty;
    }

    private async Task SaveVehicleCompensationAsync()
    {
        await EnsureCurrentRecordLoadedAsync();

        if (!_currentMonthlyRecordId.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var details = await _monthlyRecordService.SaveVehicleCompensationAsync(
                new SaveMonthlyVehicleCompensationCommand(
                    _currentMonthlyRecordId.Value,
                    SelectedVehicleCompensation?.VehicleCompensationId,
                    ParseRequiredDate(VehicleCompensationDate, nameof(VehicleCompensationDate)),
                    ParseRequiredDecimal(VehicleCompensationAmount, nameof(VehicleCompensationAmount)),
                    VehicleCompensationDescription));

            ApplyDetails(details);
            ActionMessage = SelectedVehicleCompensation is null
                ? "Fahrzeugentschaedigung gespeichert."
                : "Fahrzeugentschaedigung aktualisiert.";
            PrepareNewVehicleCompensation();
        });
    }

    private async Task DeleteVehicleCompensationAsync()
    {
        if (!_currentMonthlyRecordId.HasValue || SelectedVehicleCompensation is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await _monthlyRecordService.DeleteVehicleCompensationAsync(_currentMonthlyRecordId.Value, SelectedVehicleCompensation.VehicleCompensationId);
            var details = await _monthlyRecordService.GetOrCreateAsync(
                new MonthlyRecordQuery(_currentEmployeeId!.Value, SelectedMonth!.Value.Year, SelectedMonth.Value.Month));

            ApplyDetails(details);
            ActionMessage = "Fahrzeugentschaedigung geloescht.";
            PrepareNewVehicleCompensation();
        });
    }

    private void ApplyDetails(MonthlyRecordDetailsDto details)
    {
        _currentMonthlyRecordId = details.Header.MonthlyRecordId;
        ContextTitle = $"Monatserfassung fuer {details.Header.EmployeeFullName}";
        ContextDescription = $"{details.Header.Month:00}/{details.Header.Year} | Monatserfassung fuer Zeiten und Spesen.";
        StatusSummary = $"Status: {FormatStatus(details.Header.Status)}";
        ContractSummary = details.Header.ContractValidFrom.HasValue
            ? $"Vertragsstand: {details.Header.ContractValidFrom:dd.MM.yyyy} bis {FormatDate(details.Header.ContractValidTo)} | {details.Header.HourlyRateChf:0.00} CHF/h | BVG {details.Header.MonthlyBvgDeductionChf:0.00} CHF"
            : "Vertragsstand: kein passender Vertrag fuer den Monat gefunden.";
        TotalsSummary = $"Stunden {details.Header.TotalWorkedHours:0.##} | Spezialstunden {details.Header.TotalSpecialHours:0.##} | Spesen {details.Header.TotalExpensesChf:0.00} CHF | Fahrzeug {details.Header.TotalVehicleCompensationChf:0.00} CHF";
        PreviewSummary = "Monatsvorschau zeigt alle vorhandenen Monate der selektierten Person tabellarisch untereinander.";
        PreviewTotals = $"Arbeitsstunden {details.Header.TotalWorkedHours:0.##} | Spezialstunden {details.Header.TotalSpecialHours:0.##} | Spesen {details.Header.TotalExpensesChf:0.00} CHF | Fahrzeug {details.Header.TotalVehicleCompensationChf:0.00} CHF";
        PreviewEntryCounts = $"Eintraege im aktuellen Monat: Zeiten {details.TimeEntries.Count} | Spesen {details.ExpenseEntries.Count} | Fahrzeugentschaedigungen {details.VehicleCompensations.Count}";

        TimeEntries.Clear();
        foreach (var entry in details.TimeEntries)
        {
            TimeEntries.Add(new MonthlyTimeEntryItemViewModel
            {
                TimeEntryId = entry.TimeEntryId,
                WorkDate = entry.WorkDate,
                HoursWorked = entry.HoursWorked,
                NightHours = entry.NightHours,
                SundayHours = entry.SundayHours,
                HolidayHours = entry.HolidayHours,
                Note = entry.Note,
                Summary = $"{entry.WorkDate:dd.MM.yyyy} | Arbeit {entry.HoursWorked:0.##} h | Nacht {entry.NightHours:0.##} | Sonntag {entry.SundayHours:0.##} | Feiertag {entry.HolidayHours:0.##}"
            });
        }

        ExpenseEntries.Clear();
        foreach (var entry in details.ExpenseEntries)
        {
            ExpenseEntries.Add(new MonthlyExpenseEntryItemViewModel
            {
                ExpenseEntryId = entry.ExpenseEntryId,
                ExpenseDate = entry.ExpenseDate,
                AmountChf = entry.AmountChf,
                Summary = $"{entry.ExpenseDate:dd.MM.yyyy} | {entry.AmountChf:0.00} CHF"
            });
        }

        VehicleCompensations.Clear();
        foreach (var entry in details.VehicleCompensations)
        {
            VehicleCompensations.Add(new MonthlyVehicleCompensationItemViewModel
            {
                VehicleCompensationId = entry.VehicleCompensationId,
                CompensationDate = entry.CompensationDate,
                AmountChf = entry.AmountChf,
                Description = entry.Description,
                Summary = $"{entry.CompensationDate:dd.MM.yyyy} | {entry.AmountChf:0.00} CHF | {entry.Description}"
            });
        }

        PreviewRows.Clear();
        foreach (var row in BuildPreviewRows(details.Preview.Rows))
        {
            PreviewRows.Add(row);
        }

        PreviewNotes.Clear();
        foreach (var note in details.Preview.Notes)
        {
            PreviewNotes.Add(note);
        }

        RaisePropertyChanged(nameof(CanSaveTimeEntry));
        RaisePropertyChanged(nameof(CanDeleteTimeEntry));
        RaisePropertyChanged(nameof(CanSaveExpenseEntry));
        RaisePropertyChanged(nameof(CanDeleteExpenseEntry));
        RaisePropertyChanged(nameof(CanSaveVehicleCompensation));
        RaisePropertyChanged(nameof(CanDeleteVehicleCompensation));
        RaiseActionStateChanged();
    }

    private void PopulateTimeEntryForm(MonthlyTimeEntryItemViewModel entry)
    {
        TimeDate = entry.WorkDate.ToString("yyyy-MM-dd");
        HoursWorked = entry.HoursWorked.ToString("0.##");
        NightHours = entry.NightHours.ToString("0.##");
        SundayHours = entry.SundayHours.ToString("0.##");
        HolidayHours = entry.HolidayHours.ToString("0.##");
        TimeNote = entry.Note;
    }

    private void PopulateExpenseEntryForm(MonthlyExpenseEntryItemViewModel entry)
    {
        ExpenseDate = entry.ExpenseDate.ToString("yyyy-MM-dd");
        ExpenseAmount = entry.AmountChf.ToString("0.00");
    }

    private void PopulateVehicleCompensationForm(MonthlyVehicleCompensationItemViewModel entry)
    {
        VehicleCompensationDate = entry.CompensationDate.ToString("yyyy-MM-dd");
        VehicleCompensationAmount = entry.AmountChf.ToString("0.00");
        VehicleCompensationDescription = entry.Description;
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
            var message = FormatExceptionMessage(exception);
            ActionMessage = $"Fehler: {message}";
            PreviewSummary = message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCurrentRecordAsync()
    {
        var details = await _monthlyRecordService.GetOrCreateAsync(
            new MonthlyRecordQuery(_currentEmployeeId!.Value, SelectedMonth!.Value.Year, SelectedMonth.Value.Month));

        ApplyDetails(details);
    }

    private async Task EnsureCurrentRecordLoadedAsync()
    {
        if (_currentMonthlyRecordId.HasValue || !_currentEmployeeId.HasValue || !SelectedMonth.HasValue)
        {
            return;
        }

        await LoadCurrentRecordAsync();
    }

    private void ResetLoadedRecordState()
    {
        _currentMonthlyRecordId = null;
        StatusSummary = "Kein Monatskontext aktiv.";
        ContractSummary = "Kein Vertragsstand geladen.";
        TotalsSummary = "Noch keine Monatssummen vorhanden.";
        ActionMessage = "Noch keine Aktion ausgefuehrt.";
        PreviewSummary = "Monatsvorschau wird nach dem Laden des Monats angezeigt.";
        PreviewTotals = "Noch keine verdichteten Monatswerte vorhanden.";
        PreviewEntryCounts = "Noch keine Eintraege im aktuellen Monat vorhanden.";
        TimeEntries.Clear();
        ExpenseEntries.Clear();
        VehicleCompensations.Clear();
        PreviewRows.Clear();
        PreviewNotes.Clear();
        RaisePropertyChanged(nameof(CanSaveTimeEntry));
        RaisePropertyChanged(nameof(CanDeleteTimeEntry));
        RaisePropertyChanged(nameof(CanSaveExpenseEntry));
        RaisePropertyChanged(nameof(CanDeleteExpenseEntry));
        RaisePropertyChanged(nameof(CanSaveVehicleCompensation));
        RaisePropertyChanged(nameof(CanDeleteVehicleCompensation));
        RaiseActionStateChanged();
    }

    private void RaiseActionStateChanged()
    {
        LoadMonthlyRecordCommand.RaiseCanExecuteChanged();
        NewTimeEntryCommand.RaiseCanExecuteChanged();
        SaveTimeEntryCommand.RaiseCanExecuteChanged();
        DeleteTimeEntryCommand.RaiseCanExecuteChanged();
        NewExpenseEntryCommand.RaiseCanExecuteChanged();
        SaveExpenseEntryCommand.RaiseCanExecuteChanged();
        DeleteExpenseEntryCommand.RaiseCanExecuteChanged();
        NewVehicleCompensationCommand.RaiseCanExecuteChanged();
        SaveVehicleCompensationCommand.RaiseCanExecuteChanged();
        DeleteVehicleCompensationCommand.RaiseCanExecuteChanged();
    }

    private static DateOnly ParseRequiredDate(string value, string fieldName)
    {
        var supportedFormats = new[]
        {
            "yyyy-MM-dd",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy"
        };

        if (!DateOnly.TryParseExact(value, supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            && !DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
        {
            throw new InvalidOperationException($"{fieldName} muss ein gueltiges Datum sein.");
        }

        return parsedDate;
    }

    private static decimal ParseRequiredDecimal(string value, string fieldName)
    {
        if (!TryParseDecimal(value, out var parsedValue))
        {
            throw new InvalidOperationException($"{fieldName} muss eine gueltige Zahl sein.");
        }

        return parsedValue;
    }

    private static string FormatExceptionMessage(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message)
                && !messages.Contains(current.Message, StringComparer.Ordinal))
            {
                messages.Add(current.Message);
            }

            current = current.InnerException!;
        }

        return string.Join(" | ", messages);
    }

    private static bool TryParseDecimal(string value, out decimal parsedValue)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedValue))
        {
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedValue))
        {
            return true;
        }

        var normalized = value.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedValue);
    }

    private static IReadOnlyCollection<MonthlyPreviewRowViewModel> BuildPreviewRows(IReadOnlyCollection<MonthlyPreviewRowDto> rows)
    {
        return rows
            .Select(entry => new MonthlyPreviewRowViewModel
            {
                MonthLabel = $"{entry.Month:00}/{entry.Year}",
                EntryDate = entry.EntryDate ?? new DateOnly(entry.Year, entry.Month, 1),
                EntryDateLabel = entry.EntryDate.HasValue ? entry.EntryDate.Value.ToString("dd.MM.yyyy") : "-",
                EntryType = entry.EntryType,
                QuantityOrAmount = entry.QuantityOrAmount,
                Details = entry.Details
            })
            .ToArray();
    }

    private static string FormatStatus(Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecordStatus status)
    {
        return status switch
        {
            Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecordStatus.Draft => "Entwurf",
            Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecordStatus.Reviewed => "Geprueft",
            Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecordStatus.ImportedToPayroll => "In Lohnlauf uebernommen",
            _ => status.ToString()
        };
    }

    private static string FormatDate(DateOnly? value)
    {
        return value.HasValue ? value.Value.ToString("dd.MM.yyyy") : "offen";
    }
}
