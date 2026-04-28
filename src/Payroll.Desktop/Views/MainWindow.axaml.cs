using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Payroll.Desktop.ViewModels;

namespace Payroll.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private const string TimeEntryColumnDragFormat = "application/x-payroll-time-entry-column";

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Deactivated += OnDeactivated;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private async void OnTimeEntryHistoryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: MonthlyTimeEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromTimeEntryAsync(entry);
        }
    }

    private async void OnTimeEntryHistoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: MonthlyTimeEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromTimeEntryAsync(entry);
        }
    }

    private async void OnTimeEntryHistorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not MonthlyTimeEntryItemViewModel entry)
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromTimeEntryAsync(entry);
            EnsureTimeEntryListSelection(listBox, entry.TimeEntryId);
        }
    }

    private async void OnExpenseEntryHistoryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: MonthlyExpenseEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromExpenseEntryAsync(entry);
        }
    }

    private async void OnExpenseEntryHistoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: MonthlyExpenseEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromExpenseEntryAsync(entry);
        }
    }

    private async void OnExpenseEntryHistorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.SelectedItem is not MonthlyExpenseEntryItemViewModel entry)
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromExpenseEntryAsync(entry);
            EnsureExpenseEntryListSelection(listBox, entry.ExpenseEntryId);
        }
    }

    private static void EnsureTimeEntryListSelection(ListBox listBox, Guid timeEntryId)
    {
        foreach (var item in listBox.Items)
        {
            if (item is MonthlyTimeEntryItemViewModel entry && entry.TimeEntryId == timeEntryId)
            {
                if (!ReferenceEquals(listBox.SelectedItem, entry))
                {
                    listBox.SelectedItem = entry;
                }

                return;
            }
        }
    }

    private static void EnsureExpenseEntryListSelection(ListBox listBox, Guid expenseEntryId)
    {
        foreach (var item in listBox.Items)
        {
            if (item is MonthlyExpenseEntryItemViewModel entry && entry.ExpenseEntryId == expenseEntryId)
            {
                if (!ReferenceEquals(listBox.SelectedItem, entry))
                {
                    listBox.SelectedItem = entry;
                }

                return;
            }
        }
    }

    private async void OnTimeEntryColumnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TimeEntryColumnViewModel column })
        {
            return;
        }

        var data = new DataObject();
        data.Set(TimeEntryColumnDragFormat, column.Key);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
    }

    private void OnTimeEntryColumnHeaderDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(TimeEntryColumnDragFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTimeEntryColumnHeaderDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Control { DataContext: TimeEntryColumnViewModel targetColumn }
            || !e.Data.Contains(TimeEntryColumnDragFormat)
            || e.Data.Get(TimeEntryColumnDragFormat) is not string sourceColumnKey)
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.MoveTimeEntryColumn(sourceColumnKey, targetColumn.Key);
        }

        e.Handled = true;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.CloseAllPickers();
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.CloseAllPickers();
        }
    }

    private void OnPayrollPreviewDerivationGroupInitialized(object? sender, EventArgs e)
    {
        if (sender is not Expander expander || expander.Tag is not string title)
        {
            return;
        }

        expander.IsExpanded = title switch
        {
            "Eingaben" => true,
            "Regeln / Saetze" => false,
            "Rechenschritte" => false,
            _ => false
        };
    }

    private async void OnBrowseBackupDirectoryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null || !storageProvider.CanPickFolder)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Backup-Zielordner waehlen",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is null)
        {
            return;
        }

        var localPath = folder.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.BackupDirectoryPath = localPath;
        }
    }

    private async void OnBrowseRestoreFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null || !storageProvider.CanOpen)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Backup-Datei waehlen",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Payroll Backup")
                {
                    Patterns = ["*.payrollbackup.json"]
                },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.RestoreFilePath = localPath;
        }
    }

    private async void OnBrowsePersonImportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null || !storageProvider.CanOpen)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "CSV-Datei fuer Personendaten waehlen",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV-Dateien")
                {
                    Patterns = ["*.csv", "*.txt"]
                },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.PersonImportCsvFilePath = localPath;
        }
    }

    private async void OnBrowseTimeImportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null || !storageProvider.CanOpen)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "CSV-Datei fuer Stundendaten waehlen",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV-Dateien")
                {
                    Patterns = ["*.csv", "*.txt"]
                },
                FilePickerFileTypes.All
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var localPath = file.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.TimeImportCsvFilePath = localPath;
        }
    }

    private async void OnOpenPersonImportPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var prepared = await viewModel.PreparePersonImportPreviewAsync();
        if (!prepared || viewModel.PersonImportPreviewItems.Count == 0)
        {
            return;
        }

        var previewWindow = new PersonImportPreviewWindow
        {
            DataContext = viewModel
        };

        await previewWindow.ShowDialog<bool?>(this);
    }

    private async void OnImportTimeDataClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var alreadyImported = await viewModel.IsSelectedTimeImportMonthAlreadyImportedAsync();
        if (alreadyImported)
        {
            var shouldOverwrite = await ShowConfirmationDialogAsync(
                "Stundendaten ueberschreiben",
                "Fuer den gewaelten Monat wurden bereits Stundendaten importiert. Soll der bestehende Monatsimport vollstaendig ersetzt werden?",
                "Ueberschreiben");

            if (!shouldOverwrite)
            {
                return;
            }

            await viewModel.ImportTimeDataAsync(true);
            return;
        }

        await viewModel.ImportTimeDataAsync();
    }

    private async void OnDeleteImportedTimeMonthClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedImportedTimeMonth is null)
        {
            return;
        }

        var shouldDelete = await ShowConfirmationDialogAsync(
            "Importmonat loeschen",
            $"Der importierte Monat {viewModel.SelectedImportedTimeMonth.DisplayName} und alle dazugehoerigen importierten Stundendaten werden entfernt. Fortfahren?",
            "Monat loeschen");

        if (!shouldDelete)
        {
            return;
        }

        await viewModel.DeleteImportedTimeMonthAsync();
    }

    private async void OnFinalizePayrollMonthClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.CanCancelPayrollMonth)
        {
            var shouldCancel = await ShowConfirmationDialogAsync(
                "Lohnlauf stornieren",
                "Moechten Sie den abgeschlossenen Monat wirklich stornieren? Der Lohnlauf bleibt gespeichert, wird aber im Jahreslohn nicht mehr beruecksichtigt.",
                "Stornieren");

            if (!shouldCancel)
            {
                return;
            }

            if (viewModel.CancelPayrollMonthCommand.CanExecute(null))
            {
                viewModel.CancelPayrollMonthCommand.Execute(null);
            }

            return;
        }

        var shouldFinalize = await ShowConfirmationDialogAsync(
            "Lohnlauf abschliessen",
            "Moechten Sie den ausgewaehlten Monat wirklich finalisieren? Nach Abschluss kann der Monat nur noch per Storno aufgehoben werden.",
            "Abschliessen");

        if (!shouldFinalize)
        {
            return;
        }

        if (viewModel.FinalizePayrollMonthCommand.CanExecute(null))
        {
            viewModel.FinalizePayrollMonthCommand.Execute(null);
        }
    }

    private async void OnCreateSalaryCertificatePdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!viewModel.ValidateSalaryCertificateExportPrerequisites())
        {
            return;
        }

        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null || !storageProvider.CanSave)
        {
            viewModel.StatusMessage = "Dateidialog fuer Lohnausweis ist nicht verfuegbar.";
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lohnausweis speichern",
            SuggestedFileName = viewModel.GetSuggestedSalaryCertificateFileName(),
            FileTypeChoices =
            [
                new FilePickerFileType("PDF-Dateien")
                {
                    Patterns = ["*.pdf"]
                }
            ],
            DefaultExtension = "pdf",
            ShowOverwritePrompt = true
        });

        var localPath = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            viewModel.StatusMessage = "Kein Ausgabeort fuer Lohnausweis gewaehlt.";
            return;
        }

        await viewModel.CreateSalaryCertificatePdfAsync(localPath);
    }

    private async Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmButtonText)
    {
        var dialog = new ConfirmationDialogWindow(title, message, confirmButtonText);
        var result = await dialog.ShowDialog<bool?>(this);
        return result == true;
    }

}
