using Payroll.Domain.Settings;

namespace Payroll.Domain.Tests;

public sealed class PayrollSettingsTests
{
    [Fact]
    public void GetVacationCompensationRate_UsesAge50PlusRate_FromFirstDayOfYearEmployeeTurns50()
    {
        var settings = new PayrollSettings(
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m);

        var rate = settings.GetVacationCompensationRate(
            new DateOnly(1976, 11, 20),
            new DateOnly(2026, 1, 1));

        Assert.Equal(0.1264m, rate);
    }

    [Fact]
    public void GetVacationCompensationRate_UsesStandardRate_BeforeYearEmployeeTurns50()
    {
        var settings = new PayrollSettings(
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m);

        var rate = settings.GetVacationCompensationRate(
            new DateOnly(1977, 11, 20),
            new DateOnly(2026, 12, 31));

        Assert.Equal(0.1064m, rate);
    }
}
