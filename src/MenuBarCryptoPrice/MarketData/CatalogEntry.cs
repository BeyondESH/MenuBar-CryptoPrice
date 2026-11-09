using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public sealed record CatalogEntry(
    Market Market,
    string Symbol,
    string BaseCoin,
    string QuoteCoin,
    bool IsOnline,
    int Precision)
{
    public string DisplayName => $"{BaseCoin}/{QuoteCoin}";

    public string MarketBadge => Market.ToShortName();

    public bool IsUsdtQuoted => string.Equals(QuoteCoin, "USDT", StringComparison.OrdinalIgnoreCase);
}

public sealed class CatalogFile
{
    public DateTimeOffset FetchedAt { get; set; }

    public List<CatalogEntry> Entries { get; set; } = new();
}

public sealed record CatalogResult(IReadOnlyList<CatalogEntry> Entries, bool IsStale, string? Warning);
