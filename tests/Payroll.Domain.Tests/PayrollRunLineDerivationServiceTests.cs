using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.Tests;

public sealed class PayrollRunLineDerivationServiceTests
{
    [Fact]
    public void DeriveForEmployee_CreatesBaseSupplementsExpensesVehicleSocialContributionsAndBvgLines()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            280m,
            3.00m);

        var timeEntries = new[]
        {
            new TimeEntry(employeeId, new DateOnly(2026, 3, 1), 8m, 2m, 0m, 0m, null, 2m),
            new TimeEntry(employeeId, new DateOnly(2026, 3, 2), 6m, 0m, 1m, 0.5m, null, 0m, 3m, 4m)
        };
        var workSummary = PayrollWorkSummary.FromTimeEntries(employeeId, timeEntries);

        var expenses = new[]
        {
            new ExpenseEntry(employeeId, 24.50m)
        };

        var payrollSettings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vehiclePauschalzone1RateChf: 1.5m,
            vehiclePauschalzone2RateChf: 2.0m,
            vehicleRegiezone1RateChf: 3.0m);

        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            payrollSettings,
            workSummary,
            expenses,
            timeEntries);

        Assert.Empty(result.Issues);
        Assert.Equal(15, result.Lines.Count);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BaseHours && line.AmountChf == 420m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.NightSupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SundaySupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.HolidaySupplement && line.AmountChf == 15m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SpecialSupplement && line.AmountChf == 42m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.VacationCompensation && line.AmountChf == 56.1792m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.Expense && line.ValueOrigin == PayrollLineValueOrigin.Direct);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.VehicleCompensation && line.Code == "VEHICLE_P1" && line.AmountChf == 3m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.VehicleCompensation && line.Code == "VEHICLE_P2" && line.AmountChf == 6m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.VehicleCompensation && line.Code == "VEHICLE_R1" && line.AmountChf == 12m);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SocialContribution && line.Code == "AHV_IV_EO");
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SocialContribution && line.Code == "ALV");
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SocialContribution && line.Code == "KTG_UVG");
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SocialContribution && line.Code == "AUSBILDUNG_FERIEN");
        Assert.Equal(-42.271206912m, result.Lines.Where(line => line.LineType == PayrollLineType.SocialContribution).Sum(line => line.AmountChf));
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
            0m,
            3.00m);

        var workSummary = new PayrollWorkSummary(employeeId, 8m, 2m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            new PayrollSettings(
                workTimeSupplementSettings: new WorkTimeSupplementSettings(null, 0.50m, null),
                ahvIvEoRate: 0.053m,
                alvRate: 0.011m,
                sicknessAccidentInsuranceRate: 0.00821m,
                trainingAndHolidayRate: 0.00015m,
                vacationCompensationRate: 0.1064m,
                vacationCompensationRateAge50Plus: 0.1264m,
                vehiclePauschalzone1RateChf: 1m,
                vehiclePauschalzone2RateChf: 1m,
                vehicleRegiezone1RateChf: 1m),
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
            280m,
            3.00m);

        var workSummary = new PayrollWorkSummary(employeeId, 8m, 4m, 3m, 2m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            new PayrollSettings(
                workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
                ahvIvEoRate: 0.053m,
                alvRate: 0.011m,
                sicknessAccidentInsuranceRate: 0.00821m,
                trainingAndHolidayRate: 0.00015m,
                vacationCompensationRate: 0.1064m,
                vacationCompensationRateAge50Plus: 0.1264m,
                vehiclePauschalzone1RateChf: 1m,
                vehiclePauschalzone2RateChf: 1m,
                vehicleRegiezone1RateChf: 1m),
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

    [Fact]
    public void DeriveForEmployee_UsesAge50PlusVacationCompensationRate_WhenEmployeeIsAtLeast50()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            0m,
            3.00m);

        var timeEntries = new[]
        {
            new TimeEntry(employeeId, new DateOnly(2026, 3, 6), 10m, 0m, 0m, 0m)
        };
        var workSummary = PayrollWorkSummary.FromTimeEntries(employeeId, timeEntries);
        var payrollSettings = new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 1m,
            vehiclePauschalzone2RateChf: 1m,
            vehicleRegiezone1RateChf: 1m);

        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            new DateOnly(1970, 3, 1),
            contract,
            payrollSettings,
            workSummary,
            [],
            timeEntries);

        var vacationCompensationLine = Assert.Single(result.Lines, line => line.LineType == PayrollLineType.VacationCompensation);
        Assert.Equal(41.712m, vacationCompensationLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_UsesAge50PlusVacationCompensationRate_FromStartOfYearEmployeeTurns50()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(
            employeeId,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            0m,
            3.00m);

        var timeEntries = new[]
        {
            new TimeEntry(employeeId, new DateOnly(2026, 3, 6), 10m, 0m, 0m, 0m)
        };
        var workSummary = PayrollWorkSummary.FromTimeEntries(employeeId, timeEntries);
        var payrollSettings = new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 1m,
            vehiclePauschalzone2RateChf: 1m,
            vehicleRegiezone1RateChf: 1m);

        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            new DateOnly(1976, 11, 20),
            contract,
            payrollSettings,
            workSummary,
            [],
            timeEntries);

        var vacationCompensationLine = Assert.Single(result.Lines, line => line.LineType == PayrollLineType.VacationCompensation);
        Assert.Equal(41.712m, vacationCompensationLine.AmountChf);
    }
}
