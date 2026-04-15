using Payroll.Domain.Employees;

namespace Payroll.Domain.Tests;

public sealed class EmploymentContractTests
{
    [Fact]
    public void CalculatesGrossPayFromDecimalHours()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), null, 32.50m, 280m, 3.00m);

        var grossPay = contract.CalculateGrossPay(7.75m);
        var specialSupplement = contract.CalculateSpecialSupplement(7.75m);

        Assert.Equal(251.875m, grossPay);
        Assert.Equal(280m, contract.MonthlyBvgDeductionChf);
        Assert.Equal(23.25m, specialSupplement);
    }

    [Fact]
    public void IsActiveOn_RespectsDateRange()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 30m, 250m, 3.00m);

        Assert.True(contract.IsActiveOn(new DateOnly(2026, 6, 1)));
        Assert.False(contract.IsActiveOn(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void UpdateTerms_ChangesRatesAndValidity()
    {
        var contract = new EmploymentContract(Guid.NewGuid(), new DateOnly(2026, 1, 1), null, 30m, 250m, 3.00m);

        contract.UpdateTerms(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 12, 31),
            35m,
            300m,
            4.50m);

        Assert.Equal(new DateOnly(2026, 2, 1), contract.ValidFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), contract.ValidTo);
        Assert.Equal(35m, contract.HourlyRateChf);
        Assert.Equal(300m, contract.MonthlyBvgDeductionChf);
        Assert.Equal(4.50m, contract.SpecialSupplementRateChf);
        Assert.NotNull(contract.UpdatedAtUtc);
    }
}
