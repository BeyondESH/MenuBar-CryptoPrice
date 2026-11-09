using System.Windows;
using System.Windows.Interop;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;
using MenuBarCryptoPrice.Taskbar;

namespace MenuBarCryptoPrice;

public sealed class AppHost : IDisposable
{
    private readonly ILog _log;
    private readonly SingleInstance _singleInstance;
    private readonly ConfigService _configService;
    private readonly BitgetNetwork _network;
    private readonly TickerStore _store = new();
    private readonly CatalogService _catalog;
    private readonly TaskbarLocator _locator;
    private readonly WindowPlacementService _placement = new();
    private readonly VirtualDesktopService _virtualDesktop;
    private readonly AutoStartService _autoStart;
    private readonly ITextMeasurer _measurer = new WpfTextMeasurer();
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _topmostTimer;
    private readonly DispatcherTimer _applyConfigTimer;
    private readonly DispatcherTimer _externalConfigTimer;
    private readonly TickerStripViewModel _strip;
    private readonly VisibilityCoordinator _visibility;
    private readonly MarketDataService _data;

    private TickerWindow _window;
    private TrayIconService? _tray;
    private FileSystemWatcher? _configWatcher;
    private InstrumentKey[] _activeKeys = Array.Empty<InstrumentKey>();
    private NetworkEndpointSettings? _activeNetwork;
    private double _lastAvailableWidth = -1;
    private double _lastPlacedWidth = -1;
    private AnchorMode _lastAnchor = (AnchorMode)(-1);
    private bool _disposed;

    public AppHost(ILog log, SingleInstance singleInstance)
    {
        _log = log;
        _singleInstance = singleInstance;
        _configService = new ConfigService(AppPaths.ConfigFile, log);
        _network = new BitgetNetwork(log);
        _network.Apply(_configService.Config);
        _catalog = new CatalogService(AppPaths.CacheDirectory, log, network: _network);
        _locator = new TaskbarLocator(log);
        _virtualDesktop = new VirtualDesktopService(log);
        _autoStart = new AutoStartService(log);
        _strip = new TickerStripViewModel(_configService.Config, _store, _measurer);
        _window = CreateWindow();

        _visibility = new VisibilityCoordinator(_locator, log);
        _data = new MarketDataService(
            new BitgetRestClient(_network),
            () => new BitgetWsClient(log, _network),
            _store,
            log);

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _topmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _applyConfigTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _externalConfigTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
    }

