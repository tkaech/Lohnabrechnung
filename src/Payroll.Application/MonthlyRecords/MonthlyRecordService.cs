using Payroll.Domain.MonthlyRecords;

namespace Payroll.Application.MonthlyRecords;

public sealed class MonthlyRecordService
{
    private readonly IEmployeeMonthlyRecordRepository _repository;

    public MonthlyRecordService(IEmployeeMonthlyRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<MonthlyRecordDetailsDto> GetOrCreateAsync(MonthlyRecordQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _repository.ClearTracking();
        var monthlyRecord = await _repository.GetOrCreateAsync(query.EmployeeId, query.Year, query.Month, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _repository.ClearTracking();
        return await LoadDetailsAsync(monthlyRecord.Id, cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto> SaveTimeEntryAsync(
        SaveMonthlyTimeEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(command.MonthlyRecordId, cancellationToken);
        var isNewEntry = !command.TimeEntryId.HasValue
            && monthlyRecord.TimeEntries.All(item => item.WorkDate != command.WorkDate);

        var timeEntry = monthlyRecord.SaveTimeEntry(
            command.TimeEntryId,
            command.WorkDate,
            command.HoursWorked,
            command.NightHours,
            command.SundayHours,
            command.HolidayHours,
            command.Note);

        if (isNewEntry)
        {
            _repository.MarkAsAdded(timeEntry);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _repository.ClearTracking();
        return await LoadDetailsAsync(monthlyRecord.Id, cancellationToken);
    }

    public async Task DeleteTimeEntryAsync(Guid monthlyRecordId, Guid timeEntryId, CancellationToken cancellationToken = default)
    {
        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(monthlyRecordId, cancellationToken);
        monthlyRecord.RemoveTimeEntry(timeEntryId);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto> SaveExpenseEntryAsync(
        SaveMonthlyExpenseEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(command.MonthlyRecordId, cancellationToken);
        var expenseEntry = monthlyRecord.SaveExpenseEntry(
            command.ExpenseEntryId,
            command.ExpenseDate,
            command.AmountChf);

        if (!command.ExpenseEntryId.HasValue)
        {
            _repository.MarkAsAdded(expenseEntry);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _repository.ClearTracking();
        return await LoadDetailsAsync(monthlyRecord.Id, cancellationToken);
    }

    public async Task DeleteExpenseEntryAsync(Guid monthlyRecordId, Guid expenseEntryId, CancellationToken cancellationToken = default)
    {
        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(monthlyRecordId, cancellationToken);
        monthlyRecord.RemoveExpenseEntry(expenseEntryId);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<MonthlyRecordDetailsDto> SaveVehicleCompensationAsync(
        SaveMonthlyVehicleCompensationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(command.MonthlyRecordId, cancellationToken);
        var vehicleCompensation = monthlyRecord.SaveVehicleCompensation(
            command.VehicleCompensationId,
            command.CompensationDate,
            command.AmountChf,
            command.Description);

        if (!command.VehicleCompensationId.HasValue)
        {
            _repository.MarkAsAdded(vehicleCompensation);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _repository.ClearTracking();
        return await LoadDetailsAsync(monthlyRecord.Id, cancellationToken);
    }

    public async Task DeleteVehicleCompensationAsync(Guid monthlyRecordId, Guid vehicleCompensationId, CancellationToken cancellationToken = default)
    {
        _repository.ClearTracking();
        var monthlyRecord = await LoadAggregateAsync(monthlyRecordId, cancellationToken);
        monthlyRecord.RemoveVehicleCompensation(vehicleCompensationId);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<EmployeeMonthlyRecord> LoadAggregateAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(monthlyRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Monthly record was not found.");
    }

    private async Task<MonthlyRecordDetailsDto> LoadDetailsAsync(Guid monthlyRecordId, CancellationToken cancellationToken)
    {
        return await _repository.GetDetailsAsync(monthlyRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Monthly record details could not be loaded.");
    }
}
