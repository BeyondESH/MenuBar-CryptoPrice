using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class PriceFormatterTests
{
    [Theory]
    [InlineData("73942.9", 2, "73,942.90")]
    [InlineData("73942.9", 1, "73,942.9")]
    [InlineData("73942.9", 0, "73,943")]
    [InlineData("0.00001234", 8, "0.00001234")]
    [InlineData("1234567.89", 2, "1,234,567.89")]
    public void FormatPrice_UsesPrecisionAndThousandsSeparators(string raw, int precision, string expected)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, PriceFormatter.FormatPrice(value, precision));
    }

    [Theory]
    [InlineData("-0.02609", "-2.61%")]
    [InlineData("0.02372", "+2.37%")]
    [InlineData("0", "0.00%")]
    [InlineData("-0.0000001", "0.00%")]
    [InlineData("1.5", "+150.00%")]
    public void FormatChange_UsesSignedPercent(string raw, string expected)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, PriceFormatter.FormatChange(value));
    }

    [Fact]
    public void GetDirection_ClassifiesSign()
    {
        Assert.Equal(ChangeDirection.Up, PriceFormatter.GetDirection(0.01m));
        Assert.Equal(ChangeDirection.Down, PriceFormatter.GetDirection(-0.01m));
        Assert.Equal(ChangeDirection.Flat, PriceFormatter.GetDirection(0m));
        Assert.Equal(ChangeDirection.Flat, PriceFormatter.GetDirection(0.000000001m));
    }

    [Fact]
    public void FormatFundingRate_HandlesNullAndSign()
    {
        Assert.Equal("—", PriceFormatter.FormatFundingRate(null));
        Assert.Equal("+0.0100%", PriceFormatter.FormatFundingRate(0.0001m));
        Assert.Equal("-0.0100%", PriceFormatter.FormatFundingRate(-0.0001m));
    }

    [Theory]
    [InlineData("-0.02609", "▼ -2.61%")]
    [InlineData("0.02372", "▲ +2.37%")]
    [InlineData("0", "• 0.00%")]
    public void FormatChangeWithArrow_AddsDirectionGlyph(string raw, string expected)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, PriceFormatter.FormatChangeWithArrow(value));
    }

    [Theory]
    [InlineData("139529756.66", "1.4 亿")]
    [InlineData("2623078434.75", "26.23 亿")]
    [InlineData("12345", "1.23 万")]
    [InlineData("999", "999")]
    public void FormatVolume_UsesChineseUnits(string raw, string expected)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, PriceFormatter.FormatVolume(value));
    }
}
