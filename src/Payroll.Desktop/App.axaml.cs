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
            try
            {
                var bootstrapper = new global::Payroll.Desktop.Bootstrapping.AppBootstrapper();
                var runtimeOptions = global::Payroll.Desktop.Bootstrapping.DesktopRuntimeOptionsLoader.Load(global::Payroll.Desktop.Bootstrapping.StartupArguments.Current.ToArray());
                desktop.MainWindow = new Views.MainWindow
                {
                    DataContext = bootstrapper.CreateMainWindowViewModel(runtimeOptions)
                };
            }
            catch (Exception exception)
            {
                var logPath = StartupErrorLogger.WriteStartupErrorLog(exception);
                throw new InvalidOperationException($"{StartupErrorLogger.BuildStartupErrorMessage(exception)} | startup log: {logPath}", exception);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
