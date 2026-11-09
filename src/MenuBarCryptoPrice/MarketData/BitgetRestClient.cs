using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public interface ITickerRestClient
{
    Task<TickerQuote> GetTickerAsync(InstrumentKey key, CancellationToken cancellationToken);
}

public sealed class BitgetRestClient : ITickerRestClient
{
    private static readonly HttpClient Http = CreateHttpClient();

    public async Task<TickerQuote> GetTickerAsync(InstrumentKey key, CancellationToken cancellationToken)
    {
        var url = key.Market == Market.Spot
            ? $"https://api.bitget.com/api/v2/spot/market/tickers?symbol={Uri.EscapeDataString(key.Symbol)}"
            : $"https://api.bitget.com/api/v2/mix/market/ticker?symbol={Uri.EscapeDataString(key.Symbol)}&productType=USDT-FUTURES";

        using var response = await Http
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Bitget HTTP {(int)response.StatusCode} for {key}.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return TickerSnapshotParser.ParseRest(json, key, DateTimeOffset.UtcNow);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MenuBarCryptoPrice/1.0");
        return client;
    }
}
