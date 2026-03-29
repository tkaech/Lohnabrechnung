using Payroll.Application.Abstractions;
using Payroll.Application.Payroll;
using Payroll.Domain.Employees;

namespace Payroll.Infrastructure.Payroll;

public sealed class SwissPayrollCalculator : IPayrollCalculator
{
    public PayrollCalculationResult Calculate(PayrollCalculationInput input)
    {
        var grossSalary = input.Employee.EmploymentType == EmploymentType.MonthlySalary
            ? input.Employee.MonthlySalary
            : input.WorkedHours * input.Employee.HourlyRate;

        var ahvDeduction = Math.Round(grossSalary * input.AhvRate, 2, MidpointRounding.AwayFromZero);
        var alvDeduction = Math.Round(grossSalary * input.AlvRate, 2, MidpointRounding.AwayFromZero);
        var netSalary = grossSalary - ahvDeduction - alvDeduction + input.ExpenseTotal;

        return new PayrollCalculationResult(
            grossSalary,
            ahvDeduction,
            alvDeduction,
            input.ExpenseTotal,
            netSalary);
    }
}