    public void Start(bool autoStartLaunch)
    {
        _configService.Changed += OnConfigChanged;
        _singleInstance.ActivationRequested += OnActivationRequested;

        _visibility.VisibilityChanged += OnVisibilityChanged;
        _visibility.GeometryChanged += OnGeometryChanged;
        _visibility.DesktopSwitched += OnDesktopSwitched;

        _refreshTimer.Tick += OnRefreshTick;
        _topmostTimer.Tick += OnTopmostTick;
        _applyConfigTimer.Tick += OnApplyConfigTick;
        _externalConfigTimer.Tick += OnExternalConfigTick;
        StartConfigWatcher();

        _window.Show();
        ApplyAutoStartState();
        PositionAndSize();

        StartMarketData();

        _tray = new TrayIconService(_configService.Config.StartWithWindows, _log);
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.RepositionRequested += (_, _) => RepositionNow();
        _tray.AutoStartToggleRequested += (_, _) => ToggleAutoStart();
        _tray.AboutRequested += (_, _) => ShowAbout();
        _tray.ExitRequested += (_, _) => Application.Current.Shutdown();

        _refreshTimer.Start();
        _topmostTimer.Start();
        _visibility.Evaluate();

        if (_configService.IsFirstRun)
        {
            _configService.SaveNow();
            if (!autoStartLaunch)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => OpenSettings()),
                    DispatcherPriority.ApplicationIdle);
            }
        }

        _log.Info("MenuBarCryptoPrice started.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _refreshTimer.Stop();
        _topmostTimer.Stop();
        _applyConfigTimer.Stop();
        _externalConfigTimer.Stop();
        StopConfigWatcher();

        _configService.Changed -= OnConfigChanged;
        _singleInstance.ActivationRequested -= OnActivationRequested;
        _visibility.VisibilityChanged -= OnVisibilityChanged;
        _visibility.GeometryChanged -= OnGeometryChanged;
        _visibility.DesktopSwitched -= OnDesktopSwitched;

        _visibility.Dispose();
        _data.Dispose();
        _network.Dispose();

        _tray?.Dispose();
        _tray = null;

        try
        {
            _window.Close();
        }
        catch
        {
            // The window may already be closed.
        }

        _configService.Dispose();
        _singleInstance.Dispose();
        _log.Info("MenuBarCryptoPrice stopped.");
    }

    private TickerWindow CreateWindow()
    {
        var window = new TickerWindow(_strip);
        window.SettingsRequested += (_, _) => OpenSettings();
        window.RepositionRequested += (_, _) => RepositionNow();
        window.AutoStartToggleRequested += (_, _) => ToggleAutoStart();
        window.AboutRequested += (_, _) => ShowAbout();
        window.ExitRequested += (_, _) => Application.Current.Shutdown();
        return window;
    }

    private IReadOnlyList<InstrumentKey> Keys()
        => _configService.Config.Items.Select(item => item.Key).ToList();

    private void StartMarketData()
    {
        _activeKeys = Keys().Distinct().ToArray();
        _activeNetwork = _network.Settings;
        _data.Start(_activeKeys);
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        _strip.Refresh();

        var geometry = _locator.TryGetGeometry();
        var (anchor, available) = AnchorResolver.Resolve(_configService.Config.Anchor, geometry);
        if (double.IsNaN(available) || available <= 0)
        {
            available = 585;
        }

        var rebuild = Math.Abs(available - _lastAvailableWidth) > 1
            || anchor != _lastAnchor
            || _strip.TakeWidthChanged();
        if (!rebuild)
        {
            return;
        }

        _lastAvailableWidth = available;
        _lastAnchor = anchor;
        _strip.Rebuild(available);
        if (_window.IsVisible)
        {
            PositionAndSize();
        }
    }

    private void OnTopmostTick(object? sender, EventArgs e)
    {
        if (_window.IsVisible)
        {
            _placement.BringToTop(_window);
        }
    }

    private void OnApplyConfigTick(object? sender, EventArgs e)
    {
        _applyConfigTimer.Stop();
        ApplyConfiguration();
    }

    private void OnConfigChanged(object? sender, AppConfig config)
    {
        _applyConfigTimer.Stop();
        _applyConfigTimer.Start();
    }

    private void ApplyConfiguration()
    {
        _strip.ApplyConfig(_configService.Config);
        _lastAvailableWidth = -1;
        _lastAnchor = (AnchorMode)(-1);
        _network.Apply(_configService.Config);

        var keys = Keys().Distinct().ToArray();
        if (!keys.SequenceEqual(_activeKeys) || _network.Settings != _activeNetwork)
        {
            _activeKeys = keys;
            _activeNetwork = _network.Settings;
            _data.Start(keys);
        }

        PositionAndSize();
        _autoStart.SetEnabled(_configService.Config.StartWithWindows);
        _tray?.SetAutoStartChecked(_configService.Config.StartWithWindows);
        _window.AutoStartChecked = _configService.Config.StartWithWindows;
    }

    private void OnVisibilityChanged(bool visible, HideReason reason)
    {
        if (visible)
        {
            if (!_window.IsVisible)
            {
                _window.Show();
            }

            PositionAndSize();
            _placement.BringToTop(_window);
        }
        else if (_window.IsVisible)
        {
            _window.Hide();
        }

        _log.Info($"Ticker visibility changed: visible={visible}, reason={reason}.");
    }

    private void OnGeometryChanged()
    {
        if (!_window.IsVisible)
        {
            return;
        }

        var geometry = _locator.TryGetGeometry();
        var (anchor, available) = AnchorResolver.Resolve(_configService.Config.Anchor, geometry);
        if (double.IsNaN(available) || available <= 0)
        {
            available = 585;
        }

        if (Math.Abs(available - _lastAvailableWidth) > 1 || anchor != _lastAnchor)
        {
            _lastAvailableWidth = available;
            _lastAnchor = anchor;
            _strip.Rebuild(available);
        }

        PositionAndSize();
    }

    private void OnDesktopSwitched()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var handle = new WindowInteropHelper(_window).Handle;
            var moved = handle != IntPtr.Zero && _virtualDesktop.TryMoveToCurrentDesktop(handle);
            _log.Info($"Virtual desktop switched; window moved={moved}.");
            if (!moved)
            {
                RecreateWindow();
            }

            _visibility.Evaluate();
            PositionAndSize();
        });
    }

    private void OnActivationRequested(object? sender, EventArgs e)
        => Application.Current.Dispatcher.BeginInvoke(new Action(() => OpenSettings()));

    private void PositionAndSize()
    {
        var geometry = _locator.TryGetGeometry();
        var (anchor, available) = AnchorResolver.Resolve(_configService.Config.Anchor, geometry);
        if (double.IsNaN(available) || available <= 0)
        {
            available = 585;
        }

        if (Math.Abs(available - _lastAvailableWidth) > 1 || anchor != _lastAnchor)
        {
            _lastAvailableWidth = available;
            _lastAnchor = anchor;
            _strip.Rebuild(available);
        }

        var width = Math.Max(60, _strip.DesiredWidth);
        var height = _strip.Height;
        _placement.Place(
            _window,
            geometry,
            width,
            height,
            _configService.Config.OffsetX,
            _configService.Config.PositionMode,
            anchor);
        _window.AutoStartChecked = _configService.Config.StartWithWindows;

        if (Math.Abs(width - _lastPlacedWidth) > 0.5)
        {
            _lastPlacedWidth = width;
            _log.Info(
                $"Ticker placed: width={width:0.#} height={height:0.#} anchor={anchor} available={available:0.#} "
                + $"items={_strip.Items.Count} overflow={_strip.OverflowCount} "
                + $"trayLeft={geometry?.TrayLeft.ToString(CultureInfo.InvariantCulture) ?? "n/a"} "
                + $"iconRight={geometry?.IconRight?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} "
                + $"groupLeft={geometry?.GroupLeft?.ToString(CultureInfo.InvariantCulture) ?? "n/a"} "
                + $"widgetsRight={geometry?.WidgetsRight?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}");
        }
    }

    private void RepositionNow()
    {
        _lastAvailableWidth = -1;
        _lastAnchor = (AnchorMode)(-1);
        PositionAndSize();
        _placement.BringToTop(_window);
    }

    private void ApplyAutoStartState()
    {
        _autoStart.SetEnabled(_configService.Config.StartWithWindows);
        _window.AutoStartChecked = _configService.Config.StartWithWindows;
        _tray?.SetAutoStartChecked(_configService.Config.StartWithWindows);
    }

    private void ToggleAutoStart()
    {
        var enabled = !_configService.Config.StartWithWindows;
        _autoStart.SetEnabled(enabled);
        _configService.Update(config => config.StartWithWindows = enabled);
        _tray?.SetAutoStartChecked(enabled);
        _window.AutoStartChecked = enabled;
    }

    private void OpenSettings(string? arguments = null)
    {
        if (!SettingsLauncher.TryOpen(_log, out var error, arguments))
        {
            MessageBox.Show(
                error,
                "MenuBar CryptoPrice 设置",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _log.Info("WinUI settings app launched.");
    }

    private void StartConfigWatcher()
    {
        var directory = Path.GetDirectoryName(AppPaths.ConfigFile);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        _configWatcher = new FileSystemWatcher(directory, "config.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
        };
        _configWatcher.Changed += OnExternalConfigChanged;
        _configWatcher.Created += OnExternalConfigChanged;
        _configWatcher.Renamed += OnExternalConfigRenamed;
        _configWatcher.EnableRaisingEvents = true;
    }

    private void StopConfigWatcher()
    {
        if (_configWatcher is null)
        {
            return;
        }

        _configWatcher.EnableRaisingEvents = false;
        _configWatcher.Changed -= OnExternalConfigChanged;
        _configWatcher.Created -= OnExternalConfigChanged;
        _configWatcher.Renamed -= OnExternalConfigRenamed;
        _configWatcher.Dispose();
        _configWatcher = null;
    }

    private void OnExternalConfigChanged(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _externalConfigTimer.Stop();
            _externalConfigTimer.Start();
        });
    }

    private void OnExternalConfigRenamed(object sender, RenamedEventArgs e)
        => OnExternalConfigChanged(sender, e);

    private void OnExternalConfigTick(object? sender, EventArgs e)
    {
        _externalConfigTimer.Stop();
        _configService.Reload();
    }

    private void ShowAbout() => OpenSettings("--about");

    private void RecreateWindow()
    {
        var oldWindow = _window;
        oldWindow.Hide();

        var replacement = CreateWindow();
        _window = replacement;
        replacement.AutoStartChecked = _configService.Config.StartWithWindows;
        replacement.Show();

        try
        {
            oldWindow.Close();
        }
        catch
        {
            // The old window may already be closed.
        }
    }
}
