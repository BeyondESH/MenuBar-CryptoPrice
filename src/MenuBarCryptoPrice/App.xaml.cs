using System.Windows;
using System.Windows.Threading;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice;

public partial class App : Application
{
    private AppHost? _host;
    private ILog? _log;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppPaths.EnsureCreated();
        _log = new FileLogger(AppPaths.LogDirectory);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var singleInstance = new SingleInstance(HostLifetime.ApplicationName);
        if (!singleInstance.IsFirstInstance)
        {
            singleInstance.SignalExistingInstance();
            singleInstance.Dispose();
            Shutdown();
            return;
        }

        try
        {
            _host = new AppHost(_log, singleInstance);
            _host.Start(e.Args.Contains("--autostart", StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _log.Error("Startup failed.", ex);
            MessageBox.Show(
                "应用启动失败：" + ex.Message,
                "MenuBar CryptoPrice",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        _host = null;
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.Error("Unhandled dispatcher exception.", e.Exception);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _log?.Error("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
