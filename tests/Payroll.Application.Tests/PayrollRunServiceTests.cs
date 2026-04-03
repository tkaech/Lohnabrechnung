using Payroll.Application.Payroll;

namespace Payroll.Application.Tests;

public sealed class PayrollRunServiceTests
{
    [Fact]
    public void ServiceCanBeCreated()
    {
        var service = new PayrollRunService();

        Assert.NotNull(service);
    }
}
