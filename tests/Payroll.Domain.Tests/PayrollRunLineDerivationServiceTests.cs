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
        Assert.Equal(-42.440818032m, result.Lines.Where(line => line.LineType == PayrollLineType.SocialContribution).Sum(line => line.AmountChf));
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.BvgDeduction && line.AmountChf == -280m);
    }

    [Fact]
    public void DeriveForEmployee_GavMandatoryDepartmentSuppressesTrainingAndHolidayDeduction()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var workSummary = new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            workSummary,
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Sicherheit", true));

        Assert.DoesNotContain(result.Lines, line => line.Code == "AUSBILDUNG_FERIEN");
    }

    [Fact]
    public void DeriveForEmployee_NonGavDepartmentKeepsTrainingAndHolidayDeduction()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var workSummary = new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            workSummary,
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        Assert.Contains(result.Lines, line => line.Code == "AUSBILDUNG_FERIEN" && line.AmountChf == -0.16596m);
    }

    [Fact]
    public void DeriveForEmployee_TrainingAndHolidayDeduction_UsesOnlyEffectiveSupplementHours()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var timeEntries = new[]
        {
            new TimeEntry(employeeId, new DateOnly(2026, 3, 5), 10m, 10m, 0m, 0m)
        };
        var workSummary = PayrollWorkSummary.FromTimeEntries(employeeId, timeEntries);
        var payrollSettings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.10m, null, null),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m);

        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            payrollSettings,
            workSummary,
            [],
            timeEntries,
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        var trainingLine = Assert.Single(result.Lines, line => line.Code == "AUSBILDUNG_FERIEN");
        Assert.Equal(-0.182556m, trainingLine.AmountChf);
        Assert.NotEqual(-0.33192m, trainingLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_TrainingAndHolidayDeduction_MatchesSpecifiedExample()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var workSummary = new PayrollWorkSummary(employeeId, 20m, 10m, 0m, 0m);
        var payrollSettings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(1m, null, null),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.0833m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            payrollSettings,
            workSummary,
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        var trainingLine = Assert.Single(result.Lines, line => line.Code == "AUSBILDUNG_FERIEN");
        Assert.Equal(-0.487485m, trainingLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_WithholdingTaxSubjectEmployeeCreatesTaxLineFromAhvGross()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false, true, "B", 5m));

        var taxLine = Assert.Single(result.Lines, line => line.Code == "WITHHOLDING_TAX");
        Assert.Equal(PayrollLineType.Tax, taxLine.LineType);
        Assert.Equal(331.92m, taxLine.Quantity);
        Assert.Equal(5m, taxLine.RateChf);
        Assert.Equal(-16.596m, taxLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_NonWithholdingTaxSubjectEmployeeCreatesNoTaxLine()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false, false, "B", 5m, -20m, "Rueckzahlung"));

        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.Tax);
    }

    [Fact]
    public void DeriveForEmployee_WithholdingTaxCorrectionCreatesSeparateSignedTaxLine()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false, true, "B", 0m, 25m, "Rueckzahlung"));

        Assert.Contains(result.Lines, line => line.Code == "WITHHOLDING_TAX_CORRECTION" && line.AmountChf == 25m);
    }

    [Fact]
    public void DeriveForEmployee_AddsSalaryAdvanceSettlementWithoutChangingDeductionBasis()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m);
        var advance = new global::Payroll.Domain.MonthlyRecords.SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 3, 250m, "Vorschuss");
        advance.SaveSettlement(null, Guid.NewGuid(), 2026, 4, 50m, "Teilrueckzahlung");
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 4, 30),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            [advance],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.SalaryAdvanceSettlement && line.AmountChf == -50m);
        var ahvLine = Assert.Single(result.Lines, line => line.Code == "AHV_IV_EO");
        Assert.Equal(-5.86392m, ahvLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_CreatesSeparateSalaryAdvancePayoutLinesForCurrentMonth()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m);
        var firstAdvance = new global::Payroll.Domain.MonthlyRecords.SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 4, 120m, "Laptop");
        var secondAdvance = new global::Payroll.Domain.MonthlyRecords.SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 4, 80m, "Werkzeug");
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 4, 30),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            [firstAdvance, secondAdvance],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        var payoutLines = result.Lines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvancePayout)
            .ToArray();

        Assert.Equal(2, payoutLines.Length);
        Assert.Contains(payoutLines, line => line.AmountChf == 120m && line.Description.Contains("04/2026", StringComparison.Ordinal));
        Assert.Contains(payoutLines, line => line.AmountChf == 80m && line.Description.Contains("Werkzeug", StringComparison.Ordinal));
        var ahvLine = Assert.Single(result.Lines, line => line.Code == "AHV_IV_EO");
        Assert.Equal(-5.86392m, ahvLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_CreatesSeparateSalaryAdvanceSettlementLinesForCurrentMonth()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m);
        var firstAdvance = new global::Payroll.Domain.MonthlyRecords.SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 3, 250m, "Laptop");
        firstAdvance.SaveSettlement(null, Guid.NewGuid(), 2026, 4, 50m, "April 1");
        var secondAdvance = new global::Payroll.Domain.MonthlyRecords.SalaryAdvance(Guid.NewGuid(), employeeId, 2026, 3, 180m, "Werkzeug");
        secondAdvance.SaveSettlement(null, Guid.NewGuid(), 2026, 4, 70m, "April 2");
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 4, 30),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            [firstAdvance, secondAdvance],
            new PayrollDerivationContext(EmployeeWageType.Hourly, "Buero", false));

        var settlementLines = result.Lines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvanceSettlement)
            .ToArray();

        Assert.Equal(2, settlementLines.Length);
        Assert.Contains(settlementLines, line => line.AmountChf == -50m && line.Description.Contains("April 1", StringComparison.Ordinal));
        Assert.Contains(settlementLines, line => line.AmountChf == -70m && line.Description.Contains("April 2", StringComparison.Ordinal));
        var ahvLine = Assert.Single(result.Lines, line => line.Code == "AHV_IV_EO");
        Assert.Equal(-5.86392m, ahvLine.AmountChf);
    }

    [Fact]
    public void DeriveForEmployee_MonthlyWageTypeUsesSeparatePath()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m, EmployeeWageType.Monthly, 5000m);
        var workSummary = new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            workSummary,
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Monthly, "Buero", false));

        Assert.Empty(result.Issues);
        Assert.Contains(result.Lines, line => line.LineType == PayrollLineType.MonthlySalary && line.AmountChf == 5000m);
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.BaseHours);
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.VacationCompensation);
        Assert.Equal(-361.05m, result.Lines.Where(line => line.LineType == PayrollLineType.SocialContribution).Sum(line => line.AmountChf));
    }

    [Fact]
    public void DeriveForEmployee_MonthlyWageTypeReturnsIssueWhenMonthlyAmountIsMissing()
    {
        var employeeId = Guid.NewGuid();
        var contract = new EmploymentContract(employeeId, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m, EmployeeWageType.Monthly);
        var service = new PayrollRunLineDerivationService();

        var result = service.DeriveForEmployee(
            new DateOnly(2026, 3, 31),
            null,
            contract,
            CreatePayrollSettings(),
            new PayrollWorkSummary(employeeId, 10m, 0m, 0m, 0m),
            [],
            [],
            new PayrollDerivationContext(EmployeeWageType.Monthly, "Buero", false));

        Assert.Contains(result.Issues, issue => issue.Code == "MONTHLY_SALARY_AMOUNT_MISSING");
        Assert.DoesNotContain(result.Lines, line => line.LineType == PayrollLineType.MonthlySalary);
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
        Assert.Contains(result.Lines, line => line.Code == "AUSBILDUNG_FERIEN" && line.AmountChf == -0.16896m);
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

    private static PayrollSettings CreatePayrollSettings()
    {
        return new PayrollSettings(
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
    }
}
