using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Tests;

public sealed class ConfigServiceTests
{
    [Fact]
    public void MissingFile_ReturnsDefaults_AndMarksFirstRun()
    {
        using var temp = new TempDirectory();
        using var service = new ConfigService(temp.File("config.json"), autoSave: false);

        Assert.True(service.IsFirstRun);
        Assert.Equal(4, service.Config.Items.Count);
        Assert.Equal(4, service.Config.MaxVisibleItems);
        Assert.Equal(Market.Spot, service.Config.Items[0].Market);
        Assert.Equal("BTCUSDT", service.Config.Items[0].Symbol);
        Assert.Equal(Market.UsdFutures, service.Config.Items[2].Market);
        Assert.Equal(1, service.Config.Items[2].Precision);
    }

    [Fact]
    public void SaveAndReload_RoundTrips()
    {
        using var temp = new TempDirectory();
        var path = temp.File("config.json");

        using (var service = new ConfigService(path, autoSave: false))
        {
            service.Update(config =>
            {
                config.MaxVisibleItems = 2;
                config.Layout = LayoutMode.CompactTwoLine;
                config.FontSize = FontSizeOption.Large;
                config.ColorScheme = ColorScheme.RedUp;
                config.PositionMode = PositionMode.AboveTaskbar;
                config.StartWithWindows = true;
                config.Items.Add(new TickerItemConfig { Market = Market.Spot, Symbol = "solusdt", Precision = 4 });
            });
            service.SaveNow();
        }

        using var reloaded = new ConfigService(path, autoSave: false);
        Assert.False(reloaded.IsFirstRun);
        Assert.Equal(2, reloaded.Config.MaxVisibleItems);
        Assert.Equal(LayoutMode.CompactTwoLine, reloaded.Config.Layout);
        Assert.Equal(FontSizeOption.Large, reloaded.Config.FontSize);
        Assert.Equal(ColorScheme.RedUp, reloaded.Config.ColorScheme);
        Assert.Equal(PositionMode.AboveTaskbar, reloaded.Config.PositionMode);
        Assert.True(reloaded.Config.StartWithWindows);
        Assert.Contains(reloaded.Config.Items, item => item.Symbol == "SOLUSDT" && item.Precision == 4);
    }

    [Fact]
    public void CorruptFile_IsBackedUp_AndDefaultsAreUsed()
    {
        using var temp = new TempDirectory();
        var path = temp.File("config.json");
        File.WriteAllText(path, "{ this is not json");

        using var service = new ConfigService(path, autoSave: false);

        Assert.Equal(4, service.Config.Items.Count);
        Assert.True(File.Exists(path + ".bak"));
    }

    [Fact]
    public void Version1_MigratesPrecisionByMarket()
    {
        using var temp = new TempDirectory();
        var path = temp.File("config.json");
        File.WriteAllText(path, """
            {
              "version": 1,
              "maxVisibleItems": 3,
              "items": [
                { "market": "SPOT", "symbol": "btcusdt" },
                { "market": "USDT-FUTURES", "symbol": "btcusdt" }
              ]
            }
            """);

        using var service = new ConfigService(path, autoSave: false);

        Assert.Equal(AppConfig.CurrentVersion, service.Config.Version);
        Assert.Equal(2, service.Config.Items[0].Precision);
        Assert.Equal(1, service.Config.Items[1].Precision);
        Assert.Equal("BTCUSDT", service.Config.Items[0].Symbol);
    }

    [Fact]
    public void Validate_ClampsCountAndDeduplicatesByMarketAndSymbol()
    {
        var config = new AppConfig
        {
            MaxVisibleItems = 99,
            Items = new List<TickerItemConfig>
            {
                new() { Market = Market.Spot, Symbol = "btcusdt", Precision = 2 },
                new() { Market = Market.Spot, Symbol = "BTCUSDT", Precision = 2 },
                new() { Market = Market.UsdFutures, Symbol = "BTCUSDT", Precision = 1 },
                new() { Market = Market.Spot, Symbol = "  ", Precision = 2 }
            }
        };

        ConfigService.Validate(config);

        Assert.Equal(8, config.MaxVisibleItems);
        Assert.Equal(2, config.Items.Count);
        Assert.Equal(2, config.Items.Count(item => item.Symbol == "BTCUSDT"));
    }

    [Fact]
    public void Validate_LowerBoundAndDefaultPrecision()
    {
        var config = new AppConfig
        {
            MaxVisibleItems = 0,
            Items = new List<TickerItemConfig>
            {
                new() { Market = Market.UsdFutures, Symbol = "ETHUSDT", Precision = -1 }
            }
        };

        ConfigService.Validate(config);

        Assert.Equal(1, config.MaxVisibleItems);
        Assert.Equal(1, config.Items[0].Precision);
    }
}
