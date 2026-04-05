using Payroll.Domain.Expenses;

namespace Payroll.Domain.Tests;

public sealed class ExpenseEntryTests
{
    [Fact]
    public void ExpenseEntry_UsesChfAsCurrency()
    {
        var entry = new ExpenseEntry(Guid.NewGuid(), new DateOnly(2026, 3, 31), 48.20m);

        Assert.Equal(48.20m, entry.AmountChf);
        Assert.Equal("CHF", entry.Currency);
        Assert.Equal("EXP", ExpenseEntry.PayrollCode);
        Assert.Equal("Diverse Spesen", ExpenseEntry.DisplayName);
        Assert.Equal("EXP", entry.ExpenseTypeCode);
        Assert.Equal("Diverse Spesen", entry.Description);
    }

    [Fact]
    public void VehicleCompensation_IsTrackedSeparatelyFromExpenses()
    {
        var vehicleCompensation = new VehicleCompensation(Guid.NewGuid(), new DateOnly(2026, 3, 31), 120m, "Monthly vehicle compensation");

        Assert.Equal(120m, vehicleCompensation.AmountChf);
        Assert.Equal("Monthly vehicle compensation", vehicleCompensation.Description);
    }
}
