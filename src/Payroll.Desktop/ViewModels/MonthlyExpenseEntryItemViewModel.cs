namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyExpenseEntryItemViewModel
{
    public required Guid ExpenseEntryId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal ExpensesTotalChf { get; init; }
    public required string Summary { get; init; }
}
