namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyPreviewRowViewModel
{
    public required string MonthLabel { get; init; }
    public required DateOnly EntryDate { get; init; }
    public required string EntryDateLabel { get; init; }
    public required string EntryType { get; init; }
    public required string QuantityOrAmount { get; init; }
    public required string Details { get; init; }
}
