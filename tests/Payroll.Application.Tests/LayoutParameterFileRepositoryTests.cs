using Payroll.Infrastructure.Layout;

namespace Payroll.Application.Tests;

public sealed class LayoutParameterFileRepositoryTests
{
    [Fact]
    public async Task SaveAsync_CreatesBackupAndOverwritesOriginal()
    {
        var workspaceRoot = CreateWorkspace();

        try
        {
            var filePath = Path.Combine(workspaceRoot, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml");
            await File.WriteAllTextAsync(filePath, "<Styles>v1</Styles>");
            var repository = new LayoutParameterFileRepository(workspaceRoot);

            var saved = await repository.SaveAsync(new("design-system", "<Styles>v2</Styles>"));

            Assert.Equal("<Styles>v2</Styles>", await File.ReadAllTextAsync(filePath));
            Assert.Single(saved.AvailableBackups);
            var backupPath = Path.Combine(workspaceRoot, ".layout-parameter-backups", "design-system", saved.AvailableBackups.Single().BackupId + ".txt");
            Assert.Equal("<Styles>v1</Styles>", await File.ReadAllTextAsync(backupPath));
        }
        finally
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_KeepsAtMostTwoBackups()
    {
        var workspaceRoot = CreateWorkspace();

        try
        {
            var filePath = Path.Combine(workspaceRoot, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml");
            await File.WriteAllTextAsync(filePath, "<Styles>v1</Styles>");
            var repository = new LayoutParameterFileRepository(workspaceRoot);

            await repository.SaveAsync(new("design-system", "<Styles>v2</Styles>"));
            await Task.Delay(5);
            await repository.SaveAsync(new("design-system", "<Styles>v3</Styles>"));
            await Task.Delay(5);
            var saved = await repository.SaveAsync(new("design-system", "<Styles>v4</Styles>"));

            Assert.Equal("<Styles>v4</Styles>", await File.ReadAllTextAsync(filePath));
            Assert.Equal(2, saved.AvailableBackups.Count);
            Assert.DoesNotContain(saved.AvailableBackups, item =>
            {
                var path = Path.Combine(workspaceRoot, ".layout-parameter-backups", "design-system", item.BackupId + ".txt");
                return File.ReadAllText(path) == "<Styles>v1</Styles>";
            });
        }
        finally
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresSelectedVersionAndKeepsTwoBackups()
    {
        var workspaceRoot = CreateWorkspace();

        try
        {
            var filePath = Path.Combine(workspaceRoot, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml");
            await File.WriteAllTextAsync(filePath, "<Styles>v1</Styles>");
            var repository = new LayoutParameterFileRepository(workspaceRoot);

            await repository.SaveAsync(new("design-system", "<Styles>v2</Styles>"));
            await Task.Delay(5);
            var afterSecondSave = await repository.SaveAsync(new("design-system", "<Styles>v3</Styles>"));
            var oldestRemaining = afterSecondSave.AvailableBackups.OrderBy(item => item.CreatedAt).First();

            var restored = await repository.RestoreBackupAsync(new("design-system", oldestRemaining.BackupId));

            Assert.Equal("<Styles>v1</Styles>", await File.ReadAllTextAsync(filePath));
            Assert.Equal(2, restored.AvailableBackups.Count);
            var backupContents = restored.AvailableBackups
                .Select(item => File.ReadAllText(Path.Combine(workspaceRoot, ".layout-parameter-backups", "design-system", item.BackupId + ".txt")))
                .ToArray();
            Assert.Contains("<Styles>v3</Styles>", backupContents);
        }
        finally
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    private static string CreateWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), $"layout-parameter-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "Payroll.Desktop", "Styles"));
        File.WriteAllText(Path.Combine(root, "src", "Payroll.Desktop", "Styles", "DesignSystem.axaml"), string.Empty);
        File.WriteAllText(Path.Combine(root, "src", "Payroll.Desktop", "Styles", "PrintDesignSystem.axaml"), string.Empty);
        return root;
    }
}
