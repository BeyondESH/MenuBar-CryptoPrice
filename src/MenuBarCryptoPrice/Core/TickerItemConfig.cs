using System.Text.Json.Serialization;

namespace MenuBarCryptoPrice.Core;

public sealed class TickerItemConfig
{
    public Market Market { get; set; } = Market.Spot;

    public string Symbol { get; set; } = string.Empty;

    public int Precision { get; set; } = 2;

    [JsonIgnore]
    public InstrumentKey Key => InstrumentKey.Create(Market, Symbol);

    [JsonIgnore]
    public string BaseCoin
    {
        get
        {
            var symbol = (Symbol ?? string.Empty).Trim().ToUpperInvariant();
            foreach (var quote in new[] { "USDT", "USDC", "USD" })
            {
                if (symbol.Length > quote.Length && symbol.EndsWith(quote, StringComparison.Ordinal))
                {
                    return symbol[..^quote.Length];
                }
            }

            return symbol;
        }
    }

    [JsonIgnore]
    public string QuoteCoin
    {
        get
        {
            var symbol = (Symbol ?? string.Empty).Trim().ToUpperInvariant();
            foreach (var quote in new[] { "USDT", "USDC", "USD" })
            {
                if (symbol.Length > quote.Length && symbol.EndsWith(quote, StringComparison.Ordinal))
                {
                    return quote;
                }
            }

            return "USDT";
        }
    }

    public TickerItemConfig Clone() => new()
    {
        Market = Market,
        Symbol = Symbol,
        Precision = Precision
    };
}
