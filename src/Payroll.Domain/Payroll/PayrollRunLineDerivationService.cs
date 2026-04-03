using Payroll.Domain.Common;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;

namespace Payroll.Domain.Payroll;

public sealed class PayrollRunLineDerivationService
{
    public PayrollRunLineDerivationResult DeriveForEmployee(
        DateOnly payrollReferenceDate,
        EmploymentContract contract,
        PayrollWorkSummary workSummary,
        IReadOnlyCollection<ExpenseEntry> expenses,
        IReadOnlyCollection<VehicleCompensation> vehicleCompensations)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(workSummary);
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(vehicleCompensations);

        if (contract.EmployeeId != workSummary.EmployeeId)
        {
            throw new ArgumentException("Contract and work summary must belong to the same employee.", nameof(workSummary));
        }

        ValidateEmployeeIds(expenses, contract.EmployeeId, nameof(expenses));
        ValidateEmployeeIds(vehicleCompensations, contract.EmployeeId, nameof(vehicleCompensations));

        var lines = new List<PayrollRunLine>();
        var issues = new List<PayrollDerivationIssue>();

        if (workSummary.WorkHours > 0m)
        {
            lines.Add(PayrollRunLine.CreateCalculatedHourlyLine(
                contract.EmployeeId,
                PayrollLineType.BaseHours,
                "BASE",
                "Base hours",
                workSummary.WorkHours,
                contract.HourlyRateChf));
        }

        if (workSummary.HasAmbiguousSpecialHourOverlap)
        {
            issues.Add(new PayrollDerivationIssue(
                "AMBIGUOUS_SPECIAL_HOUR_OVERLAP",
                "Special hours exceed total work hours; overlap rules for night, sunday and holiday hours are unresolved."));
        }
        else
        {
            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.NightSupplement,
                "NIGHT",
                "Night supplement",
                workSummary.NightHours,
                contract.SupplementSettings.NightSupplementRate);

            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.SundaySupplement,
                "SUN",
                "Sunday supplement",
                workSummary.SundayHours,
                contract.SupplementSettings.SundaySupplementRate);

            AddConfiguredSupplementLine(
                lines,
                issues,
                contract,
                PayrollLineType.HolidaySupplement,
                "HOL",
                "Holiday supplement",
                workSummary.HolidayHours,
                contract.SupplementSettings.HolidaySupplementRate);
        }

        foreach (var expense in expenses)
        {
            lines.Add(PayrollRunLine.CreateDirectChfLine(
                expense.EmployeeId,
                PayrollLineType.Expense,
                expense.ExpenseTypeCode,
                expense.Description,
                expense.AmountChf));
        }

        foreach (var vehicleCompensation in vehicleCompensations)
        {
            lines.Add(PayrollRunLine.CreateDirectChfLine(
                vehicleCompensation.EmployeeId,
                PayrollLineType.VehicleCompensation,
                "VEHICLE",
                vehicleCompensation.Description,
                vehicleCompensation.AmountChf));
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

    private static void ValidateEmployeeIds<TEntry>(IEnumerable<TEntry> entries, Guid employeeId, string paramName)
        where TEntry : class
    {
        foreach (var entry in entries)
        {
            var currentEmployeeId = entry switch
            {
                ExpenseEntry expenseEntry => expenseEntry.EmployeeId,
                VehicleCompensation vehicleCompensation => vehicleCompensation.EmployeeId,
                _ => throw new ArgumentException("Unsupported entry type.", paramName)
            };

            if (currentEmployeeId != employeeId)
            {
                throw new ArgumentException("All entries must belong to the same employee.", paramName);
            }
        }
    }
}
