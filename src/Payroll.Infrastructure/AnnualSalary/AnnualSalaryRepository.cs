using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.AnnualSalary;
using Payroll.Domain.MonthlyRecords;
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
            .Where(run => run.Lines.Any(line => line.EmployeeId == query.EmployeeId)
                && (run.Status == PayrollRunStatus.Finalized || run.Status == PayrollRunStatus.Cancelled)
                && run.PeriodKey.CompareTo($"{query.Year:D4}-01") >= 0
                && run.PeriodKey.CompareTo($"{query.Year:D4}-12") <= 0)
            .Include(run => run.Lines)
            .ToListAsync(cancellationToken);

        var monthlyRecords = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == query.EmployeeId
                && record.Year == query.Year)
            .Include(record => record.TimeEntries)
            .Include(record => record.SalaryAdvances)
            .Include(record => record.SalaryAdvanceSettlements)
            .Include(record => record.ExpenseEntry)
            .ToListAsync(cancellationToken);

        var rows = new List<AnnualSalaryMonthDto>();
        for (var month = 1; month <= 12; month++)
        {
            var periodKey = $"{query.Year:D4}-{month:D2}";
            var runsForMonth = payrollRuns
                .Where(run => run.PeriodKey == periodKey)
                .ToArray();
            var finalizedLines = runsForMonth
                .Where(run => run.Status == PayrollRunStatus.Finalized)
                .SelectMany(run => run.Lines)
                .Where(line => line.EmployeeId == query.EmployeeId)
                .ToArray();
            var hasCancelledRun = finalizedLines.Length == 0
                && runsForMonth.Any(run => run.Status == PayrollRunStatus.Cancelled
                    && run.Lines.Any(line => line.EmployeeId == query.EmployeeId));
            var monthlyRecord = monthlyRecords.SingleOrDefault(record => record.Month == month);
            var hasRecordedMonthData = HasRecordedMonthData(monthlyRecord);

            rows.Add(finalizedLines.Length == 0
                ? CreateEmptyMonth(query.Year, month, hasCancelledRun, hasRecordedMonthData)
                : CreateMonth(query.Year, month, finalizedLines, hasCancelledRun, hasRecordedMonthData));
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
            employee.AhvNumber,
            employee.BirthDate,
            query.Year,
            rows,
            totals);
    }

    private static AnnualSalaryMonthDto CreateMonth(
        int year,
        int month,
        IReadOnlyCollection<PayrollRunLine> lines,
        bool hasCancelledRun,
        bool hasRecordedMonthData)
    {
        var grossSalaryChf = lines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.MonthlySalary
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
        var salaryAdvancePayoutChf = lines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvancePayout)
            .Sum(line => line.AmountChf);
        var salaryAdvanceSettlementChf = lines
            .Where(line => line.LineType == PayrollLineType.SalaryAdvanceSettlement)
            .Sum(line => line.AmountChf);
        var netSalaryChf = lines
            .Where(line => line.LineType is not PayrollLineType.Expense
                and not PayrollLineType.SalaryAdvancePayout
                and not PayrollLineType.SalaryAdvanceSettlement)
            .Sum(line => line.AmountChf)
            + salaryAdvancePayoutChf
            + salaryAdvanceSettlementChf;

        return new AnnualSalaryMonthDto(
            month,
            CreateMonthLabel(year, month),
            true,
            hasCancelledRun,
            hasRecordedMonthData,
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
        int month,
        bool hasCancelledRun,
        bool hasRecordedMonthData)
    {
        return new AnnualSalaryMonthDto(
            month,
            CreateMonthLabel(year, month),
            false,
            hasCancelledRun,
            hasRecordedMonthData,
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

    private static bool HasRecordedMonthData(EmployeeMonthlyRecord? monthlyRecord)
    {
        return monthlyRecord is not null
            && (monthlyRecord.TimeEntries.Count > 0
                || monthlyRecord.SalaryAdvances.Count > 0
                || monthlyRecord.SalaryAdvanceSettlements.Count > 0
                || monthlyRecord.ExpenseEntry is not null
                || monthlyRecord.WithholdingTaxRatePercent != 0m
                || monthlyRecord.WithholdingTaxCorrectionAmountChf != 0m
                || !string.IsNullOrWhiteSpace(monthlyRecord.WithholdingTaxCorrectionText));
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
