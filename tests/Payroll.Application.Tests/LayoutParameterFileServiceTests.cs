using Payroll.Application.Layout;

namespace Payroll.Application.Tests;

public sealed class LayoutParameterFileServiceTests
{
    [Fact]
    public async Task SaveAsync_PassesRawTextUnchangedToRepository()
    {
        var repository = new RecordingLayoutParameterFileRepository();
        var service = new LayoutParameterFileService(repository);
        var rawText = "<Styles>\r\n  <Color>#FF123456</Color>\n</Styles>";

        await service.SaveAsync(new SaveLayoutParameterFileCommand("design-system", rawText));

        Assert.Equal(rawText, repository.LastSavedCommand?.Content);
    }

    [Fact]
    public async Task RestoreBackupAsync_ForwardsKeyAndBackupId()
    {
        var repository = new RecordingLayoutParameterFileRepository();
        var service = new LayoutParameterFileService(repository);

        await service.RestoreBackupAsync(new RestoreLayoutParameterFileBackupCommand("design-system", "20260421153000123"));

        Assert.Equal("design-system", repository.LastRestoreCommand?.Key);
        Assert.Equal("20260421153000123", repository.LastRestoreCommand?.BackupId);
    }

    private sealed class RecordingLayoutParameterFileRepository : ILayoutParameterFileRepository
    {
        public SaveLayoutParameterFileCommand? LastSavedCommand { get; private set; }

        public RestoreLayoutParameterFileBackupCommand? LastRestoreCommand { get; private set; }

        public Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<LayoutParameterFileSummaryDto>>([]);
        }

        public Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LayoutParameterFileDocumentDto(key, "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml", string.Empty, []));
        }

        public Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default)
        {
            LastSavedCommand = command;
            return Task.FromResult(new LayoutParameterFileDocumentDto(command.Key, "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml", command.Content, []));
        }

        public Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default)
        {
            LastRestoreCommand = command;
            return Task.FromResult(new LayoutParameterFileDocumentDto(command.Key, "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml", string.Empty, []));
        }
    }
}
