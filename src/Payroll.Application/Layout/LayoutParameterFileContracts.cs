namespace Payroll.Application.Layout;

public sealed record LayoutParameterFileSummaryDto(
    string Key,
    string DisplayName,
    string RelativePath,
    DateTimeOffset LastModifiedAt);

public sealed record LayoutParameterBackupDto(
    string BackupId,
    DateTimeOffset CreatedAt,
    string Label);

public sealed record LayoutParameterFileDocumentDto(
    string Key,
    string DisplayName,
    string RelativePath,
    string Content,
    IReadOnlyCollection<LayoutParameterBackupDto> AvailableBackups);

public sealed record SaveLayoutParameterFileCommand(
    string Key,
    string Content);

public sealed record RestoreLayoutParameterFileBackupCommand(
    string Key,
    string BackupId);
