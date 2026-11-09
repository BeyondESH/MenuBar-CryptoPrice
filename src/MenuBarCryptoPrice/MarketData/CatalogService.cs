using System.Text.Json;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.MarketData;

public sealed class CatalogService
{
    private static readonly HttpClient SharedHttp = CreateHttpClient();

    private readonly string _cacheDirectory;
    private readonly ILog _log;
    private readonly TimeSpan _ttl;
    private readonly Func<DateTimeOffset> _clock;
    private readonly HttpClient _http;
    private readonly Dictionary<Market, CatalogFile> _memory = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CatalogService(
        string cacheDirectory,
        ILog? log = null,
        TimeSpan? ttl = null,
        Func<DateTimeOffset>? clock = null,
        HttpClient? httpClient = null)
    {
        _cacheDirectory = cacheDirectory;
        _log = log ?? NullLog.Instance;
        _ttl = ttl ?? TimeSpan.FromHours(24);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _http = httpClient ?? SharedHttp;
    }

    public async Task<CatalogResult> GetCatalogAsync(Market market, bool forceRefresh, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _memory.TryGetValue(market, out var memory);
            var cachePath = GetCachePath(market);
            CatalogFile? disk = null;

            if (!forceRefresh && memory is not null && !IsExpired(memory))
            {
                return new CatalogResult(memory.Entries, false, null);
            }

            if (!forceRefresh)
            {
                disk = TryLoadDisk(cachePath);
                if (disk is not null && !IsExpired(disk))
                {
                    _memory[market] = disk;
                    return new CatalogResult(disk.Entries, false, null);
                }
            }

            try
            {
                var fresh = await FetchAsync(market, cancellationToken).ConfigureAwait(false);
                _memory[market] = fresh;
                WriteDisk(cachePath, fresh);
                return new CatalogResult(fresh.Entries, false, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to refresh the {market} catalog.", ex);
                var fallback = disk ?? memory;
                if (fallback is not null)
                {
                    return new CatalogResult(fallback.Entries, true, "目录可能过期：网络不可用，正在使用本地缓存");
                }

                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool HasCatalog(Market market) => _memory.ContainsKey(market);

    public bool TryGetCachedEntry(Market market, string symbol, out CatalogEntry? entry)
    {
        entry = null;
        if (!_memory.TryGetValue(market, out var file))
        {
            return false;
        }

        entry = file.Entries.FirstOrDefault(item =>
            string.Equals(item.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
        return entry is not null;
    }

    public static CatalogFile ParseCatalog(Market market, string json, DateTimeOffset fetchedAt)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Bitget catalog response has no data array.");
        }

        var entries = new List<CatalogEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in data.EnumerateArray())
        {
            var symbol = GetString(item, "symbol")?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(symbol) || !seen.Add(symbol))
            {
                continue;
            }

            var baseCoin = GetString(item, "baseCoin") ?? symbol;
            var quoteCoin = GetString(item, "quoteCoin") ?? "USDT";
            var precisionText = market == Market.Spot
                ? GetString(item, "pricePrecision")
                : GetString(item, "pricePlace");
            var precision = int.TryParse(precisionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : market == Market.Spot ? 2 : 1;
            var isOnline = market != Market.Spot
                || string.Equals(GetString(item, "status"), "online", StringComparison.OrdinalIgnoreCase);

            entries.Add(new CatalogEntry(
                market,
                symbol,
                baseCoin,
                quoteCoin,
                isOnline,
                Math.Clamp(precision, 0, 8)));
        }

        entries.Sort((left, right) => string.Compare(left.Symbol, right.Symbol, StringComparison.OrdinalIgnoreCase));
        return new CatalogFile { FetchedAt = fetchedAt, Entries = entries };
    }

    private async Task<CatalogFile> FetchAsync(Market market, CancellationToken cancellationToken)
    {
        var url = market == Market.Spot
            ? "https://api.bitget.com/api/v2/spot/public/symbols"
            : "https://api.bitget.com/api/v2/mix/market/contracts?productType=USDT-FUTURES";

        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseCatalog(market, json, _clock());
    }

    private bool IsExpired(CatalogFile file) => _clock() - file.FetchedAt > _ttl;

    private string GetCachePath(Market market)
        => Path.Combine(_cacheDirectory, market == Market.Spot ? "spot-symbols.json" : "futures-contracts.json");

    private CatalogFile? TryLoadDisk(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<CatalogFile>(json, ConfigService.CreateJsonOptions());
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to read catalog cache '{path}'.", ex);
            return null;
        }
    }

    private void WriteDisk(string path, CatalogFile file)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(file, ConfigService.CreateJsonOptions());
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            _log.Warn($"Failed to write catalog cache '{path}'.", ex);
        }
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MenuBarCryptoPrice/1.0");
        return client;
    }
}
