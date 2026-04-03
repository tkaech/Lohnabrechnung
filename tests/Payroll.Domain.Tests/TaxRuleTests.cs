using Payroll.Domain.Tax;

namespace Payroll.Domain.Tests;

public sealed class TaxRuleTests
{
    [Fact]
    public void CalculatesTaxFromConfiguredRate()
    {
        var rule = new TaxRule("ZH", "A0", 0.045m, new DateOnly(2026, 1, 1));

        Assert.Equal(180m, rule.CalculateTax(4000m));
    }

    [Fact]
    public void Constructor_RejectsRateOutsideRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TaxRule("ZH", "A0", 1.5m, new DateOnly(2026, 1, 1)));
    }
}
