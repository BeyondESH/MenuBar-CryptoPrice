namespace MenuBarCryptoPrice.MarketData;

public enum ChangeDirection
{
    Flat,
    Up,
    Down
}

public static class PriceFormatter
{
    public static string FormatPrice(decimal price, int precision)
    {
        var digits = Math.Clamp(precision, 0, 8);
        return price.ToString("N" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    public static string FormatChange(decimal change)
    {
        var percent = Math.Round(change * 100m, 2, MidpointRounding.AwayFromZero);
        if (percent == 0m)
        {
            percent = 0m;
        }

        var sign = percent > 0m ? "+" : string.Empty;
        return sign + percent.ToString("0.00", CultureInfo.InvariantCulture) + "%";
    }

    public static string FormatChangeWithArrow(decimal change)
    {
        var arrow = GetDirection(change) switch
        {
            ChangeDirection.Up => "▲",
            ChangeDirection.Down => "▼",
            _ => "•"
        };

        return arrow + " " + FormatChange(change);
    }

    public static ChangeDirection GetDirection(decimal change)
    {
        var rounded = Math.Round(change, 8, MidpointRounding.AwayFromZero);
        if (rounded > 0m)
        {
            return ChangeDirection.Up;
        }

        return rounded < 0m ? ChangeDirection.Down : ChangeDirection.Flat;
    }

    public static string FormatFundingRate(decimal? rate)
        => rate is null
            ? "—"
            : (rate.Value * 100m).ToString("+0.0000;-0.0000;0.0000", CultureInfo.InvariantCulture) + "%";

    public static string FormatVolume(decimal? volume)
    {
        if (volume is null)
        {
            return "—";
        }

        var value = volume.Value;
        if (value >= 100_000_000m)
        {
            return (value / 100_000_000m).ToString("0.##", CultureInfo.InvariantCulture) + " 亿";
        }

        if (value >= 10_000m)
        {
            return (value / 10_000m).ToString("0.##", CultureInfo.InvariantCulture) + " 万";
        }

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string FormatTimestamp(DateTimeOffset time)
        => time.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}
