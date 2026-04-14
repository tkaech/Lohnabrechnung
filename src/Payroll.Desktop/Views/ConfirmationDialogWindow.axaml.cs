using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Payroll.Desktop.Views;

public sealed partial class ConfirmationDialogWindow : Window
{
    public ConfirmationDialogWindow()
    {
        InitializeComponent();
    }

    public ConfirmationDialogWindow(string title, string message, string confirmButtonText)
        : this()
    {
        DataContext = new ConfirmationDialogViewModel
        {
            DialogTitle = title,
            DialogMessage = message,
            ConfirmButtonText = confirmButtonText
        };
        Title = title;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    public sealed class ConfirmationDialogViewModel
    {
        public string DialogTitle { get; init; } = string.Empty;
        public string DialogMessage { get; init; } = string.Empty;
        public string ConfirmButtonText { get; init; } = "Bestaetigen";
    }
}
