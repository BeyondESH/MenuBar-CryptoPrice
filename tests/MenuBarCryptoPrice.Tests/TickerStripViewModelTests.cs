using System.Windows;
using System.Windows.Media;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;
using MenuBarCryptoPrice.Taskbar;

namespace MenuBarCryptoPrice.Tests;

public sealed class TickerStripViewModelTests
{
    private sealed class FixedWidthMeasurer : ITextMeasurer
    {
        public double Measure(string text, double fontSize, FontWeight weight, string fontFamily = "Segoe UI")
            => text.Length * 7;
    }

    private static TickerStore StoreWithQuotes(params InstrumentKey[] keys)
    {
        var store = new TickerStore();
        var now = DateTimeOffset.UtcNow;
        foreach (var key in keys)
        {
            store.Update(new TickerQuote(key, 100m, 0.01m, 101m, 99m, null, null, now, now));
        }

        return store;
    }

    [Fact]
    public void DefaultConfig_BuildsFourItems()
    {
        var config = ConfigDefaults.Create();
        var store = StoreWithQuotes(config.Items.Select(item => item.Key).ToArray());
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        Assert.False(viewModel.IsEmpty);
        Assert.Equal(4, viewModel.Items.Count);
        Assert.Equal(0, viewModel.OverflowCount);
        Assert.True(viewModel.DesiredWidth > 0);
        Assert.Equal(26, viewModel.Height);
        Assert.Equal(Visibility.Collapsed, viewModel.OverflowVisibility);
    }

    [Fact]
    public void CompactTwoLine_UsesTallerStrip()
    {
        var config = ConfigDefaults.Create();
        config.Layout = LayoutMode.CompactTwoLine;
        var store = StoreWithQuotes(config.Items.Select(item => item.Key).ToArray());
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        Assert.Equal(36, viewModel.Height);
        Assert.All(viewModel.Items, item => Assert.Equal(Visibility.Visible, item.TwoLineVisibility));
    }

    [Fact]
    public void NarrowAvailableWidth_HidesTailAndShowsOverflowBadge()
    {
        var config = ConfigDefaults.Create();
        var store = StoreWithQuotes(config.Items.Select(item => item.Key).ToArray());
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        viewModel.Rebuild(120);

        Assert.Single(viewModel.Items);
        Assert.Equal(3, viewModel.OverflowCount);
        Assert.Equal("+3", viewModel.OverflowText);
        Assert.Equal(Visibility.Visible, viewModel.OverflowVisibility);
        Assert.Contains("全部币种（4）", viewModel.OverflowTooltip);
        Assert.Contains("● BTC/USDT", viewModel.OverflowTooltip);
        Assert.Contains("○ ETH/USDT", viewModel.OverflowTooltip);
        Assert.Contains("超宽隐藏", viewModel.OverflowTooltip);
    }

    [Fact]
    public void ItemTooltip_ContainsFullMarketInfo()
    {
        var config = ConfigDefaults.Create();
        config.Items.Clear();
        config.Items.Add(new TickerItemConfig { Market = Market.UsdFutures, Symbol = "BTCUSDT", Precision = 1 });
        var store = new TickerStore();
        var now = DateTimeOffset.UtcNow;
        store.Update(new TickerQuote(
            InstrumentKey.Create(Market.UsdFutures, "BTCUSDT"),
            74000m,
            0.02m,
            76000m,
            73000m,
            0.0001m,
            73999m,
            now,
            now)
        {
            BidPrice = 73999.9m,
            AskPrice = 74000.1m,
            QuoteVolume24h = 2_623_078_434.75m
        });

        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());
        var tooltip = viewModel.Items[0].Tooltip;

        Assert.Contains("24h 高", tooltip);
        Assert.Contains("24h 低", tooltip);
        Assert.Contains("24h 量", tooltip);
        Assert.Contains("买一 / 卖一", tooltip);
        Assert.Contains("资金费率", tooltip);
        Assert.Contains("标记价格", tooltip);
    }

    [Fact]
    public void MaxVisibleItems_CapsCandidates()
    {
        var config = ConfigDefaults.Create();
        config.Items.Add(new TickerItemConfig { Market = Market.Spot, Symbol = "SOLUSDT", Precision = 2 });
        config.Items.Add(new TickerItemConfig { Market = Market.Spot, Symbol = "DOGEUSDT", Precision = 5 });
        config.MaxVisibleItems = 2;
        var store = StoreWithQuotes(config.Items.Select(item => item.Key).ToArray());
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        Assert.Equal(2, viewModel.Items.Count);
        Assert.Equal(0, viewModel.OverflowCount);
    }

    [Fact]
    public void EmptyConfig_ShowsPlaceholder()
    {
        var config = ConfigDefaults.Create();
        config.Items.Clear();
        var viewModel = new TickerStripViewModel(config, new TickerStore(), new FixedWidthMeasurer());

        Assert.True(viewModel.IsEmpty);
        Assert.Equal(Visibility.Visible, viewModel.EmptyVisibility);
        Assert.Equal(Visibility.Collapsed, viewModel.ContentVisibility);
        Assert.Equal("⚙ 未配置", viewModel.EmptyText);
    }

    [Fact]
    public void Refresh_UpdatesPriceText()
    {
        var config = ConfigDefaults.Create();
        config.Items.Clear();
        config.Items.Add(new TickerItemConfig { Market = Market.Spot, Symbol = "BTCUSDT", Precision = 2 });
        var store = new TickerStore();
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        Assert.Equal("—", viewModel.Items[0].PriceText);

        var now = DateTimeOffset.UtcNow;
        store.Update(new TickerQuote(
            InstrumentKey.Create(Market.Spot, "BTCUSDT"),
            73942.9m,
            -0.02609m,
            76196.57m,
            73481.3m,
            null,
            null,
            now,
            now));
        viewModel.Refresh();

        Assert.Equal("73,942.90", viewModel.Items[0].PriceText);
        Assert.Equal("▼ -2.61%", viewModel.Items[0].ChangeText);
        Assert.False(viewModel.Items[0].IsStale);
    }

    [Fact]
    public void ExtraLargeFont_UsesTallerStrip()
    {
        var config = ConfigDefaults.Create();
        config.FontSize = FontSizeOption.ExtraLarge;
        var store = StoreWithQuotes(config.Items.Select(item => item.Key).ToArray());
        var viewModel = new TickerStripViewModel(config, store, new FixedWidthMeasurer());

        Assert.Equal(30, viewModel.Height);
    }
}
