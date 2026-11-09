namespace MenuBarCryptoPrice.Core;

public static class ConfigDefaults
{
    public static AppConfig Create() => new()
    {
        Version = AppConfig.CurrentVersion,
        MaxVisibleItems = 4,
        Layout = LayoutMode.SingleLine,
        FontSize = FontSizeOption.Medium,
        ColorScheme = ColorScheme.GreenUp,
        PositionMode = PositionMode.Taskbar,
        Anchor = AnchorMode.Right,
        StartWithWindows = false,
        ProxyMode = NetworkProxyMode.System,
        ProxyUrl = string.Empty,
        RestBaseUrl = NetworkEndpointSettings.DefaultRestBaseUrl,
        WsUrl = NetworkEndpointSettings.DefaultWsUrl,
        Items = new List<TickerItemConfig>
        {
            new() { Market = Market.Spot, Symbol = "BTCUSDT", Precision = 2 },
            new() { Market = Market.Spot, Symbol = "ETHUSDT", Precision = 2 },
            new() { Market = Market.UsdFutures, Symbol = "BTCUSDT", Precision = 1 },
            new() { Market = Market.UsdFutures, Symbol = "ETHUSDT", Precision = 1 }
        }
    };
}
