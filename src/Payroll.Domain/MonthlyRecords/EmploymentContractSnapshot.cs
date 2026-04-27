using Payroll.Domain.Employees;

namespace Payroll.Domain.MonthlyRecords;

public sealed class EmploymentContractSnapshot
{
    internal EmploymentContractSnapshot()
    {
        IsInitialized = false;
        CapturedAtUtc = DateTimeOffset.MinValue;
        ValidFrom = DateOnly.MinValue;
    }

    private EmploymentContractSnapshot(
        DateTimeOffset capturedAtUtc,
        DateOnly validFrom,
        DateOnly? validTo,
        decimal hourlyRateChf,
        decimal monthlySalaryAmountChf,
        decimal monthlyBvgDeductionChf,
        decimal specialSupplementRateChf,
        EmployeeWageType wageType)
    {
        IsInitialized = true;
        CapturedAtUtc = capturedAtUtc;
        ValidFrom = validFrom;
        ValidTo = validTo;
        HourlyRateChf = hourlyRateChf;
        MonthlySalaryAmountChf = monthlySalaryAmountChf;
        MonthlyBvgDeductionChf = monthlyBvgDeductionChf;
        SpecialSupplementRateChf = specialSupplementRateChf;
        WageType = wageType;
    }

    public bool IsInitialized { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal HourlyRateChf { get; private set; }
    public decimal MonthlySalaryAmountChf { get; private set; }
    public decimal MonthlyBvgDeductionChf { get; private set; }
    public decimal SpecialSupplementRateChf { get; private set; }
    public EmployeeWageType WageType { get; private set; }

    public static EmploymentContractSnapshot Create(EmploymentContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new EmploymentContractSnapshot(
            DateTimeOffset.UtcNow,
            contract.ValidFrom,
            contract.ValidTo,
            contract.HourlyRateChf,
            contract.MonthlySalaryAmountChf,
            contract.MonthlyBvgDeductionChf,
            contract.SpecialSupplementRateChf,
            contract.WageType);
    }

    public EmploymentContract ToEmploymentContract(Guid employeeId)
    {
        return new EmploymentContract(
            employeeId,
            ValidFrom,
            ValidTo,
            HourlyRateChf,
            MonthlyBvgDeductionChf,
            SpecialSupplementRateChf,
            WageType,
            MonthlySalaryAmountChf);
    }
}
