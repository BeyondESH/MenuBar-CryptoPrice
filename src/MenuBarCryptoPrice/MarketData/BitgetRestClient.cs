using System.Net.Http;
using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public interface ITickerRestClient
{
    Task<TickerQuote> GetTickerAsync(InstrumentKey key, CancellationToken cancellationToken);
}

public sealed class BitgetRestClient : ITickerRestClient
{
    private readonly BitgetNetwork _network;
    private readonly HttpClient? _httpOverride;

    public BitgetRestClient(BitgetNetwork? network = null, HttpClient? httpClient = null)
    {
        _network = network ?? BitgetNetwork.Shared;
        _httpOverride = httpClient;
    }

    public async Task<TickerQuote> GetTickerAsync(InstrumentKey key, CancellationToken cancellationToken)
    {
        var baseUrl = _network.Settings.RestBaseUri.ToString().TrimEnd('/');
        var url = key.Market == Market.Spot
            ? $"{baseUrl}/api/v2/spot/market/tickers?symbol={Uri.EscapeDataString(key.Symbol)}"
            : $"{baseUrl}/api/v2/mix/market/ticker?symbol={Uri.EscapeDataString(key.Symbol)}&productType=USDT-FUTURES";

        var http = _httpOverride ?? _network.TickerHttp;
        string json;
        try
        {
            json = await HttpRetry.GetStringAsync(http, url, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
        {
            throw new HttpRequestException($"Bitget request failed for {key} at {baseUrl}: {ex.Message}", ex);
        }

        return TickerSnapshotParser.ParseRest(json, key, DateTimeOffset.UtcNow);
    }
}
