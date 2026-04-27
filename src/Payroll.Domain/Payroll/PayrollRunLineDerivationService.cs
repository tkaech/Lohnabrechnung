using Payroll.Domain.Common;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Settings;
using Payroll.Domain.TimeTracking;

namespace Payroll.Domain.Payroll;

public sealed class PayrollRunLineDerivationService
{
    public PayrollRunLineDerivationResult DeriveForEmployee(
        DateOnly payrollReferenceDate,
        DateOnly? employeeBirthDate,
        EmploymentContract contract,
        PayrollSettings payrollSettings,
        PayrollWorkSummary workSummary,
        IReadOnlyCollection<ExpenseEntry> expenses,
        IReadOnlyCollection<TimeEntry> timeEntries,
        PayrollDerivationContext? derivationContext = null)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(payrollSettings);
        ArgumentNullException.ThrowIfNull(workSummary);
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(timeEntries);

        if (contract.EmployeeId != workSummary.EmployeeId)
        {
            throw new ArgumentException("Contract and work summary must belong to the same employee.", nameof(workSummary));
        }

        ValidateEmployeeIds(expenses, contract.EmployeeId, nameof(expenses));
        ValidateEmployeeIds(timeEntries, contract.EmployeeId, nameof(timeEntries));

        var lines = new List<PayrollRunLine>();
        var issues = new List<PayrollDerivationIssue>();
        var supplementSettings = payrollSettings.WorkTimeSupplementSettings;
        var context = derivationContext ?? new PayrollDerivationContext(contract.WageType, null, false);

        if (context.WageType == EmployeeWageType.Monthly)
        {
            if (contract.MonthlySalaryAmountChf > 0m)
            {
                lines.Add(PayrollRunLine.CreateDirectChfLine(
                    contract.EmployeeId,
                    PayrollLineType.MonthlySalary,
                    "MONTHLY_SALARY",
                    "Monatslohn",
                    contract.MonthlySalaryAmountChf));
            }
            else
            {
                issues.Add(new PayrollDerivationIssue(
                    "MONTHLY_SALARY_AMOUNT_MISSING",
                    "Monthly salary payroll path was selected, but no monthly salary amount is configured."));
            }
        }

        if (context.WageType == EmployeeWageType.Hourly && workSummary.WorkHours > 0m)
        {
            lines.Add(PayrollRunLine.CreateCalculatedHourlyLine(
                contract.EmployeeId,
                PayrollLineType.BaseHours,
                "BASE",
                "Base hours",
                workSummary.WorkHours,
                contract.HourlyRateChf));
        }

        if (context.WageType == EmployeeWageType.Hourly && workSummary.WorkHours > 0m && contract.SpecialSupplementRateChf > 0m)
        {
            lines.Add(PayrollRunLine.CreateCalculatedHourlyLine(
                contract.EmployeeId,
                PayrollLineType.SpecialSupplement,
                "SPECIAL_CONTRACT",
                "Spezialzuschlag gemaess Vertrag",
                workSummary.WorkHours,
                contract.SpecialSupplementRateChf));
        }

