namespace Payroll.Application.BackupRestore;

public interface IBackupRestoreService
{
    string GetDefaultBackupDirectory();
    string CreateDefaultFileName(DateTimeOffset localTimestamp);
    Task<BackupFileInfoDto> CreateBackupAsync(CreateBackupCommand command, CancellationToken cancellationToken = default);
    Task<RestoreResultDto> RestoreBackupAsync(RestoreBackupCommand command, CancellationToken cancellationToken = default);
}
