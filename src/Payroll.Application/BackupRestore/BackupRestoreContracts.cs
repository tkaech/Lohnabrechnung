namespace Payroll.Application.BackupRestore;

public enum BackupContentType
{
    Configuration,
    UserData,
    Both
}

public sealed record CreateBackupCommand(
    string TargetDirectoryPath,
    string FileName,
    BackupContentType ContentType);

public sealed record RestoreBackupCommand(
    string BackupFilePath,
    BackupContentType ContentType);

public sealed record BackupFileInfoDto(
    string FilePath,
    BackupContentType ContentType,
    DateTimeOffset CreatedAtUtc);

public sealed record RestoreResultDto(
    string BackupFilePath,
    BackupContentType RestoredContentType,
    DateTimeOffset RestoredAtUtc);
