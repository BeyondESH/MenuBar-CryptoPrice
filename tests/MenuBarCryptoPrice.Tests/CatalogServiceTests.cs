using System.Net;
using System.Net.Http;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class CatalogServiceTests
{
    private const string SpotCatalogJson = """
        {"code":"00000","msg":"success","data":[
          {"symbol":"BTCUSDT","baseCoin":"BTC","quoteCoin":"USDT","status":"online","pricePrecision":"2"},
          {"symbol":"ETHUSDT","baseCoin":"ETH","quoteCoin":"USDT","status":"offline","pricePrecision":"2"}
        ]}
        """;

    private const string FuturesCatalogJson = """
        {"code":"00000","msg":"success","data":[
          {"symbol":"BTCUSDT","baseCoin":"BTC","quoteCoin":"USDT","pricePlace":"1"}
        ]}
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Responder(request));
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    [Fact]
    public void ParseCatalog_Spot_MapsStatusAndPrecision()
    {
        var file = CatalogService.ParseCatalog(Market.Spot, SpotCatalogJson, DateTimeOffset.UtcNow);

        Assert.Equal(2, file.Entries.Count);
        var btc = file.Entries.Single(entry => entry.Symbol == "BTCUSDT");
        Assert.True(btc.IsOnline);
        Assert.Equal(2, btc.Precision);
        Assert.Equal("BTC/USDT", btc.DisplayName);
        var eth = file.Entries.Single(entry => entry.Symbol == "ETHUSDT");
        Assert.False(eth.IsOnline);
    }

    [Fact]
    public void ParseCatalog_Futures_UsesPricePlace()
    {
        var file = CatalogService.ParseCatalog(Market.UsdFutures, FuturesCatalogJson, DateTimeOffset.UtcNow);

        var entry = Assert.Single(file.Entries);
        Assert.Equal(Market.UsdFutures, entry.Market);
        Assert.Equal(1, entry.Precision);
        Assert.True(entry.IsOnline);
    }

    [Fact]
    public async Task GetCatalog_CachesAndServesStaleCopyWhenOffline()
    {
        using var temp = new TempDirectory();
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        var handler = new StubHandler { Responder = _ => JsonResponse(SpotCatalogJson) };
        using var http = new HttpClient(handler);
        var service = new CatalogService(
            temp.Path,
            NullLog.Instance,
            TimeSpan.FromHours(24),
            () => now,
            http);

        var fresh = await service.GetCatalogAsync(Market.Spot, false, CancellationToken.None);
        Assert.False(fresh.IsStale);
        Assert.Equal(2, fresh.Entries.Count);
        Assert.True(File.Exists(temp.File("spot-symbols.json")));
        Assert.True(service.HasCatalog(Market.Spot));
        Assert.True(service.TryGetCachedEntry(Market.Spot, "BTCUSDT", out var cached));
        Assert.NotNull(cached);

        now = now.AddHours(25);
        handler.Responder = _ => throw new HttpRequestException("offline");

        var stale = await service.GetCatalogAsync(Market.Spot, false, CancellationToken.None);

        Assert.True(stale.IsStale);
        Assert.NotNull(stale.Warning);
        Assert.Equal(2, stale.Entries.Count);
    }
}