        if (context.WageType == EmployeeWageType.Hourly && workSummary.HasAmbiguousSpecialHourOverlap)
        {
            issues.Add(new PayrollDerivationIssue(
                "AMBIGUOUS_SPECIAL_HOUR_OVERLAP",
                "Special hours exceed total work hours; overlap rules for night, sunday and holiday hours are unresolved."));
        }
        else if (context.WageType == EmployeeWageType.Hourly)
        {
            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.NightSupplement,
                "NIGHT",
                "Night supplement",
                workSummary.NightHours,
                supplementSettings.NightSupplementRate);

            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.SundaySupplement,
                "SUN",
                "Sunday supplement",
                workSummary.SundayHours,
                supplementSettings.SundaySupplementRate);

            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.HolidaySupplement,
                "HOL",
                "Holiday supplement",
                workSummary.HolidayHours,
                supplementSettings.HolidaySupplementRate);
        }

        foreach (var expense in expenses)
        {
            if (expense.ExpensesTotalChf > 0m)
            {
                lines.Add(PayrollRunLine.CreateDirectChfLine(
                    expense.EmployeeId,
                    PayrollLineType.Expense,
                    ExpenseEntry.PayrollCode,
                    ExpenseEntry.DisplayName,
                    expense.ExpensesTotalChf));
            }
        }

        foreach (var timeEntry in timeEntries)
        {
            AddConfiguredVehicleLine(
                lines,
                timeEntry,
                "VEHICLE_P1",
                "Fahrzeugentschaedigung Pauschalzone 1",
                timeEntry.VehiclePauschalzone1Chf,
                payrollSettings.VehiclePauschalzone1RateChf);

            AddConfiguredVehicleLine(
                lines,
                timeEntry,
                "VEHICLE_P2",
                "Fahrzeugentschaedigung Pauschalzone 2",
                timeEntry.VehiclePauschalzone2Chf,
                payrollSettings.VehiclePauschalzone2RateChf);

            AddConfiguredVehicleLine(
                lines,
                timeEntry,
                "VEHICLE_R1",
                "Fahrzeugentschaedigung Regiezone 1",
                timeEntry.VehicleRegiezone1Chf,
                payrollSettings.VehicleRegiezone1RateChf);
        }

        var vacationCompensationBasisChf = lines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);

        var vacationCompensationRate = payrollSettings.GetVacationCompensationRate(employeeBirthDate, payrollReferenceDate);

        if (vacationCompensationBasisChf > 0m && vacationCompensationRate > 0m)
        {
            lines.Add(PayrollRunLine.CreateDirectChfLine(
                contract.EmployeeId,
                PayrollLineType.VacationCompensation,
                "VACATION_COMP",
                "Ferienentschaedigung",
                vacationCompensationBasisChf * vacationCompensationRate));
        }

        var contributableGrossChf = lines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.MonthlySalary
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VacationCompensation
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);

        AddPercentageDeductionLine(lines, contract.EmployeeId, "AHV_IV_EO", "AHV/IV/EO", contributableGrossChf, payrollSettings.AhvIvEoRate);
        AddPercentageDeductionLine(lines, contract.EmployeeId, "ALV", "ALV", contributableGrossChf, payrollSettings.AlvRate);
        AddPercentageDeductionLine(lines, contract.EmployeeId, "KTG_UVG", "Krankentaggeld/UVG", contributableGrossChf, payrollSettings.SicknessAccidentInsuranceRate);
        if (!context.IsDepartmentGavMandatory)
        {
            AddPercentageDeductionLine(lines, contract.EmployeeId, "AUSBILDUNG_FERIEN", "Aus- und Weiterbildung inkl. Ferien", contributableGrossChf, payrollSettings.TrainingAndHolidayRate);
        }

        if (context.IsSubjectToWithholdingTax)
        {
            if (context.WithholdingTaxRatePercent > 0m && contributableGrossChf > 0m)
            {
                lines.Add(PayrollRunLine.CreateCalculatedPercentageDeduction(
                    contract.EmployeeId,
                    PayrollLineType.Tax,
                    "WITHHOLDING_TAX",
                    BuildWithholdingTaxDescription(context.WithholdingTaxStatus),
                    contributableGrossChf,
                    context.WithholdingTaxRatePercent));
            }

            if (context.WithholdingTaxCorrectionAmountChf != 0m)
            {
                lines.Add(PayrollRunLine.CreateManualChfLine(
                    contract.EmployeeId,
                    PayrollLineType.Tax,
                    "WITHHOLDING_TAX_CORRECTION",
                    BuildWithholdingTaxCorrectionDescription(context.WithholdingTaxCorrectionText),
                    context.WithholdingTaxCorrectionAmountChf));
            }
        }

        if (contract.IsActiveOn(payrollReferenceDate) && contract.MonthlyBvgDeductionChf > 0m)
        {
            lines.Add(PayrollRunLine.CreateCalculatedFixedDeduction(
                contract.EmployeeId,
                PayrollLineType.BvgDeduction,
                "BVG",
                "BVG deduction",
                contract.MonthlyBvgDeductionChf));
        }

        return new PayrollRunLineDerivationResult(lines, issues);
    }

    private static void AddConfiguredSupplementLine(
        ICollection<PayrollRunLine> lines,
        ICollection<PayrollDerivationIssue> issues,
        EmploymentContract contract,
        PayrollLineType lineType,
        string code,
        string description,
        decimal hours,
        decimal? supplementRate)
    {
        if (hours <= 0m)
        {
            return;
        }

        if (!supplementRate.HasValue)
        {
            issues.Add(new PayrollDerivationIssue(
                $"MISSING_{code}_RULE",
                $"Missing supplement rule for {description.ToLowerInvariant()}."));
            return;
        }

        lines.Add(PayrollRunLine.CreateCalculatedHourlyLine(
            contract.EmployeeId,
            lineType,
            code,
            description,
            hours,
            contract.HourlyRateChf * supplementRate.Value));
    }

    private static void AddConfiguredVehicleLine(
        ICollection<PayrollRunLine> lines,
        TimeEntry timeEntry,
        string code,
        string description,
        decimal quantity,
        decimal rateChf)
    {
        if (quantity <= 0m || rateChf <= 0m)
        {
            return;
        }

        lines.Add(PayrollRunLine.CreateDirectChfLine(
            timeEntry.EmployeeId,
            PayrollLineType.VehicleCompensation,
            code,
            description,
            quantity * rateChf));
    }

    private static void AddPercentageDeductionLine(
        ICollection<PayrollRunLine> lines,
        Guid employeeId,
        string code,
        string description,
        decimal contributableGrossChf,
        decimal rate)
    {
        if (contributableGrossChf <= 0m || rate <= 0m)
        {
            return;
        }

        lines.Add(PayrollRunLine.CreateCalculatedFixedDeduction(
            employeeId,
            PayrollLineType.SocialContribution,
            code,
            description,
            contributableGrossChf * rate));
    }

    private static string BuildWithholdingTaxDescription(string? taxStatus)
    {
        return string.IsNullOrWhiteSpace(taxStatus)
            ? "Quellensteuer vom AHV-pflichtigen Bruttolohn"
            : $"Quellensteuer Tarif {taxStatus.Trim()} vom AHV-pflichtigen Bruttolohn";
    }

    private static string BuildWithholdingTaxCorrectionDescription(string? correctionText)
    {
        return string.IsNullOrWhiteSpace(correctionText)
            ? "Quellensteuer Korrektur / Rueckzahlung"
            : $"Quellensteuer Korrektur / Rueckzahlung: {correctionText.Trim()}";
    }

    private static void ValidateEmployeeIds<TEntry>(IEnumerable<TEntry> entries, Guid employeeId, string paramName)
        where TEntry : class
    {
        foreach (var entry in entries)
        {
            var currentEmployeeId = entry switch
            {
                ExpenseEntry expenseEntry => expenseEntry.EmployeeId,
                TimeEntry timeEntry => timeEntry.EmployeeId,
                _ => throw new ArgumentException("Unsupported entry type.", paramName)
            };

            if (currentEmployeeId != employeeId)
            {
                throw new ArgumentException("All entries must belong to the same employee.", paramName);
            }
        }
    }
}
