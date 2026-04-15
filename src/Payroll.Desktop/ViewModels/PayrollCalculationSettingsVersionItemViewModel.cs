namespace Payroll.Desktop.ViewModels;

public sealed class PayrollCalculationSettingsVersionItemViewModel : ViewModelBase
{
    public Guid VersionId { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public string Summary { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }

    public string ValidFromDisplay => ValidFrom.ToString("dd.MM.yyyy");
    public string ValidToDisplay => ValidTo?.ToString("dd.MM.yyyy") ?? "offen";
    public string StatusDisplay => IsCurrent ? "Aktiver Stand" : string.Empty;
    public bool ShowCurrentBadge => IsCurrent;
}
