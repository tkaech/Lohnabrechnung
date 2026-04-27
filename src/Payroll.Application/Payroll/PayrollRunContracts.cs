using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.Payroll;

public sealed record FinalizePayrollMonthCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    DateOnly PaymentDate);

public sealed record CancelPayrollRunCommand(
    Guid EmployeeId,
    int Year,
    int Month);

public sealed record UpdatePayrollRunPaymentDateCommand(
    Guid EmployeeId,
    int Year,
    int Month,
    DateOnly PaymentDate);

public sealed record PayrollRunMonthlyStatusQuery(
    Guid EmployeeId,
    int Year,
    int Month);

public sealed record PayrollRunFinalizedDto(
    Guid PayrollRunId,
    string PeriodKey,
    int EmployeeCount,
    int LineCount,
    decimal TotalAmountChf);

public sealed record PayrollRunMonthlyStatusDto(
    Guid EmployeeId,
    int Year,
    int Month,
    bool IsFinalized,
    DateOnly? PaymentDate,
    bool HasCancelledRun = false)
{
    public string StatusDisplay => IsFinalized ? "abgeschlossen" : HasCancelledRun ? "storniert" : "offen";
}

public sealed record PayrollRunMonthlyInputDto(
    Guid EmployeeId,
    DateOnly? EmployeeBirthDate,
    string? DepartmentName,
    bool IsDepartmentGavMandatory,
    string? TaxStatus,
    bool IsSubjectToWithholdingTax,
    EmployeeMonthlyRecord MonthlyRecord,
    EmploymentContract? Contract);
