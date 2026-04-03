using Payroll.Domain.Payroll;

namespace Payroll.Domain.Tests;

public sealed class PayrollRunTests
{
    [Fact]
    public void PayrollRun_AggregatesHoursExpensesAndFixedBvgDeduction()
    {
        var employeeId = Guid.NewGuid();
        var payrollRun = new PayrollRun("2026-03", new DateOnly(2026, 3, 31));

        payrollRun.AddLine(PayrollRunLine.CreateCalculatedHourlyLine(employeeId, PayrollLineType.BaseHours, "BASE", "Regular hours", 160m, 30m));
        payrollRun.AddLine(PayrollRunLine.CreateCalculatedHourlyLine(employeeId, PayrollLineType.NightSupplement, "NIGHT", "Night supplement", 8m, 7.50m));
        payrollRun.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.Expense, "EXP", "Expenses", 85.50m));
        payrollRun.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.VehicleCompensation, "VEH", "Vehicle compensation", 120m));
        payrollRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employeeId, PayrollLineType.BvgDeduction, "BVG", "BVG deduction", 280m));

        Assert.Equal(168m, payrollRun.GetTotalHoursForEmployee(employeeId));
        Assert.Equal(4785.50m, payrollRun.GetNetAmountChfForEmployee(employeeId));
        Assert.Equal(4785.50m, payrollRun.GetTotalAmountChf());
    }

    [Fact]
    public void FinalizedPayrollRun_CannotBeModified()
    {
        var payrollRun = new PayrollRun("2026-03", new DateOnly(2026, 3, 31));
        payrollRun.FinalizeRun();

        Assert.Throws<InvalidOperationException>(() =>
            payrollRun.AddLine(PayrollRunLine.CreateDirectChfLine(Guid.NewGuid(), PayrollLineType.Expense, "EXP", "Expenses", 10m)));
    }
}
