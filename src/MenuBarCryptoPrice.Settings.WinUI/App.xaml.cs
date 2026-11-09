using MenuBarCryptoPrice.Infrastructure;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace MenuBarCryptoPrice.Settings.WinUI;

public partial class App : Application
{
    private Window? _window;
    private HostLifetimeWatcher? _hostWatcher;
    private bool _closing;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _hostWatcher = HostLifetimeWatcher.TryAttach(HostLifetime.MutexName);
        if (_hostWatcher is not null)
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _hostWatcher.HostExited += (_, _) =>
                dispatcherQueue.TryEnqueue(CloseForHostExit);
        }

        _window.Closed += (_, _) => _hostWatcher?.Dispose();
        _window.Activate();
    }

    private void CloseForHostExit()
    {
        if (_closing || _window is null)
        {
            return;
        }

        _closing = true;
        _window.Close();
    }
}
