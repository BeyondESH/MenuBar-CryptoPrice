namespace MenuBarCryptoPrice.Core;

public readonly record struct InstrumentKey(Market Market, string Symbol)
{
    public static InstrumentKey Create(Market market, string? symbol)
        => new(market, (symbol ?? string.Empty).Trim().ToUpperInvariant());

    public override string ToString() => $"{Market.ToInstType()}:{Symbol}";
}
