using System.Text.Json;
using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public sealed record WsTickerFrame(string Action, Market Market, string Symbol, IReadOnlyList<TickerQuote> Quotes);

public static class TickerSnapshotParser
{
    public static WsTickerFrame? ParseWsFrame(string json, DateTimeOffset receivedAt)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("action", out var actionElement) || actionElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        if (!root.TryGetProperty("arg", out var arg)
            || !arg.TryGetProperty("instType", out var instTypeElement)
            || !arg.TryGetProperty("instId", out var instIdElement))
        {
            return null;
        }

        var instType = instTypeElement.GetString() ?? string.Empty;
        var symbol = (instIdElement.GetString() ?? string.Empty).ToUpperInvariant();
        Market market;
        try
        {
            market = MarketExtensions.FromInstType(instType);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var quotes = new List<TickerQuote>();
        foreach (var item in data.EnumerateArray())
        {
            var rowSymbol = GetString(item, "instId")?.ToUpperInvariant() ?? symbol;
            if (string.IsNullOrEmpty(rowSymbol) || !TryGetDecimal(item, "lastPr", out var price))
            {
                continue;
            }

            if (!TryGetDecimal(item, "change24h", out var change))
            {
                change = 0m;
            }

            if (!TryGetDecimal(item, "high24h", out var high))
            {
                high = 0m;
            }

            if (!TryGetDecimal(item, "low24h", out var low))
            {
                low = 0m;
            }

            decimal? funding = TryGetDecimal(item, "fundingRate", out var fundingValue) ? fundingValue : null;
            decimal? mark = TryGetDecimal(item, "markPrice", out var markValue) ? markValue : null;
            var sourceTime = TryGetInt64(item, "ts", out var timestamp)
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
                : receivedAt;

            quotes.Add(new TickerQuote(
                InstrumentKey.Create(market, rowSymbol),
                price,
                change,
                high,
                low,
                funding,
                mark,
                sourceTime,
                receivedAt)
            {
                BidPrice = TryGetDecimal(item, "bidPr", out var bid) ? bid : null,
                AskPrice = TryGetDecimal(item, "askPr", out var ask) ? ask : null,
                QuoteVolume24h = TryGetDecimal(item, "quoteVolume", out var volume) ? volume : null
            });
        }

        return quotes.Count == 0 ? null : new WsTickerFrame(actionElement.GetString() ?? string.Empty, market, symbol, quotes);
    }

    public static TickerQuote ParseRest(string json, InstrumentKey key, DateTimeOffset receivedAt)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("code", out var code)
            && code.ValueKind == JsonValueKind.String
            && code.GetString() != "00000")
        {
            var message = root.TryGetProperty("msg", out var msg) ? msg.GetString() : "unknown error";
            throw new InvalidOperationException($"Bitget error for {key}: {message}");
        }

        if (!root.TryGetProperty("data", out var data))
        {
            throw new InvalidOperationException($"Bitget response for {key} has no data.");
        }

        JsonElement row;
        if (data.ValueKind == JsonValueKind.Array)
        {
            if (data.GetArrayLength() == 0)
            {
                throw new InvalidOperationException($"Bitget returned no rows for {key}.");
            }

            row = data[0];
        }
        else if (data.ValueKind == JsonValueKind.Object)
        {
            row = data;
        }
        else
        {
            throw new InvalidOperationException($"Unexpected Bitget data shape for {key}.");
        }

        if (!TryGetDecimal(row, "lastPr", out var price))
        {
            throw new InvalidOperationException($"Bitget returned no lastPr for {key}.");
        }

        TryGetDecimal(row, "change24h", out var change);
        TryGetDecimal(row, "high24h", out var high);
        TryGetDecimal(row, "low24h", out var low);
        decimal? funding = TryGetDecimal(row, "fundingRate", out var fundingValue) ? fundingValue : null;
        decimal? mark = TryGetDecimal(row, "markPrice", out var markValue) ? markValue : null;
        var sourceTime = TryGetInt64(row, "ts", out var timestamp)
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : receivedAt;

        return new TickerQuote(key, price, change, high, low, funding, mark, sourceTime, receivedAt)
        {
            BidPrice = TryGetDecimal(row, "bidPr", out var bid) ? bid : null,
            AskPrice = TryGetDecimal(row, "askPr", out var ask) ? ask : null,
            QuoteVolume24h = TryGetDecimal(row, "quoteVolume", out var volume) ? volume : null
        };
    }

    public static bool IsPong(string json) => json.Trim().Equals("pong", StringComparison.OrdinalIgnoreCase);

    public static bool IsSubscribeAck(string json)
        => json.Contains("\"event\":\"subscribe\"", StringComparison.Ordinal);

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool TryGetDecimal(JsonElement element, string name, out decimal value)
    {
        value = 0m;
        if (!element.TryGetProperty(name, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value),
            JsonValueKind.Number => property.TryGetDecimal(out value),
            _ => false
        };
    }

    private static bool TryGetInt64(JsonElement element, string name, out long value)
    {
        value = 0;
        if (!element.TryGetProperty(name, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            JsonValueKind.Number => property.TryGetInt64(out value),
            _ => false
        };
    }
}
