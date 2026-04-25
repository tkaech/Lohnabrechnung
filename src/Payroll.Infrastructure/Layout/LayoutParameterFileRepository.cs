using System.Text;
using Payroll.Application.Layout;

namespace Payroll.Infrastructure.Layout;

public sealed class LayoutParameterFileRepository : ILayoutParameterFileRepository
{
    private const int MaxBackupCount = 2;
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private static readonly RegisteredLayoutParameterFile[] RegisteredFiles =
    [
        new("design-system", "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml"),
        new("print-design-system", "Print Design System", "src/Payroll.Desktop/Styles/PrintDesignSystem.axaml")
    ];

    private readonly string _workspaceRootPath;
    private readonly string _backupRootPath;

    public LayoutParameterFileRepository(string workspaceRootPath)
    {
        _workspaceRootPath = GuardPath(workspaceRootPath, nameof(workspaceRootPath));
        _backupRootPath = Path.Combine(_workspaceRootPath, ".layout-parameter-backups");
    }

    public Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<LayoutParameterFileSummaryDto> files = RegisteredFiles
            .Select(CreateSummary)
            .OrderBy(item => item.DisplayName, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(files);
    }

    public Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var file = GetRegisteredFile(key);
        return Task.FromResult(CreateDocument(file));
    }

    public Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var file = GetRegisteredFile(command.Key);
        var originalPath = ResolveOriginalFilePath(file);
        var currentContent = File.ReadAllText(originalPath);

        CreateBackup(file, currentContent);
        TrimBackups(file);
        WriteTextAtomically(originalPath, command.Content);

        return Task.FromResult(CreateDocument(file));
    }

    public Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var file = GetRegisteredFile(command.Key);
        var originalPath = ResolveOriginalFilePath(file);
        var backupPath = ResolveBackupFilePath(file, command.BackupId);
        if (!File.Exists(backupPath))
        {
            throw new InvalidOperationException("Die ausgewaehlte Backup-Version wurde nicht gefunden.");
        }

        var currentContent = File.ReadAllText(originalPath);
        CreateBackup(file, currentContent);

        var restoreContent = File.ReadAllText(backupPath);
        WriteTextAtomically(originalPath, restoreContent);
        TrimBackups(file);

        return Task.FromResult(CreateDocument(file));
    }

    private LayoutParameterFileSummaryDto CreateSummary(RegisteredLayoutParameterFile file)
    {
        var originalPath = ResolveOriginalFilePath(file);
        return new LayoutParameterFileSummaryDto(
            file.Key,
            file.DisplayName,
            file.RelativePath,
            File.GetLastWriteTimeUtc(originalPath));
    }

    private LayoutParameterFileDocumentDto CreateDocument(RegisteredLayoutParameterFile file)
    {
        var originalPath = ResolveOriginalFilePath(file);
        return new LayoutParameterFileDocumentDto(
            file.Key,
            file.DisplayName,
            file.RelativePath,
            File.ReadAllText(originalPath),
            ListBackups(file));
    }

    private IReadOnlyCollection<LayoutParameterBackupDto> ListBackups(RegisteredLayoutParameterFile file)
    {
        var backupDirectoryPath = GetBackupDirectoryPath(file);
        if (!Directory.Exists(backupDirectoryPath))
        {
            return [];
        }

        return Directory.GetFiles(backupDirectoryPath, "*.txt")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => Path.GetFileNameWithoutExtension(info.Name), StringComparer.Ordinal)
            .Select(info =>
            {
                var backupId = Path.GetFileNameWithoutExtension(info.Name);
                var createdAt = ParseBackupCreatedAt(backupId);
                return new LayoutParameterBackupDto(
                    backupId,
                    createdAt,
                    createdAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            })
            .ToArray();
    }

    private void CreateBackup(RegisteredLayoutParameterFile file, string content)
    {
        var backupDirectoryPath = GetBackupDirectoryPath(file);
        Directory.CreateDirectory(backupDirectoryPath);

        var backupId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupPath = Path.Combine(backupDirectoryPath, backupId + ".txt");
        WriteTextAtomically(backupPath, content);
    }

    private void TrimBackups(RegisteredLayoutParameterFile file)
    {
        var backupDirectoryPath = GetBackupDirectoryPath(file);
        if (!Directory.Exists(backupDirectoryPath))
        {
            return;
        }

        var staleBackups = Directory.GetFiles(backupDirectoryPath, "*.txt")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => Path.GetFileNameWithoutExtension(info.Name), StringComparer.Ordinal)
            .Skip(MaxBackupCount)
            .ToArray();

        foreach (var backup in staleBackups)
        {
            File.Delete(backup.FullName);
        }
    }

    private string ResolveOriginalFilePath(RegisteredLayoutParameterFile file)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_workspaceRootPath, file.RelativePath));
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Layout-Parameterdatei '{file.RelativePath}' wurde nicht gefunden.");
        }

        return fullPath;
    }

    private string ResolveBackupFilePath(RegisteredLayoutParameterFile file, string backupId)
    {
        return Path.Combine(GetBackupDirectoryPath(file), backupId + ".txt");
    }

    private string GetBackupDirectoryPath(RegisteredLayoutParameterFile file)
    {
        return Path.Combine(_backupRootPath, file.Key);
    }

    private static void WriteTextAtomically(string path, string content)
    {
        var directoryPath = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException("Zielverzeichnis konnte nicht ermittelt werden.");
        }

        Directory.CreateDirectory(directoryPath);

        var tempPath = Path.Combine(directoryPath, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempPath, content, Utf8WithoutBom);

        File.Move(tempPath, path, overwrite: true);
    }

    private static string GuardPath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Pfad ist erforderlich.", parameterName);
        }

        return path.Trim();
    }

    private static RegisteredLayoutParameterFile GetRegisteredFile(string key)
    {
        return RegisteredFiles.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Die angeforderte Layout-Parameterdatei ist nicht registriert.");
    }

    private static DateTimeOffset ParseBackupCreatedAt(string backupId)
    {
        if (DateTimeOffset.TryParseExact(
                backupId,
                "yyyyMMddHHmmssfff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return DateTimeOffset.UtcNow;
    }

    private sealed record RegisteredLayoutParameterFile(
        string Key,
        string DisplayName,
        string RelativePath);
}
