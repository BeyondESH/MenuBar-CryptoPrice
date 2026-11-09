using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class MarketDataServiceTests
{
    private sealed class FakeRestClient : ITickerRestClient
    {
        public int Calls;

        public Task<TickerQuote> GetTickerAsync(InstrumentKey key, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new TickerQuote(key, 100m, 0.01m, 101m, 99m, null, null, now, now));
        }
    }

    private sealed class FakeWsClient : IWsClient
    {
        public event Action<TickerQuote>? QuoteReceived;

        public event Action<bool>? ConnectionChanged;

        public event Action<string, Exception?>? Error;

        public bool IsConnected { get; private set; }

        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public void Start(IReadOnlyCollection<InstrumentKey> keys) => Started = true;

        public void Stop() => Stopped = true;

        public void Dispose()
        {
        }

        public void SetConnected(bool connected)
        {
            IsConnected = connected;
            ConnectionChanged?.Invoke(connected);
        }

        public void Push(TickerQuote quote) => QuoteReceived?.Invoke(quote);

        public void RaiseError(string source, Exception? exception) => Error?.Invoke(source, exception);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(condition(), "The expected condition was not reached in time.");
    }

    [Fact]
    public async Task Start_LoadsInitialSnapshot_ThenPollsWhileWebSocketIsDown_AndStopsAfterRecovery()
    {
        var rest = new FakeRestClient();
        var ws = new FakeWsClient();
        var store = new TickerStore();
        var options = new MarketDataOptions(TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(40))
        {
            LoopTick = TimeSpan.FromMilliseconds(20)
        };

        using var service = new MarketDataService(rest, () => ws, store, options: options);
        var keys = new[]
        {
            InstrumentKey.Create(Market.Spot, "BTCUSDT"),
            InstrumentKey.Create(Market.UsdFutures, "BTCUSDT")
        };

        service.Start(keys);

        await WaitUntilAsync(() => ws.Started, TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => Volatile.Read(ref rest.Calls) >= keys.Length, TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => store.Get(keys[0]) is not null, TimeSpan.FromSeconds(1));

        // WebSocket never reports connected, so the fallback must poll REST.
        await WaitUntilAsync(() => Volatile.Read(ref rest.Calls) >= keys.Length + 1, TimeSpan.FromSeconds(2));

        ws.SetConnected(true);
        await Task.Delay(200);
        var callsAfterRecovery = Volatile.Read(ref rest.Calls);
        await Task.Delay(300);

        Assert.Equal(callsAfterRecovery, Volatile.Read(ref rest.Calls));
    }

    [Fact]
    public async Task PollsRest_WhenWebSocketIsConnectedButQuotesAreStale()
    {
        var rest = new FakeRestClient();
        var ws = new FakeWsClient();
        var options = new MarketDataOptions(TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(40))
        {
            LoopTick = TimeSpan.FromMilliseconds(20),
            StaleAfter = TimeSpan.FromMilliseconds(80)
        };

        using var service = new MarketDataService(rest, () => ws, new TickerStore(), options: options);
        service.Start(new[] { InstrumentKey.Create(Market.Spot, "BTCUSDT") });

        await WaitUntilAsync(() => ws.Started, TimeSpan.FromSeconds(1));
        ws.SetConnected(true);
        await WaitUntilAsync(() => Volatile.Read(ref rest.Calls) >= 2, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task QuoteReceived_UpdatesStore()
    {
        var rest = new FakeRestClient();
        var ws = new FakeWsClient();
        var store = new TickerStore();
        using var service = new MarketDataService(rest, () => ws, store);
        var key = InstrumentKey.Create(Market.Spot, "BTCUSDT");

        service.Start(new[] { key });
        await WaitUntilAsync(() => ws.Started, TimeSpan.FromSeconds(1));

        var now = DateTimeOffset.UtcNow;
        ws.Push(new TickerQuote(key, 123.45m, 0.02m, 130m, 120m, null, null, now, now));

        Assert.NotNull(store.Get(key));
        Assert.Equal(123.45m, store.Get(key)!.Price);
    }

    [Fact]
    public void Stop_StopsWebSocket()
    {
        var rest = new FakeRestClient();
        var ws = new FakeWsClient();
        using var service = new MarketDataService(rest, () => ws, new TickerStore());

        service.Start(new[] { InstrumentKey.Create(Market.Spot, "BTCUSDT") });
        service.Stop();

        Assert.True(ws.Stopped);
    }
}
