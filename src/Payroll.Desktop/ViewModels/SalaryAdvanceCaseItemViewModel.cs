using Payroll.Application.Formatting;

namespace Payroll.Desktop.ViewModels;

public sealed class SalaryAdvanceCaseItemViewModel
{
    public required Guid SalaryAdvanceId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal AmountChf { get; init; }
    public required decimal SettledAmountChf { get; init; }
    public required decimal OpenAmountChf { get; init; }
    public required bool IsSettled { get; init; }
    public required bool IsCurrentMonth { get; init; }
    public required string ReferenceDisplay { get; init; }
    public required string? Note { get; init; }

    public bool IsOpen => OpenAmountChf > 0m;
    public bool CanDelete => IsCurrentMonth && SettledAmountChf == 0m;
    public string AmountDisplay => PayrollAmountFormatter.FormatChf(AmountChf);
    public string SettledAmountDisplay => PayrollAmountFormatter.FormatChf(SettledAmountChf);
    public string OpenAmountDisplay => PayrollAmountFormatter.FormatChf(OpenAmountChf);
    public string StatusDisplay => IsSettled ? "beglichen" : "pendent";
    public string NoteDisplay => string.IsNullOrWhiteSpace(Note) ? "-" : Note!;
    public string SelectionDisplay => $"{ReferenceDisplay} | offen {OpenAmountDisplay} | {StatusDisplay}";
}
