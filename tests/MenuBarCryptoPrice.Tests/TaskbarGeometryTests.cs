using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Taskbar;

namespace MenuBarCryptoPrice.Tests;

public sealed class TaskbarGeometryTests
{
    private static TaskbarGeometry Geometry()
        => new(
            new PixelRect(0, 1032, 1920, 1080),
            TrayLeft: 1642,
            IconRight: 1379,
            GroupLeft: 564,
            WidgetsRight: 158,
            Dpi: 96,
            IsAutoHide: false,
            IsHidden: false);

    [Fact]
    public void RightAvailableWidth_SubtractsTrayAndIconGuards()
    {
        Assert.Equal(247, Geometry().RightAvailablePixels);
        Assert.Equal(247, Geometry().AvailableWidthPixelsFor(AnchorMode.Right));
    }

    [Fact]
    public void LeftAvailableWidth_UsesWidgetsAndIconGroupGuards()
    {
        var geometry = Geometry();

        Assert.Equal(166, geometry.LeftAnchorStartPixels);
        Assert.Equal(390, geometry.LeftAvailablePixels);
        Assert.Equal(390, geometry.AvailableWidthPixelsFor(AnchorMode.Left));
    }

    [Fact]
    public void Resolver_PrefersLeftWhenItFits()
    {
        var (anchor, available) = AnchorResolver.Resolve(AnchorMode.Left, Geometry());

        Assert.Equal(AnchorMode.Left, anchor);
        Assert.Equal(390, available);
    }

    [Fact]
    public void Resolver_FallsBackToRightWhenLeftIsTooNarrow()
    {
        var geometry = Geometry() with { GroupLeft = 260 };

        var (anchor, available) = AnchorResolver.Resolve(AnchorMode.Left, geometry);

        Assert.Equal(AnchorMode.Right, anchor);
        Assert.Equal(247, available);
    }

    [Fact]
    public void Resolver_WithoutGeometry_UsesConservativeDefault()
    {
        var (anchor, available) = AnchorResolver.Resolve(AnchorMode.Left, null);

        Assert.Equal(AnchorMode.Right, anchor);
        Assert.Equal(585, available);
    }
}
