namespace MenuBarCryptoPrice.Core;

public enum NetworkProxyMode
{
    System,
    Direct,
    Custom
}

public sealed record NetworkEndpointSettings(
    NetworkProxyMode ProxyMode,
    Uri? ProxyUri,
    Uri RestBaseUri,
    Uri WsUri)
{
    public const string DefaultRestBaseUrl = "https://api.bitget.com";
    public const string DefaultWsUrl = "wss://ws.bitget.com/v2/ws/public";

    public static NetworkEndpointSettings FromConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var mode = Enum.IsDefined(config.ProxyMode) ? config.ProxyMode : NetworkProxyMode.System;
        Uri? proxy = null;
        if (mode == NetworkProxyMode.Custom)
        {
            if (!TryParseProxy(config.ProxyUrl, out proxy))
            {
                mode = NetworkProxyMode.System;
                proxy = null;
            }
        }

        return new NetworkEndpointSettings(
            mode,
            proxy,
            new Uri(NormalizeRestBaseUrl(config.RestBaseUrl), UriKind.Absolute),
            new Uri(NormalizeWsUrl(config.WsUrl), UriKind.Absolute));
    }

    public static bool TryParseProxy(string? text, out Uri? proxy)
    {
        proxy = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = "http://" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https" or "socks4" or "socks4a" or "socks5" or "socks5h"))
        {
            return false;
        }

        proxy = uri;
        return true;
    }

    public static string NormalizeRestBaseUrl(string? text)
        => TryNormalizeRestBaseUrl(text, out var normalized) ? normalized : DefaultRestBaseUrl;

    public static bool TryNormalizeRestBaseUrl(string? text, out string normalized)
    {
        normalized = DefaultRestBaseUrl;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var value = text.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = "https://" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return true;
    }

    public static string NormalizeWsUrl(string? text)
        => TryNormalizeWsUrl(text, out var normalized) ? normalized : DefaultWsUrl;

    public static bool TryNormalizeWsUrl(string? text, out string normalized)
    {
        normalized = DefaultWsUrl;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var value = text.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = "wss://" + value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        if (uri.Scheme is not ("ws" or "wss"))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Query).TrimEnd('/');
        return true;
    }
}
