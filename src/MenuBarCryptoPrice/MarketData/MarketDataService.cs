using System.Net.WebSockets;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.MarketData;

public sealed record MarketDataOptions(TimeSpan FallbackAfter, TimeSpan PollInterval)
{
    public TimeSpan LoopTick { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan StaleAfter { get; init; } = TimeSpan.FromSeconds(30);

    public static MarketDataOptions Default { get; } = new(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(5));
}

public sealed class MarketDataService : IDisposable
{
    private readonly ITickerRestClient _rest;
    private readonly Func<IWsClient> _wsFactory;
    private readonly TickerStore _store;
    private readonly ILog _log;
    private readonly MarketDataOptions _options;
    private readonly object _gate = new();

    private IWsClient? _ws;
    private CancellationTokenSource? _cts;
    private InstrumentKey[] _keys = Array.Empty<InstrumentKey>();
    private bool _wsConnected;
    private DateTimeOffset _wsDownSince = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPoll = DateTimeOffset.MinValue;
    private bool _disposed;

    public MarketDataService(
        ITickerRestClient rest,
        Func<IWsClient> wsFactory,
        TickerStore store,
        ILog? log = null,
        MarketDataOptions? options = null)
    {
        _rest = rest;
        _wsFactory = wsFactory;
        _store = store;
        _log = log ?? NullLog.Instance;
        _options = options ?? MarketDataOptions.Default;
    }

    public bool IsWebSocketConnected => _wsConnected;

    public void Start(IReadOnlyCollection<InstrumentKey> keys)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();

        _keys = keys.Distinct().ToArray();
        if (_keys.Length == 0)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        _wsConnected = false;
        _wsDownSince = DateTimeOffset.UtcNow;
        _lastPoll = DateTimeOffset.MinValue;

        var ws = _wsFactory();
        ws.QuoteReceived += OnQuoteReceived;
        ws.ConnectionChanged += OnConnectionChanged;
        ws.Error += OnWsError;
        lock (_gate)
        {
            _ws = ws;
        }

        ws.Start(_keys);
        _ = Task.Run(() => RunAsync(cts.Token));
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        IWsClient? ws;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
            ws = _ws;
            _ws = null;
        }

        cts?.Cancel();
        if (ws is not null)
        {
            ws.QuoteReceived -= OnQuoteReceived;
            ws.ConnectionChanged -= OnConnectionChanged;
            ws.Error -= OnWsError;
            ws.Stop();
            ws.Dispose();
        }

        _wsConnected = false;

        if (cts is not null)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                cts.Dispose();
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private void OnQuoteReceived(TickerQuote quote) => _store.Update(quote);

    private void OnConnectionChanged(bool connected)
    {
        _wsConnected = connected;
        if (!connected)
        {
            _wsDownSince = DateTimeOffset.UtcNow;
        }

        _log.Info($"WebSocket connected={connected}.");
    }

    private void OnWsError(string source, Exception? exception)
        => _log.Warn($"WebSocket error from {source}.", exception);

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitialSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _log.Error("Initial REST snapshot loop stopped unexpectedly.", ex);
        }

        await FallbackLoopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InitialSnapshotAsync(CancellationToken cancellationToken)
    {
        var keys = _keys;
        await Task.WhenAll(keys.Select(async key =>
        {
            try
            {
                var quote = await _rest.GetTickerAsync(key, cancellationToken).ConfigureAwait(false);
                _store.Update(quote);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _log.Warn($"Initial REST snapshot failed for {key}.", ex);
            }
        })).ConfigureAwait(false);
    }

    private async Task FallbackLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                if (ShouldPoll(now) && now - _lastPoll >= _options.PollInterval)
                {
                    _lastPoll = now;
                    await PollAsync(cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(_options.LoopTick, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log.Error("REST fallback loop stopped unexpectedly.", ex);
        }
    }

    private bool ShouldPoll(DateTimeOffset now)
    {
        if (!_wsConnected)
        {
            return now - _wsDownSince >= _options.FallbackAfter;
        }

        foreach (var key in _keys)
        {
            var quote = _store.Get(key);
            if (quote is null || now - quote.ReceivedAt >= _options.StaleAfter)
            {
                return true;
            }
        }

        return false;
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        var keys = _keys;
        await Task.WhenAll(keys.Select(async key =>
        {
            try
            {
                var quote = await _rest.GetTickerAsync(key, cancellationToken).ConfigureAwait(false);
                _store.Update(quote);
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException ex)
            {
                _log.Warn($"REST fallback failed for {key}.", ex);
            }
            catch (Exception ex)
            {
                _log.Warn($"REST fallback failed for {key}.", ex);
            }
        })).ConfigureAwait(false);
    }
}
