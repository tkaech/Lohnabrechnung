using System.Collections.ObjectModel;
using System.Globalization;
using Payroll.Application.Formatting;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Desktop.Formatting;

namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyRecordViewModel : ViewModelBase
{
    private readonly MonthlyRecordService _monthlyRecordService;
    private Guid? _currentEmployeeId;
    private Guid? _currentMonthlyRecordId;
    private bool _isBusy;
    private bool _isLocked;
    private bool _loadCurrentRecordAfterBusy;
    private DateTimeOffset? _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, TimeSpan.Zero);
    private string _selectedMonthText = DateTime.Today.ToString("MM/yyyy");
    private string _contextTitle = "Keine Monatserfassung geladen.";
    private string _contextDescription = "Zuerst eine Person auswaehlen, dann den Monat laden.";
    private string _statusSummary = "Kein Monatskontext aktiv.";
    private string _contractSummary = "Kein Vertragsstand geladen.";
    private string _totalsSummary = "Noch keine Monatssummen vorhanden.";
    private string _actionMessage = "Noch keine Aktion ausgefuehrt.";
    private string _timeDate = DateTime.Today.ToString("dd.MM.yyyy");
    private string _hoursWorked = "0";
    private string _nightHours = "0";
    private string _sundayHours = "0";
    private string _holidayHours = "0";
    private string _vehiclePauschalzone1 = "0";
    private string _vehiclePauschalzone2 = "0";
    private string _vehicleRegiezone1 = "0";
    private string? _timeNote;
    private MonthlyTimeEntryItemViewModel? _selectedTimeEntry;
    private Guid? _pendingTimeEntrySelectionId;
    private string _expensesTotal = "0";
    private MonthlyExpenseEntryItemViewModel? _selectedExpenseEntry;
    private Guid? _pendingExpenseEntrySelectionId;
    private string _previewSummary = "Monatsvorschau wird nach dem Laden des Monats angezeigt.";
    private string _previewTotals = "Noch keine verdichteten Monatswerte vorhanden.";
    private string _previewEntryCounts = "Noch keine Eintraege im aktuellen Monat vorhanden.";
    private string _payrollPreviewTitle = "Lohn-Voransicht";
    private string _payrollPreviewSummary = "Lohn-Voransicht wird nach dem Laden des Monats angezeigt.";
    private bool _isPayrollPreviewDerivationVisible;
    private bool _isTimeDatePickerOpen;
    private bool _isSubjectToWithholdingTax;
    private string _withholdingTaxRatePercent = "0";
    private string _withholdingTaxCorrectionAmountChf = "0";
    private string? _withholdingTaxCorrectionText;
    private IReadOnlyCollection<MonthlyPayrollPreviewLineDto> _rawPayrollPreviewLines = [];
    private IReadOnlyDictionary<string, PayrollPreviewHelpOptionDto> _payrollPreviewHelpOptions = new Dictionary<string, PayrollPreviewHelpOptionDto>(StringComparer.Ordinal);
    public event EventHandler? TimeCaptureChanged;

    public MonthlyRecordViewModel(MonthlyRecordService monthlyRecordService)
    {
        _monthlyRecordService = monthlyRecordService;
        TimeEntries = [];
        TimeEntryHistory = [];
        TimeEntryColumns =
        [
            new(TimeEntryColumnViewModel.WorkDateKey, "Datum", 100d),
            new(TimeEntryColumnViewModel.MonthKey, "Monat", 90d),
            new(TimeEntryColumnViewModel.HoursWorkedKey, "Arbeit", 72d),
            new(TimeEntryColumnViewModel.NightHoursKey, "Nacht", 72d),
            new(TimeEntryColumnViewModel.SundayHoursKey, "Sonntag", 72d),
            new(TimeEntryColumnViewModel.HolidayHoursKey, "Feiertag", 72d),
            new(TimeEntryColumnViewModel.VehiclePauschalzone1Key, "P1", 78d),
            new(TimeEntryColumnViewModel.VehiclePauschalzone2Key, "P2", 78d),
            new(TimeEntryColumnViewModel.VehicleRegiezone1Key, "R1", 78d)
        ];
        ExpenseEntryHistory = [];
        PreviewRows = [];
        PreviewNotes = [];
        PayrollPreviewLines = [];
        PayrollPreviewDerivationGroups = [];
        PayrollPreviewNotes = [];
        LoadMonthlyRecordCommand = new DelegateCommand(LoadAsync, () => CanManageRecord);
        NewTimeEntryCommand = new DelegateCommand(PrepareNewTimeEntry, () => CanManageRecord);
        SaveTimeEntryCommand = new DelegateCommand(SaveTimeEntryAsync, () => CanSaveTimeEntry);
        DeleteTimeEntryCommand = new DelegateCommand(DeleteTimeEntryAsync, () => CanDeleteTimeEntry);
        ResetExpenseValuesCommand = new DelegateCommand(PrepareNewExpenseEntry, () => CanManageRecord);
        SaveExpenseEntryCommand = new DelegateCommand(SaveExpenseEntryAsync, () => CanSaveExpenseEntry);
        SaveWithholdingTaxCommand = new DelegateCommand(SaveWithholdingTaxAsync, () => CanSaveWithholdingTax);
        ResetWithholdingTaxCommand = new DelegateCommand(ResetWithholdingTaxAsync, () => CanResetWithholdingTax);
    }

    public ObservableCollection<MonthlyTimeEntryItemViewModel> TimeEntries { get; }
    public ObservableCollection<MonthlyTimeEntryItemViewModel> TimeEntryHistory { get; }
    public ObservableCollection<TimeEntryColumnViewModel> TimeEntryColumns { get; }
    public ObservableCollection<MonthlyExpenseEntryItemViewModel> ExpenseEntryHistory { get; }
    public ObservableCollection<MonthlyPreviewRowViewModel> PreviewRows { get; }
    public ObservableCollection<string> PreviewNotes { get; }
    public ObservableCollection<MonthlyPayrollPreviewLineDto> PayrollPreviewLines { get; }
    public ObservableCollection<MonthlyPayrollPreviewDerivationGroupDto> PayrollPreviewDerivationGroups { get; }
    public ObservableCollection<string> PayrollPreviewNotes { get; }
    public DelegateCommand LoadMonthlyRecordCommand { get; }
    public DelegateCommand NewTimeEntryCommand { get; }
    public DelegateCommand SaveTimeEntryCommand { get; }
    public DelegateCommand DeleteTimeEntryCommand { get; }
    public DelegateCommand ResetExpenseValuesCommand { get; }
    public DelegateCommand SaveExpenseEntryCommand { get; }
    public DelegateCommand SaveWithholdingTaxCommand { get; }
    public DelegateCommand ResetWithholdingTaxCommand { get; }

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
                RaisePropertyChanged(nameof(CanSaveWithholdingTax));
                RaisePropertyChanged(nameof(CanResetWithholdingTax));
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
                RaisePropertyChanged(nameof(CanSaveWithholdingTax));
                RaisePropertyChanged(nameof(CanResetWithholdingTax));
                RaiseActionStateChanged();
            }
        }
    }

    public bool CanManageRecord => !IsBusy && !IsLocked && _currentEmployeeId.HasValue;
    public bool CanSaveTimeEntry => CanManageRecord && _currentMonthlyRecordId.HasValue;
    public bool CanDeleteTimeEntry => CanManageRecord && _currentMonthlyRecordId.HasValue && SelectedTimeEntry is { IsCurrentMonth: true };
    public bool CanSaveExpenseEntry => CanManageRecord && _currentMonthlyRecordId.HasValue;
    public bool CanSaveWithholdingTax => CanManageRecord && _currentMonthlyRecordId.HasValue && IsSubjectToWithholdingTax;
    public bool CanResetWithholdingTax => CanSaveWithholdingTax;

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
            _ = ApplySelectedMonthAsync(normalizedMonth, forceReload: false);
        }
    }

    public string SelectedMonthText
    {
        get => _selectedMonthText;
        set
        {
            if (!SetProperty(ref _selectedMonthText, value))
            {
                return;
            }

            if (TryParseMonth(value, out var normalizedMonth))
            {
                _ = ApplySelectedMonthAsync(normalizedMonth, forceReload: false);
            }
        }
    }

    public DateTimeOffset? SelectedMonthPickerDate
    {
        get => SelectedMonth;
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            SelectedMonth = value;
        }
    }

    public string ExpensePayrollMonth => SelectedMonth.HasValue
        ? $"{SelectedMonth.Value:MM/yyyy}"
        : "-";

    public string TimePayrollMonth => ExpensePayrollMonth;

    public bool IsTimeDatePickerOpen
    {
        get => _isTimeDatePickerOpen;
        set => SetProperty(ref _isTimeDatePickerOpen, value);
    }

    public DateTimeOffset? TimeDatePicker
    {
        get
        {
            if (!TryParseDate(TimeDate, out var parsedDate))
            {
                return null;
            }

            return new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, TimeSpan.Zero);
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            TimeDate = DateOnly.FromDateTime(value.Value.Date).ToString("dd.MM.yyyy");
            IsTimeDatePickerOpen = false;
            RaisePropertyChanged(nameof(TimeDatePicker));
        }
    }

    public void MoveTimeEntryColumn(string? sourceKey, string? targetKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(targetKey) || sourceKey == targetKey)
        {
            return;
        }

        var source = TimeEntryColumns.FirstOrDefault(column => column.Key == sourceKey);
        var target = TimeEntryColumns.FirstOrDefault(column => column.Key == targetKey);
        if (source is null || target is null)
        {
            return;
        }

        var oldIndex = TimeEntryColumns.IndexOf(source);
        var newIndex = TimeEntryColumns.IndexOf(target);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            return;
        }

        TimeEntryColumns.Move(oldIndex, newIndex);
        RefreshTimeEntryCells();
    }

    private void RefreshTimeEntryCells()
    {
        foreach (var entry in TimeEntries)
        {
            entry.ApplyColumnOrder(TimeEntryColumns);
        }

        foreach (var entry in TimeEntryHistory)
        {
            entry.ApplyColumnOrder(TimeEntryColumns);
        }
    }

    public void ToggleTimeDatePicker() => IsTimeDatePickerOpen = !IsTimeDatePickerOpen;

    public void EnsureTimeDatePickerOpen() => IsTimeDatePickerOpen = true;

    public void CloseAllPickers() => IsTimeDatePickerOpen = false;

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

    public string PayrollPreviewSummary
    {
        get => _payrollPreviewSummary;
        private set => SetProperty(ref _payrollPreviewSummary, value);
    }

    public string PayrollPreviewTitle
    {
        get => _payrollPreviewTitle;
        private set => SetProperty(ref _payrollPreviewTitle, value);
    }

    public bool HasPayrollPreviewLines => PayrollPreviewLines.Count > 0;
    public bool HasPayrollPreviewDerivationGroups => PayrollPreviewDerivationGroups.Any(group => group.HasItems);
    public bool IsPayrollPreviewDerivationVisible
    {
        get => _isPayrollPreviewDerivationVisible;
        set
        {
            if (SetProperty(ref _isPayrollPreviewDerivationVisible, value))
            {
                RaisePropertyChanged(nameof(ShowPayrollPreviewDerivation));
                RaisePropertyChanged(nameof(ShowPayrollPreviewResultOnly));
                RaisePropertyChanged(nameof(ShowPayrollPreviewSplitView));
            }
        }
    }

    public bool ShowPayrollPreviewDerivation => IsPayrollPreviewDerivationVisible && HasPayrollPreviewDerivationGroups;
    public bool ShowPayrollPreviewResultOnly => HasPayrollPreviewLines && !ShowPayrollPreviewDerivation;
    public bool ShowPayrollPreviewSplitView => HasPayrollPreviewLines && ShowPayrollPreviewDerivation;

    public bool IsSubjectToWithholdingTax
    {
        get => _isSubjectToWithholdingTax;
        private set
        {
            if (SetProperty(ref _isSubjectToWithholdingTax, value))
            {
                RaisePropertyChanged(nameof(CanSaveWithholdingTax));
                RaisePropertyChanged(nameof(CanResetWithholdingTax));
                SaveWithholdingTaxCommand.RaiseCanExecuteChanged();
                ResetWithholdingTaxCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string WithholdingTaxRatePercent
    {
        get => _withholdingTaxRatePercent;
        set => SetProperty(ref _withholdingTaxRatePercent, value);
    }

    public string WithholdingTaxCorrectionAmountChf
    {
        get => _withholdingTaxCorrectionAmountChf;
        set => SetProperty(ref _withholdingTaxCorrectionAmountChf, value);
    }

    public string? WithholdingTaxCorrectionText
    {
        get => _withholdingTaxCorrectionText;
        set => SetProperty(ref _withholdingTaxCorrectionText, value);
    }

    public string TimeDate
    {
        get => _timeDate;
        set
        {
            if (SetProperty(ref _timeDate, value))
            {
                RaisePropertyChanged(nameof(TimeDatePicker));
            }
        }
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

    public string VehiclePauschalzone1
    {
        get => _vehiclePauschalzone1;
        set => SetProperty(ref _vehiclePauschalzone1, value);
    }

    public string VehiclePauschalzone2
    {
        get => _vehiclePauschalzone2;
        set => SetProperty(ref _vehiclePauschalzone2, value);
    }

    public string VehicleRegiezone1
    {
        get => _vehicleRegiezone1;
        set => SetProperty(ref _vehicleRegiezone1, value);
    }

    public MonthlyTimeEntryItemViewModel? SelectedTimeEntry
    {
        get => _selectedTimeEntry;
        set
        {
            if (SetProperty(ref _selectedTimeEntry, value))
            {
                if (value is not null)
                {
                    PopulateTimeEntryForm(value);
                }

                RaisePropertyChanged(nameof(CanManageRecord));
                RaisePropertyChanged(nameof(CanSaveTimeEntry));
                RaisePropertyChanged(nameof(CanDeleteTimeEntry));
                RaisePropertyChanged(nameof(CanSaveExpenseEntry));
                RaisePropertyChanged(nameof(CanSaveWithholdingTax));
                RaisePropertyChanged(nameof(CanResetWithholdingTax));
                RaiseActionStateChanged();
            }
        }
    }

    public MonthlyExpenseEntryItemViewModel? SelectedExpenseEntry
    {
        get => _selectedExpenseEntry;
        set
        {
            if (SetProperty(ref _selectedExpenseEntry, value))
            {
                if (value is not null)
                {
                    ExpensesTotal = NumericFormatManager.FormatDecimal(value.ExpensesTotalChf, "0.00");
                }

                RaisePropertyChanged(nameof(CanManageRecord));
                RaisePropertyChanged(nameof(CanSaveTimeEntry));
                RaisePropertyChanged(nameof(CanDeleteTimeEntry));
                RaisePropertyChanged(nameof(CanSaveExpenseEntry));
                RaiseActionStateChanged();
            }
        }
    }

    public string ExpensesTotal
    {
        get => _expensesTotal;
        set => SetProperty(ref _expensesTotal, value);
    }

    public Task ActivateMonthFromTimeEntryAsync(MonthlyTimeEntryItemViewModel? entry)
    {
        if (entry is null)
        {
            return Task.CompletedTask;
        }

        PopulateTimeEntryForm(entry);

        if (SelectedMonth?.Year == entry.Year && SelectedMonth?.Month == entry.Month)
        {
            SelectedTimeEntry = entry;
            return Task.CompletedTask;
        }

        _pendingTimeEntrySelectionId = entry.TimeEntryId;
        return ActivateMonthAsync(entry.Year, entry.Month);
    }

    public Task ActivateMonthFromExpenseEntryAsync(MonthlyExpenseEntryItemViewModel? entry)
    {
        if (entry is null)
        {
            return Task.CompletedTask;
        }

        SelectedExpenseEntry = entry;

        if (SelectedMonth?.Year == entry.Year && SelectedMonth?.Month == entry.Month)
        {
            return Task.CompletedTask;
        }

        _pendingExpenseEntrySelectionId = entry.ExpenseEntryId;
        return ActivateMonthAsync(entry.Year, entry.Month);
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
        PayrollPreviewTitle = "Lohn-Voransicht";
        PayrollPreviewSummary = "Lohn-Voransicht wird nach dem Laden des Monats angezeigt.";
        TimeEntries.Clear();
        TimeEntryHistory.Clear();
        ExpenseEntryHistory.Clear();
        SelectedTimeEntry = null;
        SelectedExpenseEntry = null;
        _pendingTimeEntrySelectionId = null;
        _pendingExpenseEntrySelectionId = null;
        PreviewRows.Clear();
        PreviewNotes.Clear();
        PayrollPreviewLines.Clear();
        PayrollPreviewDerivationGroups.Clear();
        PayrollPreviewNotes.Clear();
        RaisePropertyChanged(nameof(HasPayrollPreviewLines));
        RaisePropertyChanged(nameof(HasPayrollPreviewDerivationGroups));
        RaisePropertyChanged(nameof(ShowPayrollPreviewDerivation));
        RaisePropertyChanged(nameof(ShowPayrollPreviewResultOnly));
        RaisePropertyChanged(nameof(ShowPayrollPreviewSplitView));
        PrepareNewTimeEntry();
        PrepareNewExpenseEntry();
    }

    private async Task LoadAsync()
    {
        if (!_currentEmployeeId.HasValue || !SelectedMonth.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(LoadCurrentRecordAsync);
    }

    public async Task ReloadCurrentMonthAsync()
    {
        if (!_currentEmployeeId.HasValue || !SelectedMonth.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(LoadCurrentRecordAsync);
    }

    public void ApplyPayrollPreviewHelpOptions(IReadOnlyCollection<PayrollPreviewHelpOptionDto> options)
    {
        _payrollPreviewHelpOptions = options
            .GroupBy(option => option.Code, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        RefreshVisiblePayrollPreviewLines();
    }

    private async Task ActivateMonthAsync(int year, int month)
    {
        var offset = SelectedMonth?.Offset ?? TimeSpan.Zero;
        var normalizedMonth = new DateTimeOffset(year, month, 1, 0, 0, 0, offset);
        await ApplySelectedMonthAsync(normalizedMonth, forceReload: true);
    }

    private async Task ApplySelectedMonthAsync(DateTimeOffset normalizedMonth, bool forceReload)
    {
        var changed = SetProperty(ref _selectedMonth, normalizedMonth);
        if (changed)
        {
            SyncSelectedMonthText(normalizedMonth);
            RaisePropertyChanged(nameof(ExpensePayrollMonth));
            RaisePropertyChanged(nameof(TimePayrollMonth));
            RaisePropertyChanged(nameof(SelectedMonthPickerDate));
            PrepareNewTimeEntry();
            PrepareNewExpenseEntry();
            ResetLoadedRecordState(preservePendingSelection: true);
        }

        if ((changed || forceReload) && _currentEmployeeId.HasValue)
        {
            if (IsBusy)
            {
                _loadCurrentRecordAfterBusy = true;
                return;
            }

            await LoadAsync();
        }
    }

    private void PrepareNewTimeEntry()
    {
        SelectedTimeEntry = null;
        TimeDate = GetDefaultTimeEntryDate().ToString("dd.MM.yyyy");
        HoursWorked = "0";
        NightHours = "0";
        SundayHours = "0";
        HolidayHours = "0";
        VehiclePauschalzone1 = "0";
        VehiclePauschalzone2 = "0";
        VehicleRegiezone1 = "0";
        TimeNote = null;
    }

    private DateOnly GetDefaultTimeEntryDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!SelectedMonth.HasValue)
        {
            return today;
        }

        return SelectedMonth.Value.Year == today.Year && SelectedMonth.Value.Month == today.Month
            ? today
            : new DateOnly(SelectedMonth.Value.Year, SelectedMonth.Value.Month, 1);
    }

    private void PrepareNewExpenseEntry()
    {
        ExpensesTotal = "0";
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
                    ParseRequiredDecimal(VehiclePauschalzone1, nameof(VehiclePauschalzone1)),
                    ParseRequiredDecimal(VehiclePauschalzone2, nameof(VehiclePauschalzone2)),
                    ParseRequiredDecimal(VehicleRegiezone1, nameof(VehicleRegiezone1)),
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
                    ParseRequiredDecimal(ExpensesTotal, nameof(ExpensesTotal))));

            ApplyDetails(details);
            ActionMessage = "Spesen gespeichert.";
        });
    }

    private async Task SaveWithholdingTaxAsync()
    {
        await SaveWithholdingTaxAsync("Quellensteuer gespeichert.");
    }

    private async Task ResetWithholdingTaxAsync()
    {
        WithholdingTaxRatePercent = "0";
        WithholdingTaxCorrectionAmountChf = "0.00";
        WithholdingTaxCorrectionText = null;
        await SaveWithholdingTaxAsync("Quellensteuer zurueckgesetzt.");
    }

    private async Task SaveWithholdingTaxAsync(string successMessage)
    {
        await EnsureCurrentRecordLoadedAsync();

        if (!_currentMonthlyRecordId.HasValue)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var details = await _monthlyRecordService.SaveWithholdingTaxAsync(
                new SaveMonthlyWithholdingTaxCommand(
                    _currentMonthlyRecordId.Value,
                    ParseRequiredDecimal(WithholdingTaxRatePercent, nameof(WithholdingTaxRatePercent)),
                    ParseRequiredDecimal(WithholdingTaxCorrectionAmountChf, nameof(WithholdingTaxCorrectionAmountChf)),
                    WithholdingTaxCorrectionText));

            ApplyDetails(details);
            ActionMessage = successMessage;
        });
    }

    private void ApplyDetails(MonthlyRecordDetailsDto details)
    {
        _currentMonthlyRecordId = details.Header.MonthlyRecordId;
        ContextTitle = $"Monatserfassung fuer {details.Header.EmployeeFullName}";
        ContextDescription = $"{details.Header.Month:00}/{details.Header.Year} | Monatserfassung fuer Zeiten und Spesen.";
        StatusSummary = $"Status: {FormatStatus(details.Header.Status)}";
        IsSubjectToWithholdingTax = details.Header.IsSubjectToWithholdingTax;
        WithholdingTaxRatePercent = NumericFormatManager.FormatDecimal(details.Header.WithholdingTaxRatePercent, "0.###");
        WithholdingTaxCorrectionAmountChf = NumericFormatManager.FormatDecimal(details.Header.WithholdingTaxCorrectionAmountChf, "0.00");
        WithholdingTaxCorrectionText = details.Header.WithholdingTaxCorrectionText;
        ContractSummary = details.Header.ContractValidFrom.HasValue
            ? $"Vertragsstand: {details.Header.ContractValidFrom:dd.MM.yyyy} bis {FormatDate(details.Header.ContractValidTo)} | {PayrollAmountFormatter.FormatChf(details.Header.HourlyRateChf ?? 0m)}/h | BVG {PayrollAmountFormatter.FormatChf(details.Header.MonthlyBvgDeductionChf ?? 0m)}"
            : "Vertragsstand: kein passender Vertrag fuer den Monat gefunden.";
        TotalsSummary = $"Stunden {details.Header.TotalWorkedHours:0.##} | Spezialstunden {details.Header.TotalSpecialHours:0.##} | Spesen {PayrollAmountFormatter.FormatChf(details.Header.TotalExpensesChf)} | Fahrzeug {PayrollAmountFormatter.FormatChf(details.Header.TotalVehicleCompensationChf)}";
        PreviewSummary = "Monatsvorschau zeigt alle vorhandenen Monate der selektierten Person tabellarisch untereinander.";
        PreviewTotals = $"Arbeitsstunden {details.Header.TotalWorkedHours:0.##} | Spezialstunden {details.Header.TotalSpecialHours:0.##} | Spesen {PayrollAmountFormatter.FormatChf(details.Header.TotalExpensesChf)} | Fahrzeug {PayrollAmountFormatter.FormatChf(details.Header.TotalVehicleCompensationChf)}";
        PreviewEntryCounts = $"Eintraege im aktuellen Monat: Zeiten {details.TimeEntries.Count} | Spesenblock {(details.ExpenseEntry is null ? 0 : 1)}";
        PayrollPreviewTitle = $"{details.Header.EmployeeLastName} {details.Header.EmployeeFirstName} | {details.Header.PersonnelNumber}";
        PayrollPreviewSummary = details.PayrollPreview.Lines.Count == 0
            ? "Monat noch nicht erfasst"
            : $"{details.Header.Month:00}/{details.Header.Year}";

        TimeEntries.Clear();
        foreach (var entry in details.TimeEntries)
        {
            var timeEntry = new MonthlyTimeEntryItemViewModel
            {
                TimeEntryId = entry.TimeEntryId,
                Year = entry.WorkDate.Year,
                Month = entry.WorkDate.Month,
                WorkDate = entry.WorkDate,
                HoursWorked = entry.HoursWorked,
                NightHours = entry.NightHours,
                SundayHours = entry.SundayHours,
                HolidayHours = entry.HolidayHours,
                VehiclePauschalzone1Chf = entry.VehiclePauschalzone1Chf,
                VehiclePauschalzone2Chf = entry.VehiclePauschalzone2Chf,
                VehicleRegiezone1Chf = entry.VehicleRegiezone1Chf,
                IsCurrentMonth = true,
                Note = entry.Note,
                Summary = $"{entry.WorkDate:dd.MM.yyyy} | Arbeit {entry.HoursWorked:0.##} h | Nacht {entry.NightHours:0.##} | Sonntag {entry.SundayHours:0.##} | Feiertag {entry.HolidayHours:0.##} | Fahrzeug {PayrollAmountFormatter.FormatChf(entry.VehiclePauschalzone1Chf + entry.VehiclePauschalzone2Chf + entry.VehicleRegiezone1Chf)}"
            };
            timeEntry.ApplyColumnOrder(TimeEntryColumns);
            TimeEntries.Add(timeEntry);
        }

        ExpensesTotal = details.ExpenseEntry is not null
            ? NumericFormatManager.FormatDecimal(details.ExpenseEntry.ExpensesTotalChf, "0.00")
            : "0";

        TimeEntryHistory.Clear();
        foreach (var entry in details.TimeEntryHistory)
        {
            var isCurrentMonth = entry.WorkDate.Year == details.Header.Year
                && entry.WorkDate.Month == details.Header.Month;

            var timeEntry = new MonthlyTimeEntryItemViewModel
            {
                TimeEntryId = entry.TimeEntryId,
                Year = entry.WorkDate.Year,
                Month = entry.WorkDate.Month,
                WorkDate = entry.WorkDate,
                HoursWorked = entry.HoursWorked,
                NightHours = entry.NightHours,
                SundayHours = entry.SundayHours,
                HolidayHours = entry.HolidayHours,
                VehiclePauschalzone1Chf = entry.VehiclePauschalzone1Chf,
                VehiclePauschalzone2Chf = entry.VehiclePauschalzone2Chf,
                VehicleRegiezone1Chf = entry.VehicleRegiezone1Chf,
                IsCurrentMonth = isCurrentMonth,
                Note = entry.Note,
                Summary = $"{entry.WorkDate:dd.MM.yyyy} | Monat {entry.WorkDate:MM/yyyy} | Arbeit {entry.HoursWorked:0.##} h | Nacht {entry.NightHours:0.##} | Sonntag {entry.SundayHours:0.##} | Feiertag {entry.HolidayHours:0.##} | Fahrzeug {PayrollAmountFormatter.FormatChf(entry.VehiclePauschalzone1Chf + entry.VehiclePauschalzone2Chf + entry.VehicleRegiezone1Chf)}"
            };
            timeEntry.ApplyColumnOrder(TimeEntryColumns);
            TimeEntryHistory.Add(timeEntry);
        }

        ExpenseEntryHistory.Clear();
        foreach (var entry in details.ExpenseEntryHistory)
        {
            ExpenseEntryHistory.Add(new MonthlyExpenseEntryItemViewModel
            {
                ExpenseEntryId = entry.ExpenseEntryId,
                Year = entry.Year,
                Month = entry.Month,
                ExpensesTotalChf = entry.ExpensesTotalChf,
                Summary = $"{entry.Month:00}/{entry.Year} | Diverse Spesen {PayrollAmountFormatter.FormatChf(entry.ExpensesTotalChf)}"
            });
        }

        if (_pendingTimeEntrySelectionId.HasValue)
        {
            var selectedTimeEntry = TimeEntries.FirstOrDefault(entry => entry.TimeEntryId == _pendingTimeEntrySelectionId.Value)
                ?? TimeEntryHistory.FirstOrDefault(entry => entry.TimeEntryId == _pendingTimeEntrySelectionId.Value);
            _pendingTimeEntrySelectionId = null;
            if (selectedTimeEntry is not null)
            {
                SelectedTimeEntry = selectedTimeEntry;
            }
        }

        if (_pendingExpenseEntrySelectionId.HasValue)
        {
            var selectedExpenseEntry = ExpenseEntryHistory.FirstOrDefault(entry => entry.ExpenseEntryId == _pendingExpenseEntrySelectionId.Value);
            _pendingExpenseEntrySelectionId = null;
            if (selectedExpenseEntry is not null)
            {
                SelectedExpenseEntry = selectedExpenseEntry;
            }
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

        _rawPayrollPreviewLines = details.PayrollPreview.Lines.ToArray();
        RefreshVisiblePayrollPreviewLines();

        PayrollPreviewDerivationGroups.Clear();
        foreach (var group in details.PayrollPreview.DerivationGroups)
        {
            PayrollPreviewDerivationGroups.Add(group);
        }

        PayrollPreviewNotes.Clear();
        foreach (var note in details.PayrollPreview.Notes)
        {
            PayrollPreviewNotes.Add(note);
        }

        RaisePropertyChanged(nameof(HasPayrollPreviewDerivationGroups));
        RaisePropertyChanged(nameof(ShowPayrollPreviewDerivation));
        RaisePropertyChanged(nameof(ShowPayrollPreviewResultOnly));
        RaisePropertyChanged(nameof(ShowPayrollPreviewSplitView));

        TimeCaptureChanged?.Invoke(this, EventArgs.Empty);

        RaisePropertyChanged(nameof(CanSaveTimeEntry));
        RaisePropertyChanged(nameof(CanDeleteTimeEntry));
        RaisePropertyChanged(nameof(CanSaveExpenseEntry));
        RaisePropertyChanged(nameof(CanSaveWithholdingTax));
        RaisePropertyChanged(nameof(CanResetWithholdingTax));
        RaiseActionStateChanged();
    }

    private void PopulateTimeEntryForm(MonthlyTimeEntryItemViewModel entry)
    {
        TimeDate = entry.WorkDate.ToString("dd.MM.yyyy");
        HoursWorked = NumericFormatManager.FormatDecimal(entry.HoursWorked, "0.##");
        NightHours = NumericFormatManager.FormatDecimal(entry.NightHours, "0.##");
        SundayHours = NumericFormatManager.FormatDecimal(entry.SundayHours, "0.##");
        HolidayHours = NumericFormatManager.FormatDecimal(entry.HolidayHours, "0.##");
        VehiclePauschalzone1 = NumericFormatManager.FormatDecimal(entry.VehiclePauschalzone1Chf, "0.00");
        VehiclePauschalzone2 = NumericFormatManager.FormatDecimal(entry.VehiclePauschalzone2Chf, "0.00");
        VehicleRegiezone1 = NumericFormatManager.FormatDecimal(entry.VehicleRegiezone1Chf, "0.00");
        TimeNote = entry.Note;
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
            PayrollPreviewSummary = message;
        }
        finally
        {
            IsBusy = false;
        }

        if (_loadCurrentRecordAfterBusy && _currentEmployeeId.HasValue && SelectedMonth.HasValue)
        {
            _loadCurrentRecordAfterBusy = false;
            await ExecuteBusyAsync(LoadCurrentRecordAsync);
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

    private void ResetLoadedRecordState(bool preservePendingSelection = false)
    {
        _currentMonthlyRecordId = null;
        StatusSummary = "Kein Monatskontext aktiv.";
        ContractSummary = "Kein Vertragsstand geladen.";
        TotalsSummary = "Noch keine Monatssummen vorhanden.";
        ActionMessage = "Noch keine Aktion ausgefuehrt.";
        PreviewSummary = "Monatsvorschau wird nach dem Laden des Monats angezeigt.";
        PreviewTotals = "Noch keine verdichteten Monatswerte vorhanden.";
        PreviewEntryCounts = "Noch keine Eintraege im aktuellen Monat vorhanden.";
        PayrollPreviewTitle = "Lohn-Voransicht";
        PayrollPreviewSummary = "Lohn-Voransicht wird nach dem Laden des Monats angezeigt.";
        TimeEntries.Clear();
        TimeEntryHistory.Clear();
        ExpenseEntryHistory.Clear();
        SelectedTimeEntry = null;
        SelectedExpenseEntry = null;
        if (!preservePendingSelection)
        {
            _pendingTimeEntrySelectionId = null;
            _pendingExpenseEntrySelectionId = null;
        }
        PreviewRows.Clear();
        PreviewNotes.Clear();
        _rawPayrollPreviewLines = [];
        PayrollPreviewLines.Clear();
        PayrollPreviewDerivationGroups.Clear();
        PayrollPreviewNotes.Clear();
        RaisePropertyChanged(nameof(HasPayrollPreviewLines));
        RaisePropertyChanged(nameof(HasPayrollPreviewDerivationGroups));
        TimeCaptureChanged?.Invoke(this, EventArgs.Empty);
        PrepareNewExpenseEntry();
        RaisePropertyChanged(nameof(CanSaveTimeEntry));
        RaisePropertyChanged(nameof(CanDeleteTimeEntry));
        RaisePropertyChanged(nameof(CanSaveExpenseEntry));
        RaisePropertyChanged(nameof(CanSaveWithholdingTax));
        RaisePropertyChanged(nameof(CanResetWithholdingTax));
        RaiseActionStateChanged();
    }

    private void RaiseActionStateChanged()
    {
        LoadMonthlyRecordCommand.RaiseCanExecuteChanged();
        NewTimeEntryCommand.RaiseCanExecuteChanged();
        SaveTimeEntryCommand.RaiseCanExecuteChanged();
        DeleteTimeEntryCommand.RaiseCanExecuteChanged();
        ResetExpenseValuesCommand.RaiseCanExecuteChanged();
        SaveExpenseEntryCommand.RaiseCanExecuteChanged();
        SaveWithholdingTaxCommand.RaiseCanExecuteChanged();
        ResetWithholdingTaxCommand.RaiseCanExecuteChanged();
    }

    private void RefreshVisiblePayrollPreviewLines()
    {
        PayrollPreviewLines.Clear();

        foreach (var line in _rawPayrollPreviewLines)
        {
            PayrollPreviewLines.Add(line with
            {
                Detail = ResolveVisiblePayrollPreviewDetail(line)
            });
        }

        RaisePropertyChanged(nameof(HasPayrollPreviewLines));
        RaisePropertyChanged(nameof(HasPayrollPreviewDerivationGroups));
        RaisePropertyChanged(nameof(ShowPayrollPreviewDerivation));
        RaisePropertyChanged(nameof(ShowPayrollPreviewResultOnly));
        RaisePropertyChanged(nameof(ShowPayrollPreviewSplitView));
    }

    private string? ResolveVisiblePayrollPreviewDetail(MonthlyPayrollPreviewLineDto line)
    {
        if (!_payrollPreviewHelpOptions.TryGetValue(line.Code, out var option))
        {
            return line.Detail;
        }

        if (!option.IsEnabled)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(option.HelpText)
            ? line.Detail
            : option.HelpText.Trim();
    }

    private static DateOnly ParseRequiredDate(string value, string fieldName)
    {
        if (!TryParseDate(value, out var parsedDate))
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
        return NumericFormatManager.TryParseDecimal(value, out parsedValue);
    }

    private static bool TryParseDate(string value, out DateOnly parsedDate)
    {
        var supportedFormats = new[]
        {
            "yyyy-MM-dd",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy"
        };

        return DateOnly.TryParseExact(value, supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
            || DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate);
    }

    private static bool TryParseMonth(string value, out DateTimeOffset normalizedMonth)
    {
        normalizedMonth = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var supportedFormats = new[]
        {
            "MM/yyyy",
            "M/yyyy",
            "MM.yyyy",
            "M.yyyy",
            "yyyy-MM",
            "yyyy/M",
            "yyyy/MM"
        };

        if (DateTime.TryParseExact(value, supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedMonth))
        {
            normalizedMonth = new DateTimeOffset(parsedMonth.Year, parsedMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
            return true;
        }

        return false;
    }

    private void SyncSelectedMonthText(DateTimeOffset normalizedMonth)
    {
        var formattedMonth = normalizedMonth.ToString("MM/yyyy");
        if (_selectedMonthText != formattedMonth)
        {
            _selectedMonthText = formattedMonth;
            RaisePropertyChanged(nameof(SelectedMonthText));
        }
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
