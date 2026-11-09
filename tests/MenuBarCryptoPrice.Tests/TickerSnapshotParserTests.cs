using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class TickerSnapshotParserTests
{
    private const string SpotWsSnapshot = """
        {"action":"snapshot","arg":{"instType":"SPOT","channel":"ticker","instId":"BTCUSDT"},"data":[{"instId":"BTCUSDT","lastPr":"74070.01","open24h":"76065.61","high24h":"76196.57","low24h":"73481.3","change24h":"-0.02413","bidPr":"74070.01","askPr":"74070.02","bidSz":"0.072228","askSz":"0.240812","baseVolume":"1861.497713","quoteVolume":"139507391.549847","openUtc":"74854.18","changeUtc24h":"-0.01048","ts":"1794210164967"}],"ts":1794210164969}
        """;

    private const string FuturesWsSnapshot = """
        {"action":"snapshot","arg":{"instType":"USDT-FUTURES","channel":"ticker","instId":"BTCUSDT"},"data":[{"instId":"BTCUSDT","lastPr":"73942","bidPr":"73942","askPr":"73942.1","bidSz":"0.9063","askSz":"12.7586","open24h":"75903.0","high24h":"76160","low24h":"73408.8","change24h":"-0.02584","fundingRate":"0.0001","nextFundingTime":"1794211200000","markPrice":"73942.4","indexPrice":"73964.9771739130434785","holdingAmount":"37611.85","ts":"1794210200000"}],"ts":1794210200001}
        """;

    [Fact]
    public void ParseWsFrame_SpotSnapshot()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var frame = TickerSnapshotParser.ParseWsFrame(SpotWsSnapshot, receivedAt);

        Assert.NotNull(frame);
        Assert.Equal("snapshot", frame!.Action);
        Assert.Equal(Market.Spot, frame.Market);
        Assert.Single(frame.Quotes);
        var quote = frame.Quotes[0];
        Assert.Equal(InstrumentKey.Create(Market.Spot, "BTCUSDT"), quote.Key);
        Assert.Equal(74070.01m, quote.Price);
        Assert.Equal(-0.02413m, quote.Change24h);
        Assert.Equal(76196.57m, quote.High24h);
        Assert.Equal(73481.3m, quote.Low24h);
        Assert.Null(quote.FundingRate);
        Assert.Equal(receivedAt, quote.ReceivedAt);
        Assert.Equal(74070.01m, quote.BidPrice);
        Assert.Equal(74070.02m, quote.AskPrice);
        Assert.Equal(139507391.549847m, quote.QuoteVolume24h);
    }

    [Fact]
    public void ParseWsFrame_FuturesSnapshot_IncludesFundingAndMark()
    {
        var frame = TickerSnapshotParser.ParseWsFrame(FuturesWsSnapshot, DateTimeOffset.UtcNow);

        Assert.NotNull(frame);
        Assert.Equal(Market.UsdFutures, frame!.Market);
        var quote = frame.Quotes[0];
        Assert.Equal(73942m, quote.Price);
        Assert.Equal(0.0001m, quote.FundingRate);
        Assert.Equal(73942.4m, quote.MarkPrice);
    }

    [Fact]
    public void ParseRest_SpotPayload()
    {
        const string json = """
            {"code":"00000","msg":"success","requestTime":1794210121967,"data":[{"open":"75895","symbol":"BTCUSDT","high24h":"76196.57","low24h":"73481.3","lastPr":"74094.9","quoteVolume":"139529756.66","baseVolume":"1861.78","usdtVolume":"139529756.66","ts":"1794210120790","bidPr":"74094.89","askPr":"74094.9","change24h":"-0.02372"}]}
            """;

        var quote = TickerSnapshotParser.ParseRest(
            json,
            InstrumentKey.Create(Market.Spot, "BTCUSDT"),
            DateTimeOffset.UtcNow);

        Assert.Equal(74094.9m, quote.Price);
        Assert.Equal(-0.02372m, quote.Change24h);
        Assert.Equal(76196.57m, quote.High24h);
        Assert.Equal(InstrumentKey.Create(Market.Spot, "BTCUSDT"), quote.Key);
        Assert.Equal(74094.89m, quote.BidPrice);
        Assert.Equal(74094.9m, quote.AskPrice);
        Assert.Equal(139529756.66m, quote.QuoteVolume24h);
    }

    [Fact]
    public void ParseRest_FuturesPayload()
    {
        const string json = """
            {"code":"00000","msg":"success","requestTime":1794210122346,"data":[{"symbol":"BTCUSDT","lastPr":"74058.8","askPr":"74058.8","bidPr":"74058.7","high24h":"76160","low24h":"73408.8","ts":"1794210122347","change24h":"-0.02382","fundingRate":"0.0001","markPrice":"74058.8"}]}
            """;

        var quote = TickerSnapshotParser.ParseRest(
            json,
            InstrumentKey.Create(Market.UsdFutures, "BTCUSDT"),
            DateTimeOffset.UtcNow);

        Assert.Equal(74058.8m, quote.Price);
        Assert.Equal(0.0001m, quote.FundingRate);
        Assert.Equal(Market.UsdFutures, quote.Key.Market);
    }

    [Fact]
    public void ParseRest_UnknownSymbol_Throws()
    {
        const string json = """
            {"code":"40034","msg":"Parameter does not exist","data":[]}
            """;

        Assert.Throws<InvalidOperationException>(() => TickerSnapshotParser.ParseRest(
            json,
            InstrumentKey.Create(Market.Spot, "NOPEUSDT"),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MessageHelpers_DetectPongAndAck()
    {
        Assert.True(TickerSnapshotParser.IsPong("pong"));
        Assert.True(TickerSnapshotParser.IsPong(" pong "));
        Assert.True(TickerSnapshotParser.IsSubscribeAck("""{"event":"subscribe","arg":{}}"""));
        Assert.False(TickerSnapshotParser.IsSubscribeAck("""{"action":"snapshot"}"""));
    }
}
