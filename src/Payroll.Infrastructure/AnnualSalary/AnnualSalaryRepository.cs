using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.AnnualSalary;
using Payroll.Domain.Payroll;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.AnnualSalary;

public sealed class AnnualSalaryRepository : IAnnualSalaryRepository
{
    private readonly PayrollDbContext _dbContext;

    public AnnualSalaryRepository(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AnnualSalaryOverviewDto> GetOverviewAsync(
        AnnualSalaryOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleAsync(item => item.Id == query.EmployeeId, cancellationToken);

        var payrollRuns = await _dbContext.PayrollRuns
            .AsNoTracking()
            .Where(run => run.Status == PayrollRunStatus.Finalized
                && run.PeriodKey.CompareTo($"{query.Year:D4}-01") >= 0
                && run.PeriodKey.CompareTo($"{query.Year:D4}-12") <= 0)
            .Include(run => run.Lines)
            .ToListAsync(cancellationToken);

        var rows = new List<AnnualSalaryMonthDto>();
        for (var month = 1; month <= 12; month++)
        {
            var periodKey = $"{query.Year:D4}-{month:D2}";
            var lines = payrollRuns
                .Where(run => run.PeriodKey == periodKey)
                .SelectMany(run => run.Lines)
                .Where(line => line.EmployeeId == query.EmployeeId)
                .ToArray();

            rows.Add(lines.Length == 0
                ? CreateEmptyMonth(query.Year, month)
                : CreateMonth(query.Year, month, lines));
        }

        var totals = new AnnualSalaryTotalsDto(
            rows.Sum(row => row.GrossSalaryChf),
            rows.Sum(row => row.AhvIvEoDeductionChf),
            rows.Sum(row => row.AlvDeductionChf),
            rows.Sum(row => row.SicknessDailyAllowanceDeductionChf),
            rows.Sum(row => row.TrainingAndEducationDeductionChf),
            rows.Sum(row => row.TotalSocialDeductionChf),
            rows.Sum(row => row.BvgDeductionChf),
            rows.Sum(row => row.WithholdingTaxChf),
            rows.Sum(row => row.ExpensesChf),
            rows.Sum(row => row.NetSalaryChf));

        return new AnnualSalaryOverviewDto(
            employee.Id,
            employee.PersonnelNumber,
            employee.FirstName,
            employee.LastName,
            query.Year,
            rows,
            totals);
    }

    private static AnnualSalaryMonthDto CreateMonth(
        int year,
        int month,
        IReadOnlyCollection<PayrollRunLine> lines)
    {
        var grossSalaryChf = lines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VacationCompensation
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);
        var ahvIvEoDeductionChf = AbsSum(lines, "AHV_IV_EO");
        var alvDeductionChf = AbsSum(lines, "ALV");
        var sicknessDailyAllowanceDeductionChf = AbsSum(lines, "KTG_UVG");
        var trainingAndEducationDeductionChf = AbsSum(lines, "AUSBILDUNG_FERIEN");
        var totalSocialDeductionChf = ahvIvEoDeductionChf
            + alvDeductionChf
            + sicknessDailyAllowanceDeductionChf
            + trainingAndEducationDeductionChf;
        var bvgDeductionChf = Math.Abs(lines
            .Where(line => line.LineType == PayrollLineType.BvgDeduction)
            .Sum(line => line.AmountChf));
        var withholdingTaxChf = Math.Abs(lines
            .Where(line => line.LineType == PayrollLineType.Tax)
            .Sum(line => line.AmountChf));
        var expensesChf = lines
            .Where(line => line.LineType == PayrollLineType.Expense)
            .Sum(line => line.AmountChf);
        var netSalaryChf = lines
            .Where(line => line.LineType != PayrollLineType.Expense)
            .Sum(line => line.AmountChf);

        return new AnnualSalaryMonthDto(
            month,
            CreateMonthLabel(year, month),
            true,
            grossSalaryChf,
            ahvIvEoDeductionChf,
            alvDeductionChf,
            sicknessDailyAllowanceDeductionChf,
            trainingAndEducationDeductionChf,
            totalSocialDeductionChf,
            bvgDeductionChf,
            withholdingTaxChf,
            expensesChf,
            netSalaryChf);
    }

    private static AnnualSalaryMonthDto CreateEmptyMonth(
        int year,
        int month)
    {
        return new AnnualSalaryMonthDto(
            month,
            CreateMonthLabel(year, month),
            false,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m);
    }

    private static decimal AbsSum(IEnumerable<PayrollRunLine> lines, params string[] codes)
    {
        return Math.Abs(lines
            .Where(line => codes.Contains(line.Code, StringComparer.OrdinalIgnoreCase))
            .Sum(line => line.AmountChf));
    }

    private static string CreateMonthLabel(int year, int month)
    {
        return new DateTime(year, month, 1).ToString("MMM", CultureInfo.CurrentCulture);
    }
}
