using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MenuBarCryptoPrice.Settings.WinUI.ViewModels;

public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    private static readonly SolidColorBrush LiveBrushValue = CreateBrush(0x4E, 0xD8, 0xA0);
    private static readonly SolidColorBrush WarnBrushValue = CreateBrush(0xE0, 0xB1, 0x5C);
    private static readonly SolidColorBrush IdleBrushValue = CreateBrush(0xA1, 0xA7, 0xAD);

    private readonly ConfigService _configService;
    private readonly CatalogService _catalog;
    private readonly BitgetRestClient _rest;
    private readonly BitgetNetwork _network;
    private readonly ILog _log;
    private readonly DispatcherTimer _liveTimer;
    private readonly DispatcherTimer _statusTimer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<Market, IReadOnlyList<CatalogEntry>> _catalogs = new();

    private string _catalogStatus = "正在加载币种目录…";
    private string _liveStatus = "等待行情";
    private Brush _liveBrush = IdleBrushValue;
    private bool _isStatusOpen;
    private string _statusMessage = string.Empty;
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Success;
    private bool _isBusy;
    private string _busyText = "正在加载…";
    private bool _refreshing;
    private bool _disposed;
    private string _connectionTestStatus = "尚未测试";
    private bool _isTestingConnection;

    public SettingsViewModel(
        ConfigService configService,
        CatalogService catalog,
        BitgetRestClient rest,
        ILog log,
        BitgetNetwork network)
    {
        _configService = configService;
        _catalog = catalog;
        _rest = rest;
        _log = log;
        _network = network;

        Items = new ObservableCollection<ItemRowViewModel>();
        RebuildRows();

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _liveTimer.Tick += async (_, _) => await RefreshQuotesAsync();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            IsStatusOpen = false;
        };
    }

    public ObservableCollection<ItemRowViewModel> Items { get; }

    public string ConfigPath => AppPaths.ConfigFile;

    public string VersionText
    {
        get
        {
            var version = typeof(SettingsViewModel).Assembly.GetName().Version;
            return version is null ? "v1.0.0" : $"v{version.ToString(3)}";
        }
    }

    public string CatalogStatus
    {
        get => _catalogStatus;
        private set => SetProperty(ref _catalogStatus, value);
    }

    public string LiveStatus
    {
        get => _liveStatus;
        private set => SetProperty(ref _liveStatus, value);
    }

    public Brush LiveBrush
    {
        get => _liveBrush;
        private set => SetProperty(ref _liveBrush, value);
    }

    public bool IsStatusOpen
    {
        get => _isStatusOpen;
        set => SetProperty(ref _isStatusOpen, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public InfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        private set => SetProperty(ref _statusSeverity, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string BusyText
    {
        get => _busyText;
        private set => SetProperty(ref _busyText, value);
    }

    public int MaxVisibleIndex
    {
        get => Math.Clamp(Config.MaxVisibleItems, 1, 8) - 1;
        set
        {
            if (value < 0)
            {
                return;
            }

            var count = Math.Clamp(value + 1, 1, 8);
            if (count == Config.MaxVisibleItems)
            {
                return;
            }

            _configService.Update(config => config.MaxVisibleItems = count);
            SaveConfig();
            OnPropertyChanged();
            UpdateRowStates();
        }
    }

    public int LayoutIndex
    {
        get => (int)Config.Layout;
        set
        {
            if (value < 0 || value > 1 || value == (int)Config.Layout)
            {
                return;
            }

            _configService.Update(config => config.Layout = (LayoutMode)value);
            SaveConfig();
            OnPropertyChanged();
        }
    }

    public int FontSizeIndex
    {
        get => (int)Config.FontSize;
        set
        {
            if (value < 0 || value > 3 || value == (int)Config.FontSize)
            {
                return;
            }

            _configService.Update(config => config.FontSize = (FontSizeOption)value);
            SaveConfig();
            OnPropertyChanged();
        }
    }

    public int ColorSchemeIndex
    {
        get => (int)Config.ColorScheme;
        set
        {
            if (value < 0 || value > 1 || value == (int)Config.ColorScheme)
            {
                return;
            }

            _configService.Update(config => config.ColorScheme = (ColorScheme)value);
            SaveConfig();
            OnPropertyChanged();
            foreach (var row in Items)
            {
                row.ReapplyColorScheme(Config.ColorScheme);
            }
        }
    }

    public int PositionIndex
    {
        get => (int)Config.PositionMode;
        set
        {
            if (value < 0 || value > 1 || value == (int)Config.PositionMode)
            {
                return;
            }

            _configService.Update(config => config.PositionMode = (PositionMode)value);
            SaveConfig();
            OnPropertyChanged();
        }
    }

    public int AnchorIndex
    {
        get => (int)Config.Anchor;
        set
        {
            if (value < 0 || value > 1 || value == (int)Config.Anchor)
            {
                return;
            }

            _configService.Update(config => config.Anchor = (AnchorMode)value);
            SaveConfig();
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => Config.StartWithWindows;
        set
        {
            if (value == Config.StartWithWindows)
            {
                return;
            }

            _configService.Update(config => config.StartWithWindows = value);
            SaveConfig();
            OnPropertyChanged();
        }
    }

    public int ProxyModeIndex
    {
        get => (int)Config.ProxyMode;
        set
        {
            if (value < 0 || value > 2 || value == (int)Config.ProxyMode)
            {
                return;
            }

            _configService.Update(config => config.ProxyMode = (NetworkProxyMode)value);
            SaveNetworkSettings();
        }
    }

    public bool IsCustomProxy => Config.ProxyMode == NetworkProxyMode.Custom;

    public string ProxyUrl => Config.ProxyUrl;

    public string RestBaseUrl => Config.RestBaseUrl;

    public string WsUrl => Config.WsUrl;

    public string ConnectionTestStatus
    {
        get => _connectionTestStatus;
        private set => SetProperty(ref _connectionTestStatus, value);
    }

    public bool IsTestingConnection
    {
        get => _isTestingConnection;
        private set => SetProperty(ref _isTestingConnection, value);
    }

    private AppConfig Config => _configService.Config;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyText = "正在加载币种目录…";

        try
        {
            var spotTask = _catalog.GetCatalogAsync(Market.Spot, false, _cts.Token);
            var futuresTask = _catalog.GetCatalogAsync(Market.UsdFutures, false, _cts.Token);
            await Task.WhenAll(spotTask, futuresTask);

            var spot = await spotTask;
            var futures = await futuresTask;
            _catalogs[Market.Spot] = spot.Entries;
            _catalogs[Market.UsdFutures] = futures.Entries;

            var warning = spot.Warning ?? futures.Warning;
            CatalogStatus = $"现货 {spot.Entries.Count} 个 · 永续 {futures.Entries.Count} 个"
                + (warning is null ? string.Empty : $"（{warning}）");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            CatalogStatus = "目录加载失败：" + ex.Message;
            _log.Error("Failed to load the Bitget catalog.", ex);
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshQuotesAsync();
        _liveTimer.Start();
    }

    public async Task RefreshCatalogsAsync()
    {
        IsBusy = true;
        BusyText = "正在刷新币种目录…";
        try
        {
            var spot = await _catalog.GetCatalogAsync(Market.Spot, true, _cts.Token);
            var futures = await _catalog.GetCatalogAsync(Market.UsdFutures, true, _cts.Token);
            _catalogs[Market.Spot] = spot.Entries;
            _catalogs[Market.UsdFutures] = futures.Entries;
            CatalogStatus = $"目录已刷新：现货 {spot.Entries.Count} 个 · 永续 {futures.Entries.Count} 个";
            ShowStatus("币种目录已刷新", InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            CatalogStatus = "刷新失败：" + ex.Message;
            ShowStatus("目录刷新失败：" + ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<IReadOnlyList<CatalogEntry>> GetCatalogAsync(Market market, bool force)
    {
        if (!force && _catalogs.TryGetValue(market, out var cached))
        {
            return cached;
        }

        var result = await _catalog.GetCatalogAsync(market, force, _cts.Token);
        _catalogs[market] = result.Entries;
        return result.Entries;
    }

    public bool Contains(CatalogEntry entry)
    {
        var key = InstrumentKey.Create(entry.Market, entry.Symbol);
        return Config.Items.Any(item => item.Key == key);
    }

    public void AddItem(CatalogEntry entry)
    {
        var item = new TickerItemConfig
        {
            Market = entry.Market,
            Symbol = entry.Symbol,
            Precision = entry.Precision
        };

        if (Config.Items.Any(existing => existing.Key == item.Key))
        {
            ShowStatus("该币种已在列表中", InfoBarSeverity.Warning);
            return;
        }

        _configService.Update(config => config.Items.Add(item));
        SaveConfig();
        RebuildRows();
        _ = RefreshQuotesAsync();
    }

    public void AddItem(TickerItemConfig item)
    {
        if (Config.Items.Any(existing => existing.Key == item.Key))
        {
            ShowStatus("该币种已在列表中", InfoBarSeverity.Warning);
            return;
        }

        _configService.Update(config => config.Items.Add(item));
        SaveConfig();
        RebuildRows();
        _ = RefreshQuotesAsync();
    }

    public void Remove(ItemRowViewModel row)
    {
        _configService.Update(config => config.Items.Remove(row.Item));
        SaveConfig();
        RebuildRows();
    }

    public void Move(ItemRowViewModel row, int delta)
    {
        var index = Config.Items.IndexOf(row.Item);
        var target = index + delta;
        if (index < 0 || target < 0 || target >= Config.Items.Count)
        {
            return;
        }

        _configService.Update(config =>
        {
            var item = config.Items[index];
            config.Items.RemoveAt(index);
            config.Items.Insert(target, item);
        });

        SaveConfig();
        RebuildRows();
    }

    public void RestoreDefaults()
    {
        var defaults = ConfigDefaults.Create();
        _configService.Update(config =>
        {
            config.MaxVisibleItems = defaults.MaxVisibleItems;
            config.Layout = defaults.Layout;
            config.FontSize = defaults.FontSize;
            config.ColorScheme = defaults.ColorScheme;
            config.PositionMode = defaults.PositionMode;
            config.Anchor = defaults.Anchor;
            config.StartWithWindows = defaults.StartWithWindows;
            config.ProxyMode = defaults.ProxyMode;
            config.ProxyUrl = defaults.ProxyUrl;
            config.RestBaseUrl = defaults.RestBaseUrl;
            config.WsUrl = defaults.WsUrl;
            config.Items = defaults.Items.Select(item => item.Clone()).ToList();
        });

        _network.Apply(Config);
        SaveConfig();
        RebuildRows();
        OnPropertyChanged(nameof(MaxVisibleIndex));
        OnPropertyChanged(nameof(LayoutIndex));
        OnPropertyChanged(nameof(FontSizeIndex));
        OnPropertyChanged(nameof(ColorSchemeIndex));
        OnPropertyChanged(nameof(PositionIndex));
        OnPropertyChanged(nameof(AnchorIndex));
        OnPropertyChanged(nameof(StartWithWindows));
        NotifyNetworkProperties();
        _ = RefreshQuotesAsync();
    }

    public bool SetProxyUrl(string? value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length > 0)
        {
            if (!NetworkEndpointSettings.TryParseProxy(text, out var parsed) || parsed is null)
            {
                ShowStatus(
                    "代理地址格式无效，示例：http://127.0.0.1:7890 或 socks5://127.0.0.1:7891",
                    InfoBarSeverity.Warning);
                OnPropertyChanged(nameof(ProxyUrl));
                return false;
            }

            text = parsed.ToString();
        }

        if (string.Equals(text, Config.ProxyUrl, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(ProxyUrl));
            return true;
        }

        _configService.Update(config => config.ProxyUrl = text);
        SaveNetworkSettings();
        return true;
    }

    public bool SetRestBaseUrl(string? value)
    {
        if (!NetworkEndpointSettings.TryNormalizeRestBaseUrl(value, out var normalized))
        {
            ShowStatus("REST 地址无效，示例：https://api.bitget.com", InfoBarSeverity.Warning);
            OnPropertyChanged(nameof(RestBaseUrl));
            return false;
        }

        if (string.Equals(normalized, Config.RestBaseUrl, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(RestBaseUrl));
            return true;
        }

        _configService.Update(config => config.RestBaseUrl = normalized);
        SaveNetworkSettings();
        return true;
    }

    public bool SetWsUrl(string? value)
    {
        if (!NetworkEndpointSettings.TryNormalizeWsUrl(value, out var normalized))
        {
            ShowStatus("WebSocket 地址无效，示例：wss://ws.bitget.com/v2/ws/public", InfoBarSeverity.Warning);
            OnPropertyChanged(nameof(WsUrl));
            return false;
        }

        if (string.Equals(normalized, Config.WsUrl, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(WsUrl));
            return true;
        }

        _configService.Update(config => config.WsUrl = normalized);
        SaveNetworkSettings();
        return true;
    }

    public void RestoreNetworkDefaults()
    {
        var defaults = ConfigDefaults.Create();
        _configService.Update(config =>
        {
            config.ProxyMode = defaults.ProxyMode;
            config.ProxyUrl = defaults.ProxyUrl;
            config.RestBaseUrl = defaults.RestBaseUrl;
            config.WsUrl = defaults.WsUrl;
        });

        SaveNetworkSettings();
        ShowStatus("网络设置已恢复默认", InfoBarSeverity.Success);
    }

    public async Task TestConnectionAsync()
    {
        if (IsTestingConnection || _disposed)
        {
            return;
        }

        IsTestingConnection = true;
        ConnectionTestStatus = "正在连接 Bitget…";
        var watch = Stopwatch.StartNew();
        try
        {
            var quote = await _rest.GetTickerAsync(
                InstrumentKey.Create(Market.Spot, "BTCUSDT"),
                _cts.Token);

            watch.Stop();
            ConnectionTestStatus = $"连接成功 · {watch.ElapsedMilliseconds} ms · BTC/USDT {quote.Price:0.##}";
            ShowStatus("Bitget 连接正常", InfoBarSeverity.Success);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            watch.Stop();
            ConnectionTestStatus = $"连接失败 · {watch.ElapsedMilliseconds} ms · {ex.Message}";
            ShowStatus("Bitget 连接失败：" + ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    private void SaveNetworkSettings()
    {
        _network.Apply(Config);
        SaveConfig();
        NotifyNetworkProperties();
    }

    private void NotifyNetworkProperties()
    {
        OnPropertyChanged(nameof(ProxyModeIndex));
        OnPropertyChanged(nameof(IsCustomProxy));
        OnPropertyChanged(nameof(ProxyUrl));
        OnPropertyChanged(nameof(RestBaseUrl));
        OnPropertyChanged(nameof(WsUrl));
    }

    public void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _liveTimer.Stop();
        _statusTimer.Stop();
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task RefreshQuotesAsync()
    {
        if (_refreshing || _disposed || Items.Count == 0)
        {
            return;
        }

        _refreshing = true;
        try
        {
            var rows = Items.ToList();
            var tasks = rows.Select(FetchAsync).ToArray();
            var quotes = await Task.WhenAll(tasks);

            var newest = quotes
                .Where(quote => quote is not null)
                .Select(quote => quote!)
                .OrderByDescending(quote => quote.ReceivedAt)
                .FirstOrDefault();

            if (newest is null)
            {
                LiveStatus = "等待行情";
                LiveBrush = IdleBrushValue;
            }
            else if (newest.IsStale(DateTimeOffset.UtcNow))
            {
                LiveStatus = "数据延迟";
                LiveBrush = WarnBrushValue;
            }
            else
            {
                LiveStatus = "实时";
                LiveBrush = LiveBrushValue;
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task<TickerQuote?> FetchAsync(ItemRowViewModel row)
    {
        try
        {
            var quote = await _rest.GetTickerAsync(row.Item.Key, _cts.Token);
            row.ApplyQuote(quote, Config.ColorScheme);
            return quote;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to refresh {row.Item.Key}.", ex);
            row.ApplyQuote(null, Config.ColorScheme);
            return null;
        }
    }

    private void ChangeMarket(ItemRowViewModel row, Market market)
    {
        var precision = ResolvePrecision(market, row.Item.Symbol);
        _configService.Update(config =>
        {
            var target = config.Items.FirstOrDefault(item => ReferenceEquals(item, row.Item));
            if (target is null)
            {
                return;
            }

            target.Market = market;
            target.Precision = precision;
        });

        row.ApplyMarket(market, precision);
        SaveConfig();
        _ = RefreshQuotesAsync();
    }

    private int ResolvePrecision(Market market, string symbol)
    {
        if (_catalog.TryGetCachedEntry(market, symbol, out var entry) && entry is not null)
        {
            return entry.Precision;
        }

        return market == Market.Spot ? 2 : 1;
    }

    private void RebuildRows()
    {
        Items.Clear();
        for (var index = 0; index < Config.Items.Count; index++)
        {
            Items.Add(new ItemRowViewModel(index, Config.Items[index], ChangeMarket));
        }

        UpdateRowStates();
    }

    private void UpdateRowStates()
    {
        var max = Config.MaxVisibleItems;
        for (var index = 0; index < Items.Count; index++)
        {
            var row = Items[index];
            row.Index = index;
            row.CanMoveUp = index > 0;
            row.CanMoveDown = index < Items.Count - 1;
            row.IsBeyondLimit = index >= max;
        }
    }

    private void SaveConfig()
    {
        _configService.SaveNow();
        ShowStatus("已保存，任务栏已同步", InfoBarSeverity.Success);
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
        => new(Color.FromArgb(0xFF, red, green, blue));
}
