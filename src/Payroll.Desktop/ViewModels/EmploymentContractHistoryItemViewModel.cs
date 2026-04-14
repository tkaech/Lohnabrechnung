namespace Payroll.Desktop.ViewModels;

public sealed class EmploymentContractHistoryItemViewModel : ViewModelBase
{
    private bool _isCurrent;

    public Guid ContractId { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public string HourlyRateDisplay { get; init; } = string.Empty;
    public string MonthlyBvgDisplay { get; init; } = string.Empty;
    public string SpecialSupplementDisplay { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string WarningText { get; init; } = string.Empty;
    public DelegateCommand? LoadToEditorCommand { get; init; }
    public DelegateCommand? ContinueEditingCommand { get; init; }
    public DelegateCommand? DeleteCommand { get; init; }

    public bool IsCurrent
    {
        get => _isCurrent;
        init => _isCurrent = value;
    }

    public bool HasOverlapWarning => !string.IsNullOrWhiteSpace(WarningText);
    public bool HasStatusBadge => IsCurrent || HasOverlapWarning;
    public string ValidFromDisplay => ValidFrom.ToString("dd.MM.yyyy");
    public string ValidToDisplay => ValidTo.HasValue ? ValidTo.Value.ToString("dd.MM.yyyy") : "offen";
    public string CompactRateSummary => $"{MonthlyBvgDisplay} | {SpecialSupplementDisplay}";
    public string StatusBadgeText => IsCurrent ? "Aktiver Stand" : "Warnung";
    public string StatusBadgeBackground => IsCurrent ? "#FFE7F6EC" : "#FFFDE8E8";
    public string StatusBadgeForeground => IsCurrent ? "#FF1F6B45" : "#FF9F2D2D";
    public string CardBackground => IsCurrent ? "#FFF6FBF8" : HasOverlapWarning ? "#FFFFF8F8" : "#FFFFFFFF";
    public string CardBorderBrush => IsCurrent ? "#FF8FC6A2" : HasOverlapWarning ? "#FFE0A3A3" : "#FFD7E1EA";
    public bool CanDelete => DeleteCommand is not null;
}
