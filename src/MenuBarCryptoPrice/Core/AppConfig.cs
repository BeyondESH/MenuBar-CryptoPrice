using System.Text.Json.Serialization;

namespace MenuBarCryptoPrice.Core;

public enum LayoutMode
{
    SingleLine,
    CompactTwoLine
}

public enum FontSizeOption
{
    Small,
    Medium,
    Large,
    ExtraLarge
}

public enum ColorScheme
{
    GreenUp,
    RedUp
}

public enum PositionMode
{
    Taskbar,
    AboveTaskbar
}

public enum AnchorMode
{
    Right,
    Left
}

public sealed class AppConfig
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public int MaxVisibleItems { get; set; } = 4;

    public LayoutMode Layout { get; set; } = LayoutMode.SingleLine;

    public FontSizeOption FontSize { get; set; } = FontSizeOption.Medium;

    public ColorScheme ColorScheme { get; set; } = ColorScheme.GreenUp;

    public PositionMode PositionMode { get; set; } = PositionMode.Taskbar;

    public AnchorMode Anchor { get; set; } = AnchorMode.Right;

    public bool StartWithWindows { get; set; }

    public int OffsetX { get; set; }

    public List<TickerItemConfig> Items { get; set; } = new();

    [JsonIgnore]
    public IReadOnlyList<TickerItemConfig> VisibleItems
        => Items.Take(Math.Clamp(MaxVisibleItems, 1, 8)).ToList();

    public AppConfig Clone() => new()
    {
        Version = Version,
        MaxVisibleItems = MaxVisibleItems,
        Layout = Layout,
        FontSize = FontSize,
        ColorScheme = ColorScheme,
        PositionMode = PositionMode,
        Anchor = Anchor,
        StartWithWindows = StartWithWindows,
        OffsetX = OffsetX,
        Items = Items.Select(item => item.Clone()).ToList()
    };
}

public static class SettingDisplay
{
    public static string ToDisplayName(this LayoutMode value)
        => value == LayoutMode.SingleLine ? "单行" : "双行紧凑";

    public static string ToDisplayName(this FontSizeOption value) => value switch
    {
        FontSizeOption.Small => "小",
        FontSizeOption.Large => "大",
        FontSizeOption.ExtraLarge => "特大",
        _ => "中"
    };

    public static double ToFontSizeDip(this FontSizeOption value) => value switch
    {
        FontSizeOption.Small => 11,
        FontSizeOption.Large => 14,
        FontSizeOption.ExtraLarge => 16,
        _ => 12
    };

    public static string ToDisplayName(this ColorScheme value)
        => value == ColorScheme.GreenUp ? "绿涨红跌" : "红涨绿跌";

    public static string ToDisplayName(this PositionMode value)
        => value == PositionMode.Taskbar ? "任务栏内" : "任务栏上方";

    public static string ToDisplayName(this AnchorMode value)
        => value == AnchorMode.Left ? "左侧" : "右侧";
}
