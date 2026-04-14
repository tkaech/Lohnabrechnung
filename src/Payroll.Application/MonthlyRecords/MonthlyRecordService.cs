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
        if (!command.OverwriteExistingMonth
            && await _repository.MonthlySnapshotsDifferFromCurrentAsync(monthlyRecord, cancellationToken))
        {
            throw new MonthlyRecordOverwriteRequiredException();
        }

        if (command.OverwriteExistingMonth)
        {
            await _repository.RefreshMonthlySnapshotsAsync(monthlyRecord, cancellationToken);
        }

        var isNewEntry = !command.TimeEntryId.HasValue
            && monthlyRecord.TimeEntries.Count == 0;

        var timeEntry = monthlyRecord.SaveTimeEntry(
            command.TimeEntryId,
            command.WorkDate,
            command.HoursWorked,
            command.NightHours,
            command.SundayHours,
            command.HolidayHours,
            command.VehiclePauschalzone1Chf,
            command.VehiclePauschalzone2Chf,
            command.VehicleRegiezone1Chf,
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
        if (!command.OverwriteExistingMonth
            && await _repository.MonthlySnapshotsDifferFromCurrentAsync(monthlyRecord, cancellationToken))
        {
            throw new MonthlyRecordOverwriteRequiredException();
        }

        if (command.OverwriteExistingMonth)
        {
            await _repository.RefreshMonthlySnapshotsAsync(monthlyRecord, cancellationToken);
        }

        var hadExpenseEntry = monthlyRecord.ExpenseEntry is not null;
        var expenseEntry = monthlyRecord.SaveExpenseEntry(command.ExpensesTotalChf);

        if (!hadExpenseEntry)
        {
            _repository.MarkAsAdded(expenseEntry);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        _repository.ClearTracking();
        return await LoadDetailsAsync(monthlyRecord.Id, cancellationToken);
    }

    public Task<IReadOnlyCollection<MonthlyTimeCaptureOverviewRowDto>> ListTimeCaptureOverviewAsync(
        MonthlyTimeCaptureOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _repository.ClearTracking();
        return _repository.ListTimeCaptureOverviewAsync(query.Year, query.Month, cancellationToken);
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
