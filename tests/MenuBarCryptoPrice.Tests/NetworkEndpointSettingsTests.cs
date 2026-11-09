using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.Tests;

public sealed class NetworkEndpointSettingsTests
{
    [Fact]
    public void FromConfig_DefaultUsesSystemProxyAndOfficialEndpoints()
    {
        var settings = NetworkEndpointSettings.FromConfig(new AppConfig());

        Assert.Equal(NetworkProxyMode.System, settings.ProxyMode);
        Assert.Null(settings.ProxyUri);
        Assert.Equal("https://api.bitget.com", settings.RestBaseUri.ToString().TrimEnd('/'));
        Assert.Equal("wss://ws.bitget.com/v2/ws/public", settings.WsUri.ToString());
    }

    [Fact]
    public void FromConfig_CustomProxyWithoutSchemeIsNormalized()
    {
        var config = new AppConfig
        {
            ProxyMode = NetworkProxyMode.Custom,
            ProxyUrl = "127.0.0.1:7890"
        };

        var settings = NetworkEndpointSettings.FromConfig(config);

        Assert.Equal(NetworkProxyMode.Custom, settings.ProxyMode);
        Assert.NotNull(settings.ProxyUri);
        Assert.Equal("http", settings.ProxyUri!.Scheme);
        Assert.Equal("127.0.0.1", settings.ProxyUri.Host);
        Assert.Equal(7890, settings.ProxyUri.Port);
    }

    [Theory]
    [InlineData("socks5://127.0.0.1:7891", "socks5")]
    [InlineData("socks5h://127.0.0.1:7891", "socks5h")]
    [InlineData("socks4://127.0.0.1:1080", "socks4")]
    [InlineData("http://proxy.local:8080", "http")]
    [InlineData("https://proxy.local:8443", "https")]
    public void TryParseProxy_AcceptsSupportedSchemes(string value, string expectedScheme)
    {
        Assert.True(NetworkEndpointSettings.TryParseProxy(value, out var uri));
        Assert.NotNull(uri);
        Assert.Equal(expectedScheme, uri!.Scheme);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://127.0.0.1:21")]
    [InlineData("not a url")]
    public void TryParseProxy_RejectsUnsupportedInput(string value)
    {
        Assert.False(NetworkEndpointSettings.TryParseProxy(value, out _));
    }

    [Fact]
    public void FromConfig_InvalidCustomProxyFallsBackToSystem()
    {
        var config = new AppConfig
        {
            ProxyMode = NetworkProxyMode.Custom,
            ProxyUrl = "ftp://127.0.0.1:21"
        };

        var settings = NetworkEndpointSettings.FromConfig(config);

        Assert.Equal(NetworkProxyMode.System, settings.ProxyMode);
        Assert.Null(settings.ProxyUri);
    }

    [Theory]
    [InlineData("https://api.bitget.com/", "https://api.bitget.com")]
    [InlineData("api.bitget.com", "https://api.bitget.com")]
    [InlineData("http://proxy.local/bitget/", "http://proxy.local/bitget")]
    public void NormalizeRestBaseUrl_ProducesAppendableBase(string input, string expected)
    {
        Assert.Equal(expected, NetworkEndpointSettings.NormalizeRestBaseUrl(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://proxy.local")]
    [InlineData("wss://proxy.local")]
    [InlineData("http://proxy.local?x=1")]
    public void NormalizeRestBaseUrl_InvalidInputUsesOfficialEndpoint(string input)
    {
        Assert.Equal(
            NetworkEndpointSettings.DefaultRestBaseUrl,
            NetworkEndpointSettings.NormalizeRestBaseUrl(input));
    }

    [Theory]
    [InlineData("wss://ws.bitget.com/v2/ws/public", "wss://ws.bitget.com/v2/ws/public")]
    [InlineData("ws.bitget.com/v2/ws/public", "wss://ws.bitget.com/v2/ws/public")]
    [InlineData("ws://localhost:8080/v2/ws/public", "ws://localhost:8080/v2/ws/public")]
    [InlineData("wss://gateway.local/ws?token=abc", "wss://gateway.local/ws?token=abc")]
    public void NormalizeWsUrl_KeepsPathAndQuery(string input, string expected)
    {
        Assert.Equal(expected, NetworkEndpointSettings.NormalizeWsUrl(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://ws.bitget.com/v2/ws/public")]
    public void NormalizeWsUrl_InvalidInputUsesOfficialEndpoint(string input)
    {
        Assert.Equal(NetworkEndpointSettings.DefaultWsUrl, NetworkEndpointSettings.NormalizeWsUrl(input));
    }
}
