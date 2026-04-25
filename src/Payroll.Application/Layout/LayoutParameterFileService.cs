namespace Payroll.Application.Layout;

public sealed class LayoutParameterFileService
{
    private readonly ILayoutParameterFileRepository _repository;

    public LayoutParameterFileService(ILayoutParameterFileRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        return _repository.ListFilesAsync(cancellationToken);
    }

    public Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Dateischluessel ist erforderlich.", nameof(key));
        }

        return _repository.GetFileAsync(key, cancellationToken);
    }

    public Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Key))
        {
            throw new ArgumentException("Dateischluessel ist erforderlich.", nameof(command));
        }

        return _repository.SaveAsync(command, cancellationToken);
    }

    public Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Key))
        {
            throw new ArgumentException("Dateischluessel ist erforderlich.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.BackupId))
        {
            throw new ArgumentException("Backup-Id ist erforderlich.", nameof(command));
        }

        return _repository.RestoreBackupAsync(command, cancellationToken);
    }
}
