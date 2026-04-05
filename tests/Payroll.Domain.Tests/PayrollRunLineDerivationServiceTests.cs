using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Payroll;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.Tests;

public sealed class PayrollRunLineDerivationServiceTests
{
    [Fact]
    public void DeriveForEmployee_CreatesBaseSupplementsExpensesVehicleAndBvgLines()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            280m);

        var workSummary = PayrollWorkSummary.FromTimeEntries(employeeId, [
            new TimeEntry(employeeId, new DateOnly(2026, 3, 1), 8m, 2m),
            new TimeEntry(employeeId, new DateOnly(2026, 3, 2), 6m, 0m, 1m, 0.5m)
        ]);

        var expenses = new[]
        {
            new ExpenseEntry(employeeId, new DateOnly(2026, 3, 10), 24.50m)
        };

        var vehicleCompensations = new[]
        {
            new VehicleCompensation(employeeId, new DateOnly(2026, 3, 31), 120m, "Monthly vehicle compensation")
        };

        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            contract,
            new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            workSummary,
            expenses,
            vehicleCompensations);

        Assert.Empty(result.Issues);
        Assert.Equal(7, result.Lines.Count);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BaseHours && line.AmountChf == 420m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.NightSupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SundaySupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.HolidaySupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.Expense && line.ValueOrigin == PayrollLineValueOrigin.Direct);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.VehicleCompensation && line.AmountChf == 120m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BvgDeduction && line.AmountChf == -280m);
    }

    [Fact]
    public void DeriveForEmployee_ReturnsIssueWhenSupplementRateIsMissing()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            0m);

        var workSummary = new PayrollWorkSummary(employeeId, 8m, 2m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            contract,
            new WorkTimeSupplementSettings(null, 0.50m, null),
            workSummary,
            [],
            []);

        Assert.Single(result.Issues);
        Assert.Equal("MISSING_NIGHT_RULE", result.Issues.Single().Code);
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.NightSupplement);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BaseHours);
    }

    [Fact]
    public void WorkSummary_AggregatesMultipleTimeEntries()
    {
        var employeeId = Guid.NewGuid();

        var summary = PayrollWorkSummary.FromTimeEntries(employeeId, [
            new TimeEntry(employeeId, new DateOnly(2026, 3, 1), 8m, 1m),
            new TimeEntry(employeeId, new DateOnly(2026, 3, 2), 7.5m, 0.5m, 1m, 0.25m)
        ]);

        Assert.Equal(15.5m, summary.WorkHours);
        Assert.Equal(1.5m, summary.NightHours);
        Assert.Equal(1m, summary.SundayHours);
        Assert.Equal(0.25m, summary.HolidayHours);
    }

    [Fact]
    public void WorkSummary_ExposesAmbiguousOverlapWhenSpecialHoursExceedWorkHours()
    {
        var summary = new PayrollWorkSummary(Guid.NewGuid(), 8m, 4m, 3m, 2m);

        Assert.True(summary.HasAmbiguousSpecialHourOverlap);
    }

    [Fact]
    public void DeriveForEmployee_ReturnsIssueAndSkipsSupplementsWhenOverlapIsAmbiguous()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            280m);

        var workSummary = new PayrollWorkSummary(employeeId, 8m, 4m, 3m, 2m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            contract,
            new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            workSummary,
            [],
            []);

        Assert.Contains(result.Issues, issue => issue.Code == "AMBIGUOUS_SPECIAL_HOUR_OVERLAP");
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.NightSupplement);
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.SundaySupplement);
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.HolidaySupplement);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BaseHours);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BvgDeduction);
    }
}
