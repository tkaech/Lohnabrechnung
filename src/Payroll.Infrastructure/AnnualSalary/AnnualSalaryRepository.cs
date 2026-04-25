using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.AnnualSalary;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.AnnualSalary;

public sealed class AnnualSalaryRepository : IAnnualSalaryRepository
{
    private readonly PayrollDbContext _dbContext;
    private readonly PayrollRunLineDerivationService _payrollRunLineDerivationService = new();

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

        var currentPayrollSettings = await LoadCurrentPayrollSettingsAsync(cancellationToken);
        var records = await _dbContext.EmployeeMonthlyRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == query.EmployeeId && record.Year == query.Year)
            .Include(record => record.TimeEntries)
            .Include(record => record.ExpenseEntry)
            .ToListAsync(cancellationToken);

        var rows = new List<AnnualSalaryMonthDto>();
        for (var month = 1; month <= 12; month++)
        {
            var record = records.SingleOrDefault(item => item.Month == month);
            rows.Add(record is null
                ? CreateEmptyMonth(query.Year, month)
                : await CreateMonthAsync(record, employee.BirthDate, currentPayrollSettings, cancellationToken));
        }

        var totals = new AnnualSalaryTotalsDto(
            rows.Sum(row => row.GrossSalaryChf),
            rows.Sum(row => row.AhvIvEoAlvDeductionChf + row.NbuDeductionChf),
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

    private async Task<AnnualSalaryMonthDto> CreateMonthAsync(
        EmployeeMonthlyRecord record,
        DateOnly? employeeBirthDate,
        PayrollSettings currentPayrollSettings,
        CancellationToken cancellationToken)
    {
        if (record.TimeEntries.Count == 0)
        {
            return CreateEmptyMonth(
                record.Year,
                record.Month,
                hasMonthData: record.ExpenseEntry is not null,
                expensesChf: record.ExpenseEntry?.ExpensesTotalChf ?? 0m);
        }

        var contract = ResolveContractForMonth(
            record,
            await LoadBestAvailableContractForMonthAsync(record.EmployeeId, record.PeriodStart, record.PeriodEnd, cancellationToken));
        if (contract is null)
        {
            return CreateEmptyMonth(record.Year, record.Month, hasMonthData: true, expensesChf: record.ExpenseEntry?.ExpensesTotalChf ?? 0m);
        }

        var payrollSettings = ResolvePayrollSettingsForMonth(record, currentPayrollSettings);
        var workSummary = PayrollWorkSummary.FromTimeEntries(record.EmployeeId, record.TimeEntries);
        var expenses = record.ExpenseEntry is null
            ? Array.Empty<Domain.Expenses.ExpenseEntry>()
            : [record.ExpenseEntry];

        var derivation = _payrollRunLineDerivationService.DeriveForEmployee(
            record.PeriodEnd,
            employeeBirthDate,
            contract,
            payrollSettings,
            workSummary,
            expenses,
            record.TimeEntries.ToArray());

        var grossSalaryChf = derivation.Lines
            .Where(line => line.LineType is PayrollLineType.BaseHours
                or PayrollLineType.NightSupplement
                or PayrollLineType.SundaySupplement
                or PayrollLineType.HolidaySupplement
                or PayrollLineType.SpecialSupplement
                or PayrollLineType.VacationCompensation
                or PayrollLineType.VehicleCompensation)
            .Sum(line => line.AmountChf);
        var ahvIvEoAlvDeductionChf = AbsSum(derivation.Lines, "AHV_IV_EO", "ALV");
        var nbuDeductionChf = AbsSum(derivation.Lines, "KTG_UVG");
        var bvgDeductionChf = Math.Abs(derivation.Lines
            .Where(line => line.LineType == PayrollLineType.BvgDeduction)
            .Sum(line => line.AmountChf));
        var withholdingTaxChf = Math.Abs(derivation.Lines
            .Where(line => line.LineType == PayrollLineType.Tax)
            .Sum(line => line.AmountChf));
        var expensesChf = record.ExpenseEntry?.ExpensesTotalChf ?? 0m;
        var netSalaryChf = derivation.Lines
            .Where(line => line.LineType != PayrollLineType.Expense)
            .Sum(line => line.AmountChf);

        return new AnnualSalaryMonthDto(
            record.Month,
            CreateMonthLabel(record.Year, record.Month),
            true,
            grossSalaryChf,
            ahvIvEoAlvDeductionChf,
            nbuDeductionChf,
            bvgDeductionChf,
            withholdingTaxChf,
            expensesChf,
            netSalaryChf);
    }

    private static AnnualSalaryMonthDto CreateEmptyMonth(
        int year,
        int month,
        bool hasMonthData = false,
        decimal expensesChf = 0m)
    {
        return new AnnualSalaryMonthDto(
            month,
            CreateMonthLabel(year, month),
            hasMonthData,
            0m,
            0m,
            0m,
            0m,
            0m,
            expensesChf,
            0m);
    }

    private async Task<PayrollSettings> LoadCurrentPayrollSettingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollSettings
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PayrollSettings();
    }

    private async Task<Domain.Employees.EmploymentContract?> LoadRelevantContractAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var contracts = await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract =>
                contract.EmployeeId == employeeId
                && contract.ValidFrom <= periodEnd
                && (!contract.ValidTo.HasValue || contract.ValidTo.Value >= periodStart))
            .ToListAsync(cancellationToken);

        return contracts
            .OrderByDescending(contract => contract.ValidFrom)
            .FirstOrDefault();
    }

    private async Task<Domain.Employees.EmploymentContract?> LoadBestAvailableContractForMonthAsync(
        Guid employeeId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        var relevantContract = await LoadRelevantContractAsync(employeeId, periodStart, periodEnd, cancellationToken);
        if (relevantContract is not null)
        {
            return relevantContract;
        }

        return await _dbContext.EmploymentContracts
            .AsNoTracking()
            .Where(contract => contract.EmployeeId == employeeId)
            .OrderByDescending(contract => contract.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static PayrollSettings ResolvePayrollSettingsForMonth(EmployeeMonthlyRecord record, PayrollSettings currentPayrollSettings)
    {
        return record.PayrollParameterSnapshot.IsInitialized
            ? record.PayrollParameterSnapshot.ToPayrollSettings()
            : currentPayrollSettings;
    }

    private static Domain.Employees.EmploymentContract? ResolveContractForMonth(
        EmployeeMonthlyRecord record,
        Domain.Employees.EmploymentContract? currentContract)
    {
        return record.EmploymentContractSnapshot.IsInitialized
            ? record.EmploymentContractSnapshot.ToEmploymentContract(record.EmployeeId)
            : currentContract;
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
