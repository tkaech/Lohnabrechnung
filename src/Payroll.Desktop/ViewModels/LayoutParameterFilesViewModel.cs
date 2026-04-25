using System.Collections.ObjectModel;
using Payroll.Application.Layout;

namespace Payroll.Desktop.ViewModels;

public sealed class LayoutParameterFilesViewModel : ViewModelBase
{
    private readonly LayoutParameterFileService _service;
    private bool _suppressSelectionLoad;
    private LayoutParameterFileSummaryDto? _selectedFile;
    private LayoutParameterBackupDto? _selectedBackup;
    private string _editorText = string.Empty;
    private string _selectedFilePath = string.Empty;
    private string _statusMessage = "Layout-Parameterdatei auswaehlen.";
    private bool _isBusy;

    public LayoutParameterFilesViewModel(LayoutParameterFileService service)
    {
        _service = service;
        Files = [];
        Backups = [];
        SaveCommand = new DelegateCommand(SaveAsync, () => CanSave);
        RestoreCommand = new DelegateCommand(RestoreAsync, () => CanRestore);
    }

    public ObservableCollection<LayoutParameterFileSummaryDto> Files { get; }

    public ObservableCollection<LayoutParameterBackupDto> Backups { get; }

    public DelegateCommand SaveCommand { get; }

    public DelegateCommand RestoreCommand { get; }

    public LayoutParameterFileSummaryDto? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (!SetProperty(ref _selectedFile, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(CanSave));
            RaisePropertyChanged(nameof(CanRestore));
            SaveCommand.RaiseCanExecuteChanged();
            RestoreCommand.RaiseCanExecuteChanged();

            if (value is not null)
            {
                _ = LoadSelectedFileAsync(value.Key);
            }
            else
            {
                SelectedFilePath = string.Empty;
                EditorText = string.Empty;
                Backups.Clear();
                SelectedBackup = null;
            }
        }
    }

    public LayoutParameterBackupDto? SelectedBackup
    {
        get => _selectedBackup;
        set
        {
            if (SetProperty(ref _selectedBackup, value))
            {
                RaisePropertyChanged(nameof(CanRestore));
                RestoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string EditorText
    {
        get => _editorText;
        set => SetProperty(ref _editorText, value);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        private set => SetProperty(ref _selectedFilePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(CanSave));
                RaisePropertyChanged(nameof(CanRestore));
                SaveCommand.RaiseCanExecuteChanged();
                RestoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanSave => !IsBusy && SelectedFile is not null;

    public bool CanRestore => !IsBusy && SelectedFile is not null && SelectedBackup is not null;

    public async Task InitializeAsync()
    {
        IsBusy = true;

        try
        {
            var files = await _service.ListFilesAsync();
            Files.Clear();
            foreach (var file in files)
            {
                Files.Add(file);
            }

            if (Files.Count == 0)
            {
                StatusMessage = "Keine Layout-Parameterdateien registriert.";
                SelectedFile = null;
                return;
            }

            _suppressSelectionLoad = true;
            SelectedFile = Files[0];
            _suppressSelectionLoad = false;
        }
        finally
        {
            IsBusy = false;
        }

        await LoadSelectedFileAsync(Files[0].Key);
    }

    private async Task LoadSelectedFileAsync(string key)
    {
        if (_suppressSelectionLoad || IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var document = await _service.GetFileAsync(key);
            ApplyDocument(document);
            StatusMessage = $"Layout-Datei geladen: {document.DisplayName}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (SelectedFile is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var document = await _service.SaveAsync(new SaveLayoutParameterFileCommand(SelectedFile.Key, EditorText));
            ApplyDocument(document);
            await RefreshSelectedFileSummaryAsync(document.Key);
            StatusMessage = $"Layout-Datei gespeichert: {document.DisplayName}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreAsync()
    {
        if (SelectedFile is null || SelectedBackup is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var document = await _service.RestoreBackupAsync(new RestoreLayoutParameterFileBackupCommand(SelectedFile.Key, SelectedBackup.BackupId));
            ApplyDocument(document);
            await RefreshSelectedFileSummaryAsync(document.Key);
            StatusMessage = $"Backup wiederhergestellt: {document.DisplayName}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyDocument(LayoutParameterFileDocumentDto document)
    {
        SelectedFilePath = document.RelativePath;
        EditorText = document.Content;
        Backups.Clear();
        foreach (var backup in document.AvailableBackups)
        {
            Backups.Add(backup);
        }

        SelectedBackup = Backups.FirstOrDefault();
    }

    private async Task RefreshSelectedFileSummaryAsync(string key)
    {
        var index = Files.IndexOf(Files.First(item => item.Key == key));
        var updated = (await _service.ListFilesAsync()).First(item => item.Key == key);
        Files[index] = updated;
        _suppressSelectionLoad = true;
        SelectedFile = updated;
        _suppressSelectionLoad = false;
    }
}
