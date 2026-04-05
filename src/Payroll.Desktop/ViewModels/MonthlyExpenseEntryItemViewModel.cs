namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyExpenseEntryItemViewModel
{
    public required Guid ExpenseEntryId { get; init; }
    public required DateOnly ExpenseDate { get; init; }
    public required decimal AmountChf { get; init; }
    public required string Summary { get; init; }
}
