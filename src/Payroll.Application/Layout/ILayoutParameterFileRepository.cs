namespace Payroll.Application.Layout;

public interface ILayoutParameterFileRepository
{
    Task<IReadOnlyCollection<LayoutParameterFileSummaryDto>> ListFilesAsync(CancellationToken cancellationToken = default);
    Task<LayoutParameterFileDocumentDto> GetFileAsync(string key, CancellationToken cancellationToken = default);
    Task<LayoutParameterFileDocumentDto> SaveAsync(SaveLayoutParameterFileCommand command, CancellationToken cancellationToken = default);
    Task<LayoutParameterFileDocumentDto> RestoreBackupAsync(RestoreLayoutParameterFileBackupCommand command, CancellationToken cancellationToken = default);
}
