using Payroll.Domain.Expenses;

namespace Payroll.Domain.Tests;

public sealed class ExpenseEntryTests
{
    [Fact]
    public void ExpenseEntry_TracksMonthlyExpenseTotalInChf()
    {
        var entry = new ExpenseEntry(Guid.NewGuid(), 48.20m);

        Assert.Equal(48.20m, entry.ExpensesTotalChf);
        Assert.Equal("CHF", entry.Currency);
        Assert.Equal("EXP", ExpenseEntry.PayrollCode);
        Assert.Equal("Diverse Spesen", ExpenseEntry.DisplayName);
        Assert.Equal("EXP", entry.ExpenseTypeCode);
        Assert.Equal("Diverse Spesen", entry.Description);
    }
}
