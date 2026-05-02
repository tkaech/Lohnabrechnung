using Avalonia.Controls;
using Avalonia.Interactivity;
using Payroll.Desktop.ViewModels;

namespace Payroll.Desktop.Views;

public sealed partial class TimeImportPreviewWindow : Window
{
    public TimeImportPreviewWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async void OnConfirmImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            Close(false);
            return;
        }

        await viewModel.ImportSelectedTimeDataAsync();
        if (viewModel.TimeImportFeedback.IsSuccess)
        {
            await Task.Delay(500);
            Close(true);
        }
    }
}
