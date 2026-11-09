using System.Net;
using System.Net.Http;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.MarketData;

public sealed class BitgetNetwork : IDisposable
{
    public const string UserAgent = "MenuBarCryptoPrice/1.0";

    private static readonly TimeSpan RetiredClientLinger = TimeSpan.FromSeconds(30);
    private static readonly Lazy<BitgetNetwork> SharedInstance = new(() => new BitgetNetwork());

    private readonly ILog _log;
    private readonly object _gate = new();

    private NetworkEndpointSettings _settings;
    private SocketsHttpHandler? _handler;
    private HttpClient? _tickerHttp;
    private HttpClient? _catalogHttp;
    private bool _disposed;

    public BitgetNetwork(ILog? log = null)
    {
        _log = log ?? NullLog.Instance;
        _settings = NetworkEndpointSettings.FromConfig(new AppConfig());
        (_handler, _tickerHttp, _catalogHttp) = CreateClients(_settings);
    }

    public static BitgetNetwork Shared => SharedInstance.Value;

    public NetworkEndpointSettings Settings
    {
        get
        {
            lock (_gate)
            {
                return _settings;
            }
        }
    }

    public HttpClient TickerHttp
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _tickerHttp!;
            }
        }
    }

    public HttpClient CatalogHttp
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _catalogHttp!;
            }
        }
    }

    public void Apply(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var next = NetworkEndpointSettings.FromConfig(config);
        HttpClient? oldTicker;
        HttpClient? oldCatalog;
        SocketsHttpHandler? oldHandler;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_settings == next)
            {
                return;
            }

            oldTicker = _tickerHttp;
            oldCatalog = _catalogHttp;
            oldHandler = _handler;
            _settings = next;
            (_handler, _tickerHttp, _catalogHttp) = CreateClients(next);
        }

        Retire(oldTicker, oldCatalog, oldHandler);
        _log.Info(
            $"Bitget network applied: proxy={next.ProxyMode}, rest={next.RestBaseUri.GetLeftPart(UriPartial.Path)}, "
            + $"ws={next.WsUri.GetLeftPart(UriPartial.Path)}.");
    }

    public void Dispose()
    {
        HttpClient? ticker;
        HttpClient? catalog;
        SocketsHttpHandler? handler;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ticker = _tickerHttp;
            catalog = _catalogHttp;
            handler = _handler;
            _tickerHttp = null;
            _catalogHttp = null;
            _handler = null;
        }

        ticker?.Dispose();
        catalog?.Dispose();
        handler?.Dispose();
    }

    private static (SocketsHttpHandler Handler, HttpClient Ticker, HttpClient Catalog) CreateClients(
        NetworkEndpointSettings settings)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(8),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = settings.ProxyMode != NetworkProxyMode.Direct,
            Proxy = settings.ProxyMode == NetworkProxyMode.Custom && settings.ProxyUri is not null
                ? new WebProxy(settings.ProxyUri)
                : null
        };

        var ticker = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(7) };
        var catalog = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(20) };
        ticker.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        catalog.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return (handler, ticker, catalog);
    }

    private static void Retire(HttpClient? ticker, HttpClient? catalog, SocketsHttpHandler? handler)
    {
        if (ticker is null || catalog is null || handler is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(RetiredClientLinger).ConfigureAwait(false);
            ticker.Dispose();
            catalog.Dispose();
            handler.Dispose();
        });
    }
}
