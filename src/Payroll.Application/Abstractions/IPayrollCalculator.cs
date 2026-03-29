using Payroll.Application.Payroll;

namespace Payroll.Application.Abstractions;

public interface IPayrollCalculator
{
    PayrollCalculationResult Calculate(PayrollCalculationInput input);
}
