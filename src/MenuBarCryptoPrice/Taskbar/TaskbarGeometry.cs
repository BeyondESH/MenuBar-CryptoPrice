using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.Taskbar;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Math.Max(0, Right - Left);

    public int Height => Math.Max(0, Bottom - Top);
}

public readonly record struct TaskbarGeometry(
    PixelRect Taskbar,
    int TrayLeft,
    int? IconRight,
    int? GroupLeft,
    int? WidgetsRight,
    int Dpi,
    bool IsAutoHide,
    bool IsHidden)
{
    public double Scale => Dpi <= 0 ? 1.0 : Dpi / 96.0;

    public int RightAvailablePixels
    {
        get
        {
            var leftGuard = (IconRight ?? Taskbar.Left) + 8;
            return Math.Max(0, TrayLeft - 8 - leftGuard);
        }
    }

    public int LeftAnchorStartPixels => (WidgetsRight ?? Taskbar.Left) + 8;

    public int LeftAvailablePixels
    {
        get
        {
            var rightGuard = (GroupLeft ?? TrayLeft) - 8;
            return Math.Max(0, rightGuard - LeftAnchorStartPixels);
        }
    }

    public int AvailableWidthPixelsFor(AnchorMode anchor)
        => anchor == AnchorMode.Left ? LeftAvailablePixels : RightAvailablePixels;

    public double AvailableWidthDipFor(AnchorMode anchor)
        => AvailableWidthPixelsFor(anchor) / Scale;

    public double AvailableWidthDip => AvailableWidthDipFor(AnchorMode.Right);
}

public static class AnchorResolver
{
    public const double MinimumLeftWidthDip = 120;

    public static (AnchorMode Anchor, double AvailableDip) Resolve(AnchorMode requested, TaskbarGeometry? geometry)
    {
        if (geometry is null)
        {
            return (AnchorMode.Right, 585);
        }

        if (requested == AnchorMode.Left)
        {
            var left = geometry.Value.AvailableWidthDipFor(AnchorMode.Left);
            if (left >= MinimumLeftWidthDip)
            {
                return (AnchorMode.Left, left);
            }
        }

        var right = geometry.Value.AvailableWidthDipFor(AnchorMode.Right);
        return (AnchorMode.Right, Math.Max(60, right));
    }
}
