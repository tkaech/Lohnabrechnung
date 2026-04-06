namespace Payroll.Desktop.ViewModels;

public sealed class MonthlyExpenseEntryItemViewModel
{
    public required Guid ExpenseEntryId { get; init; }
    public required decimal ExpensesTotalChf { get; init; }
    public required string Summary { get; init; }
}
