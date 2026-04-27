namespace Payroll.Desktop.ViewModels;

public sealed class EmploymentContractHistoryItemViewModel : ViewModelBase
{
    public Guid ContractId { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public string HourlyRateDisplay { get; init; } = string.Empty;
    public string MonthlySalaryDisplay { get; init; } = string.Empty;
    public string MonthlyBvgDisplay { get; init; } = string.Empty;
    public string SpecialSupplementDisplay { get; init; } = string.Empty;
    public string WageTypeDisplay { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }

    public string ValidFromDisplay => ValidFrom.ToString("dd.MM.yyyy");
    public string ValidToDisplay => ValidTo?.ToString("dd.MM.yyyy") ?? "offen";
    public string CompactSummary => $"{WageTypeDisplay} | {MonthlySalaryDisplay} | {MonthlyBvgDisplay} | {SpecialSupplementDisplay}";
    public bool HasStatusBadge => IsCurrent;
    public string StatusBadgeText => "Aktiver Stand";
    public string StatusBadgeBackground => "#FFE7F6EC";
    public string StatusBadgeForeground => "#FF1F6B45";
}
