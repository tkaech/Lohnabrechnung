using Avalonia.Controls;
using Avalonia.Interactivity;
using Payroll.Desktop.ViewModels;

namespace Payroll.Desktop.Views;

public sealed partial class PersonImportPreviewWindow : Window
{
    public PersonImportPreviewWindow()
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

        await viewModel.ImportSelectedPersonDataAsync();
        if (viewModel.PersonImportFeedback.IsSuccess)
        {
            await Task.Delay(500);
            Close(true);
        }
    }
}
