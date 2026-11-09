using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class BackoffTests
{
    [Fact]
    public void Delay_FollowsSequence_AndCaps()
    {
        var expected = new[] { 1_000, 2_000, 4_000, 8_000, 15_000, 30_000, 30_000, 30_000 };
        for (var attempt = 0; attempt < expected.Length; attempt++)
        {
            Assert.Equal(TimeSpan.FromMilliseconds(expected[attempt]), Backoff.GetDelay(attempt));
        }
    }

    [Fact]
    public void Delay_AddsBoundedJitter()
    {
        var random = new Random(1234);
        for (var index = 0; index < 20; index++)
        {
            var delay = Backoff.GetDelay(0, random);
            Assert.InRange(delay.TotalMilliseconds, 1_000, 1_250);
        }
    }

    [Fact]
    public void InstrumentKey_NormalizesAndDistinguishesMarkets()
    {
        var spot = InstrumentKey.Create(Market.Spot, " btcusdt ");
        var futures = InstrumentKey.Create(Market.UsdFutures, "BTCUSDT");

        Assert.Equal("BTCUSDT", spot.Symbol);
        Assert.Equal(spot, InstrumentKey.Create(Market.Spot, "btcusdt"));
        Assert.NotEqual(spot, futures);
    }
}
