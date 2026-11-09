using System.Text.Json;
using System.Text.Json.Serialization;

namespace MenuBarCryptoPrice.Core;

public enum Market
{
    Spot,
    UsdFutures
}

public static class MarketExtensions
{
    public static string ToInstType(this Market market) => market == Market.Spot ? "SPOT" : "USDT-FUTURES";

    public static string ToDisplayName(this Market market) => market == Market.Spot ? "现货" : "U本位永续";

    public static string ToShortName(this Market market) => market == Market.Spot ? "现" : "永";

    public static Market FromInstType(string instType) => instType switch
    {
        "SPOT" => Market.Spot,
        "USDT-FUTURES" => Market.UsdFutures,
        _ => throw new ArgumentOutOfRangeException(nameof(instType), instType, "Unsupported Bitget instType")
    };
}

public sealed class MarketJsonConverter : JsonConverter<Market>
{
    public override Market Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "SPOT" or "Spot" or "spot" => Market.Spot,
            "USDT-FUTURES" or "UsdFutures" or "usdFutures" or "FUTURES" => Market.UsdFutures,
            _ => throw new JsonException($"Unknown market value '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Market value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToInstType());
}
