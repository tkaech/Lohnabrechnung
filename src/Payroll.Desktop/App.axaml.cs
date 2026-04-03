using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Payroll.Desktop;

public sealed partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var bootstrapper = new global::Payroll.Desktop.Bootstrapping.AppBootstrapper();
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = bootstrapper.CreateMainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
