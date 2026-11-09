using System.Windows.Media;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Taskbar;

public static class Palette
{
    public static readonly Brush Up = Freeze(Color.FromRgb(0x4E, 0xD8, 0xA0));
    public static readonly Brush Down = Freeze(Color.FromRgb(0xFF, 0x7B, 0x72));
    public static readonly Brush Flat = Freeze(Color.FromRgb(0xA1, 0xA7, 0xAD));
    public static readonly Brush Stale = Freeze(Color.FromRgb(0x7C, 0x83, 0x8A));
    public static readonly Brush Text = Freeze(Color.FromRgb(0xF2, 0xF4, 0xF5));
    public static readonly Brush Secondary = Freeze(Color.FromRgb(0xA1, 0xA7, 0xAD));

    public static Brush ForDirection(ChangeDirection direction, ColorScheme scheme) => direction switch
    {
        ChangeDirection.Up => scheme == ColorScheme.GreenUp ? Up : Down,
        ChangeDirection.Down => scheme == ColorScheme.GreenUp ? Down : Up,
        _ => Flat
    };

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
