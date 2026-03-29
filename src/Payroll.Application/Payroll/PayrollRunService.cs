using Microsoft.EntityFrameworkCore;
using Payroll.Application.Abstractions;
using Payroll.Domain.Payroll;

namespace Payroll.Application.Payroll;

public sealed class PayrollRunService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPayrollCalculator _payrollCalculator;

    public PayrollRunService(IAppDbContext dbContext, IPayrollCalculator payrollCalculator)
    {
        _dbContext = dbContext;
        _payrollCalculator = payrollCalculator;
    }

    public async Task<PayrollRun> CreateMonthlyPayrollAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var payrollRun = new PayrollRun(year, month, DateOnly.FromDateTime(DateTime.Today));

        var employees = await _dbContext.Employees
            .Where(employee => employee.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            var hours = await _dbContext.ImportedWorkTimes
                .Where(item => item.EmployeeId == employee.Id && item.WorkDate.Year == year && item.WorkDate.Month == month)
                .SumAsync(item => (decimal?)item.Hours, cancellationToken) ?? 0m;

            var expenses = await _dbContext.ExpenseClaims
                .Where(item => item.EmployeeId == employee.Id && item.ExpenseDate.Year == year && item.ExpenseDate.Month == month)
                .SumAsync(item => (decimal?)item.Amount, cancellationToken) ?? 0m;

            var result = _payrollCalculator.Calculate(new PayrollCalculationInput(
                employee,
                hours,
                expenses,
                AhvRate: 0.053m,
                AlvRate: 0.011m));

            payrollRun.Entries.Add(new PayrollEntry(
                payrollRun.Id,
                employee.Id,
                result.GrossSalary,
                result.AhvDeduction,
                result.AlvDeduction,
                result.ExpenseReimbursement));
        }

        payrollRun.MarkCalculated();
        _dbContext.PayrollRuns.Add(payrollRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return payrollRun;
    }
}
