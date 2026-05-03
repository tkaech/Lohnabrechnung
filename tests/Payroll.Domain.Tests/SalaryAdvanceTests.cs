using Payroll.Domain.MonthlyRecords;

namespace Payroll.Domain.Tests;

public sealed class SalaryAdvanceTests
{
    [Fact]
    public void SalaryAdvance_OpenAmountReflectsPartialAndFullSettlement()
    {
        var employeeId = Guid.NewGuid();
        var advance = new SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 3, 600m, "Mitte Monat");

        advance.SaveSettlement(null, Guid.NewGuid(), 2026, 4, 150m, "April");
        advance.SaveSettlement(null, Guid.NewGuid(), 2026, 5, 450m, "Mai");

        Assert.Equal(600m, advance.AmountChf);
        Assert.Equal(600m, advance.SettledAmountChf);
        Assert.Equal(0m, advance.OpenAmountChf);
        Assert.True(advance.IsSettled);
    }

    [Fact]
    public void SalaryAdvance_SaveSettlement_RejectsAmountBeyondOpenBalance()
    {
        var employeeId = Guid.NewGuid();
        var advance = new SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 3, 300m, null);
        advance.SaveSettlement(null, Guid.NewGuid(), 2026, 4, 200m, null);

        var error = Assert.Throws<InvalidOperationException>(() =>
            advance.SaveSettlement(null, Guid.NewGuid(), 2026, 5, 150m, null));

        Assert.Contains("open salary advance balance", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100m, advance.OpenAmountChf);
    }
}
