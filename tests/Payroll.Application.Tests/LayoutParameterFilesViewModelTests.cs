using Payroll.Application.Layout;
using Payroll.Desktop.ViewModels;

namespace Payroll.Application.Tests;

public sealed class LayoutParameterFilesViewModelTests
{
    [Fact]
    public async Task SelectingFile_LoadsContentIntoEditor()
    {
        var repository = new InMemoryLayoutParameterFileRepository();
        var viewModel = new LayoutParameterFilesViewModel(new LayoutParameterFileService(repository));

        await viewModel.InitializeAsync();

        Assert.Equal("line-1\nline-2", viewModel.EditorText);
        Assert.Equal("src/Payroll.Desktop/Styles/DesignSystem.axaml", viewModel.SelectedFilePath);
        Assert.Single(viewModel.Files);
    }

    [Fact]
    public async Task SaveAndRestore_UpdateStatusAndBackups()
    {
        var repository = new InMemoryLayoutParameterFileRepository();
        var viewModel = new LayoutParameterFilesViewModel(new LayoutParameterFileService(repository));
        await viewModel.InitializeAsync();

        viewModel.EditorText = "changed";
        viewModel.SaveCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage.Contains("gespeichert", StringComparison.Ordinal));

        Assert.Equal("changed", repository.CurrentContent);
        Assert.Single(viewModel.Backups);

        viewModel.SelectedBackup = viewModel.Backups.Single();
        viewModel.RestoreCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.StatusMessage.Contains("wiederhergestellt", StringComparison.Ordinal));

        Assert.Equal("line-1\nline-2", viewModel.EditorText);
        Assert.Single(viewModel.Backups);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var startedAt = DateTime.UtcNow;

        while (!condition())
        {
            if ((DateTime.UtcNow - startedAt).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Condition was not met within the expected time.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class InMemoryLayoutParameterFileRepository : ILayoutParameterFileRepository
    {
        private readonly List<LayoutParameterBackupDto> _backups = [];

        public string CurrentContent { get; private set; } = "line-1\nline-2";

        public Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<LayoutParameterFileSummaryDto>>(
            [
                new LayoutParameterFileSummaryDto("design-system", "Design System", "src/Payroll.Desktop/Styles/DesignSystem.axaml", DateTimeOffset.UtcNow)
            ]);
        }

        public Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDocument(key));
        }

        public Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default)
        {
            _backups.Clear();
            _backups.Add(new LayoutParameterBackupDto("backup-1", DateTimeOffset.UtcNow, "2026-04-21 15:30:00"));
            CurrentContent = command.Content;
            return Task.FromResult(CreateDocument(command.Key));
        }

        public Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default)
        {
            CurrentContent = "line-1\nline-2";
            _backups.Clear();
            _backups.Add(new LayoutParameterBackupDto("backup-2", DateTimeOffset.UtcNow, "2026-04-21 15:31:00"));
            return Task.FromResult(CreateDocument(command.Key));
        }

        private LayoutParameterFileDocumentDto CreateDocument(string key)
        {
            return new LayoutParameterFileDocumentDto(
                key,
                "Design System",
                "src/Payroll.Desktop/Styles/DesignSystem.axaml",
                CurrentContent,
                _backups.ToArray());
        }
    }
}
