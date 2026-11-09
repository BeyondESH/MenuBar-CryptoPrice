using System.Windows.Media;

namespace MenuBarCryptoPrice.Taskbar;

public interface ITextMeasurer
{
    double Measure(string text, double fontSize, FontWeight weight, string fontFamily = "Segoe UI");
}

public sealed class WpfTextMeasurer : ITextMeasurer
{
    public double Measure(string text, double fontSize, FontWeight weight, string fontFamily = "Segoe UI")
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var typeface = new Typeface(
            new FontFamily(fontFamily),
            FontStyles.Normal,
            weight,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            1.0);

        return formatted.WidthIncludingTrailingWhitespace;
    }
}
