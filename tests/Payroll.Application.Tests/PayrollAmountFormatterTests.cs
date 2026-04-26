using Payroll.Application.Formatting;

namespace Payroll.Application.Tests;

public sealed class PayrollAmountFormatterTests
{
    [Fact]
    public void FormatAmount_UsesSwissSeparatorsAndTwoDecimals()
    {
        Assert.Equal("1'222'222'222.00", PayrollAmountFormatter.FormatAmount(1_222_222_222m));
    }

    [Fact]
    public void FormatChf_AppendsCurrencyCode()
    {
        Assert.Equal("1'222'222'222.00 CHF", PayrollAmountFormatter.FormatChf(1_222_222_222m));
    }
}
