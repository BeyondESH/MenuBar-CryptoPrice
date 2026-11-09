using System.Net.WebSockets;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.MarketData;

public interface IWsClient : IDisposable
{
    event Action<TickerQuote>? QuoteReceived;

    event Action<bool>? ConnectionChanged;

    event Action<string, Exception?>? Error;

    bool IsConnected { get; }

    void Start(IReadOnlyCollection<InstrumentKey> keys);

    void Stop();
}

public sealed class BitgetWsClient : IWsClient
{
    private const string Endpoint = "wss://ws.bitget.com/v2/ws/public";

    private readonly ILog _log;
    private readonly Random _random = new();
    private readonly object _gate = new();
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private bool _connected;
    private bool _disposed;

    public BitgetWsClient(ILog? log = null)
    {
        _log = log ?? NullLog.Instance;
    }

    public event Action<TickerQuote>? QuoteReceived;

    public event Action<bool>? ConnectionChanged;

    public event Action<string, Exception?>? Error;

    public bool IsConnected => _connected;

    public void Start(IReadOnlyCollection<InstrumentKey> keys)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();

        var unique = keys.Distinct().ToArray();
        if (unique.Length == 0)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = Task.Run(() => RunAsync(unique, cts.Token));
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _cts;
            _cts = null;
            try
            {
                _socket?.Abort();
            }
            catch
            {
                // Aborting a disposed socket is harmless.
            }

            _socket = null;
        }

        cts?.Cancel();
        if (cts is not null)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                cts.Dispose();
            });
        }

        SetConnected(false);
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

    private async Task RunAsync(InstrumentKey[] keys, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var connectedAt = DateTimeOffset.MinValue;
            try
            {
                using var socket = new ClientWebSocket();
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                await socket.ConnectAsync(new Uri(Endpoint), cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    _socket = socket;
                }

                await SubscribeAsync(socket, keys, cancellationToken).ConfigureAwait(false);
                connectedAt = DateTimeOffset.UtcNow;
                SetConnected(true);
                _log.Info($"Bitget WebSocket connected; subscribed {keys.Length} instrument(s).");

                var lastFrame = DateTimeOffset.UtcNow;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var receiveTask = ReceiveLoopAsync(socket, () => lastFrame = DateTimeOffset.UtcNow, linked.Token);
                var keepAliveTask = KeepAliveAsync(socket, () => lastFrame, linked.Token);

                var completed = await Task.WhenAny(receiveTask, keepAliveTask).ConfigureAwait(false);
                linked.Cancel();
                await completed.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Warn("Bitget WebSocket loop failed.", ex);
                Error?.Invoke("websocket", ex);
            }
            finally
            {
                lock (_gate)
                {
                    _socket = null;
                }

                SetConnected(false);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (connectedAt != DateTimeOffset.MinValue
                && DateTimeOffset.UtcNow - connectedAt > TimeSpan.FromSeconds(15))
            {
                attempt = 0;
            }

            var delay = Backoff.GetDelay(attempt++, _random);
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task SubscribeAsync(ClientWebSocket socket, InstrumentKey[] keys, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            op = "subscribe",
            args = keys.Select(key => new
            {
                instType = key.Market.ToInstType(),
                channel = "ticker",
                instId = key.Symbol
            }).ToArray()
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, Action onFrame, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var builder = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("Bitget closed the WebSocket connection.");
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage)
            {
                continue;
            }

            var message = builder.ToString();
            builder.Clear();
            onFrame();

            if (TickerSnapshotParser.IsPong(message) || TickerSnapshotParser.IsSubscribeAck(message))
            {
                continue;
            }

            var frame = TickerSnapshotParser.ParseWsFrame(message, DateTimeOffset.UtcNow);
            if (frame is null)
            {
                continue;
            }

            foreach (var quote in frame.Quotes)
            {
                QuoteReceived?.Invoke(quote);
            }
        }
    }

    private async Task KeepAliveAsync(ClientWebSocket socket, Func<DateTimeOffset> lastFrame, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (DateTimeOffset.UtcNow - lastFrame() > TimeSpan.FromSeconds(60))
            {
                throw new TimeoutException("No WebSocket frames received for 60 seconds.");
            }

            var payload = Encoding.UTF8.GetBytes("ping");
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
    }

    private void SetConnected(bool value)
    {
        if (_connected == value)
        {
            return;
        }

        _connected = value;
        ConnectionChanged?.Invoke(value);
    }
}
