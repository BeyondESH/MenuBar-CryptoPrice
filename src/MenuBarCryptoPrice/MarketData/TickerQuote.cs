using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.MarketData;

public sealed record TickerQuote(
    InstrumentKey Key,
    decimal Price,
    decimal Change24h,
    decimal High24h,
    decimal Low24h,
    decimal? FundingRate,
    decimal? MarkPrice,
    DateTimeOffset SourceTime,
    DateTimeOffset ReceivedAt)
{
    public decimal? BidPrice { get; init; }

    public decimal? AskPrice { get; init; }

    public decimal? QuoteVolume24h { get; init; }

    public bool IsStale(DateTimeOffset now) => now - ReceivedAt > TimeSpan.FromSeconds(30);
}
