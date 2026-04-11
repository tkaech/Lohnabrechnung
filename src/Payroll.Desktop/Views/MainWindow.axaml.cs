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
        if (sender is not ListBox { SelectedItem: MonthlyTimeEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromTimeEntryAsync(entry);
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
        if (sender is not ListBox { SelectedItem: MonthlyExpenseEntryItemViewModel entry })
        {
            return;
        }

        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            await monthlyRecord.ActivateMonthFromExpenseEntryAsync(entry);
        }
    }

    private void OnTimeMonthInputPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.ToggleTimeMonthPicker();
        }
    }

    private void OnTimeMonthInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.EnsureTimeMonthPickerOpen();
        }
    }

    private void OnExpenseMonthInputPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.ToggleExpenseMonthPicker();
        }
    }

    private void OnExpenseMonthInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.EnsureExpenseMonthPickerOpen();
        }
    }

    private void OnTimeDateInputPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.ToggleTimeDatePicker();
        }
    }

    private void OnTimeDateInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { } monthlyRecord })
        {
            monthlyRecord.EnsureTimeDatePickerOpen();
        }
    }

    private void OnTimeDatePickerSelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { MonthlyRecord: { IsTimeDatePickerOpen: true } monthlyRecord })
        {
            monthlyRecord.CloseAllPickers();
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
        if (IsWithinPickerSurface(e.Source))
        {
            return;
        }

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

    private static bool IsWithinPickerSurface(object? source)
    {
        var current = source as StyledElement;
        while (current is not null)
        {
            if (current is Control control && control.Name is "TimeMonthFieldHost" or "TimeMonthInput" or "TimeMonthPopup"
                or "ExpenseMonthFieldHost" or "ExpenseMonthInput" or "ExpenseMonthPopup"
                or "TimeDateFieldHost" or "TimeDateInput" or "TimeDatePopup")
            {
                return true;
            }

            current = current.Parent as StyledElement;
        }

        return false;
    }
}
