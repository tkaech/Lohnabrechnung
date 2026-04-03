using Payroll.Domain.Employees;

namespace Payroll.Domain.Tests;

public sealed class EmploymentContractTests
{
    [Fact]
    public void CalculatesGrossPayFromDecimalHours()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), null, 32.50m, 280m);

        var grossPay = contract.CalculateGrossPay(7.75m);

        Assert.Equal(251.875m, grossPay);
        Assert.Equal(280m, contract.MonthlyBvgDeductionChf);
    }

    [Fact]
    public void IsActiveOn_RespectsDateRange()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 30m, 250m);

        Assert.True(contract.IsActiveOn(new DateOnly(2026, 6, 1)));
        Assert.False(contract.IsActiveOn(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Contract_CanCarrySupplementSettings()
    {
        var settings = new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m);
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), null, 30m, 250m, settings);

        Assert.Equal(0.25m, contract.SupplementSettings.NightSupplementRate);
        Assert.Equal(0.50m, contract.SupplementSettings.SundaySupplementRate);
        Assert.Equal(1.00m, contract.SupplementSettings.HolidaySupplementRate);
    }

    [Fact]
    public void UpdateTerms_ChangesRatesAndValidity()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), null, 30m, 250m);

        contract.UpdateTerms(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 12, 31),
            35m,
            300m,
            new WorkTimeSupplementSettings(0.25m, null, 0.75m));

        Assert.Equal(new DateOnly(2026, 2, 1), contract.ValidFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), contract.ValidTo);
        Assert.Equal(35m, contract.HourlyRateChf);
        Assert.Equal(300m, contract.MonthlyBvgDeductionChf);
        Assert.Equal(0.25m, contract.SupplementSettings.NightSupplementRate);
        Assert.Equal(0.75m, contract.SupplementSettings.HolidaySupplementRate);
        Assert.NotNull(contract.UpdatedAtUtc);
    }
}
