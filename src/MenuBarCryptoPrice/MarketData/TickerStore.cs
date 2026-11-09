using System.Collections.Concurrent;
using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public sealed class TickerStore
{
    private readonly ConcurrentDictionary<InstrumentKey, TickerQuote> _quotes = new();

    public event EventHandler? Updated;

    public void Update(TickerQuote quote)
    {
        _quotes[quote.Key] = quote;
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public TickerQuote? Get(InstrumentKey key)
        => _quotes.TryGetValue(key, out var quote) ? quote : null;

    public void Clear() => _quotes.Clear();
}
