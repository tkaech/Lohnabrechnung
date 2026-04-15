using Payroll.Domain.AHV;

namespace Payroll.Domain.Tests;

public sealed class ContributionRateTests
{
    [Fact]
    public void CalculatesEmployeeAndEmployerContributions()
    {
        var rate = new ContributionRate("AHV", 0.053m, 0.053m, new DateOnly(2026, 1, 1));

        Assert.Equal(212m, rate.CalculateEmployeeContribution(4000m));
        Assert.Equal(212m, rate.CalculateEmployerContribution(4000m));
    }

    [Fact]
    public void IsValidOn_UsesConfiguredValidityWindow()
    {
        var rate = new ContributionRate("AHV", 0.053m, 0.053m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.True(rate.IsValidOn(new DateOnly(2026, 7, 1)));
        Assert.False(rate.IsValidOn(new DateOnly(2027, 1, 1)));
    }
}
