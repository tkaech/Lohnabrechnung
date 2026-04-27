using Payroll.Domain.Payroll;

namespace Payroll.Application.Payroll;

public sealed class PayrollRunService
{
    private readonly IPayrollRunRepository _repository;
    private readonly PayrollRunLineDerivationService _derivationService = new();

    public PayrollRunService(IPayrollRunRepository repository)
    {
        _repository = repository;
    }

    public async Task<PayrollRunFinalizedDto> FinalizeMonthAsync(
        FinalizePayrollMonthCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(command));
        }

        _ = new DateOnly(command.Year, command.Month, 1);

        var periodKey = CreatePeriodKey(command.Year, command.Month);
        if (await _repository.GetFinalizedRunForEmployeePeriodAsync(command.EmployeeId, periodKey, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Payroll month is already finalized.");
        }

        var payrollSettingsForPeriod = await _repository.LoadPayrollSettingsForPeriodAsync(command.Year, command.Month, cancellationToken);
        var input = await _repository.LoadMonthlyInputAsync(command.EmployeeId, command.Year, command.Month, cancellationToken);
        if (input is null)
        {
            throw new InvalidOperationException("No monthly record found for payroll finalization.");
        }

        var payrollRun = new PayrollRun(periodKey, command.PaymentDate);
        if (input.Contract is null)
        {
            throw new InvalidOperationException("No valid contract found for payroll finalization.");
        }

        var monthlyRecord = input.MonthlyRecord;
        var contract = input.Contract;

        var expenses = monthlyRecord.ExpenseEntry is null
            ? Array.Empty<Domain.Expenses.ExpenseEntry>()
            : [monthlyRecord.ExpenseEntry];

        var result = _derivationService.DeriveForEmployee(
            monthlyRecord.PeriodEnd,
            input.EmployeeBirthDate,
            contract,
            payrollSettingsForPeriod,
            PayrollWorkSummary.FromTimeEntries(monthlyRecord.EmployeeId, monthlyRecord.TimeEntries),
            expenses,
            monthlyRecord.TimeEntries.ToArray(),
            new PayrollDerivationContext(contract.WageType, input.DepartmentName, input.IsDepartmentGavMandatory));

        if (result.Issues.Count > 0)
        {
            throw new InvalidOperationException("Payroll month cannot be finalized while derivation issues exist.");
        }

        if (result.Lines.Count == 0)
        {
            throw new InvalidOperationException("No payable payroll lines could be finalized.");
        }

        payrollRun.AddLines(result.Lines);
        payrollRun.FinalizeRun();
        _repository.Add(payrollRun);
        await _repository.SaveChangesAsync(cancellationToken);

        return new PayrollRunFinalizedDto(
            payrollRun.Id,
            payrollRun.PeriodKey,
            1,
            payrollRun.Lines.Count,
            payrollRun.GetTotalAmountChf());
    }

    public async Task<PayrollRunMonthlyStatusDto> GetMonthlyStatusAsync(
        PayrollRunMonthlyStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(query));
        }

        var periodKey = CreatePeriodKey(query.Year, query.Month);
        var finalizedRun = await _repository.GetFinalizedRunForEmployeePeriodAsync(query.EmployeeId, periodKey, cancellationToken);
        var latestRun = finalizedRun
            ?? await _repository.GetLatestRunForEmployeePeriodAsync(query.EmployeeId, periodKey, cancellationToken);
        var hasCancelledRun = finalizedRun is null
            && latestRun?.Status == PayrollRunStatus.Cancelled;

        return new PayrollRunMonthlyStatusDto(
            query.EmployeeId,
            query.Year,
            query.Month,
            finalizedRun is not null,
            latestRun?.PaymentDate,
            hasCancelledRun);
    }

    public async Task<bool> UpdatePaymentDateAsync(
        UpdatePayrollRunPaymentDateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(command));
        }

        _ = new DateOnly(command.Year, command.Month, 1);

        var periodKey = CreatePeriodKey(command.Year, command.Month);
        var payrollRun = await _repository.GetFinalizedRunForEmployeePeriodForUpdateAsync(
            command.EmployeeId,
            periodKey,
            cancellationToken);
        if (payrollRun is null)
        {
            return false;
        }

        payrollRun.UpdatePaymentDate(command.PaymentDate);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task CancelMonthAsync(
        CancelPayrollRunCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee is required.", nameof(command));
        }

        _ = new DateOnly(command.Year, command.Month, 1);

        var periodKey = CreatePeriodKey(command.Year, command.Month);
        var payrollRun = await _repository.GetFinalizedRunForEmployeePeriodForUpdateAsync(
            command.EmployeeId,
            periodKey,
            cancellationToken);

        if (payrollRun is null)
        {
            if (await _repository.HasCancelledRunForEmployeePeriodAsync(command.EmployeeId, periodKey, cancellationToken))
            {
                throw new InvalidOperationException("Payroll month is already cancelled.");
            }

            throw new InvalidOperationException("No finalized payroll month found for cancellation.");
        }

        payrollRun.Cancel(DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public static string CreatePeriodKey(int year, int month)
    {
        _ = new DateOnly(year, month, 1);
        return $"{year:D4}-{month:D2}";
    }
}
