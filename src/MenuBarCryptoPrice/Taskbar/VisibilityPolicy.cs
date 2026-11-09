namespace MenuBarCryptoPrice.Taskbar;

public enum HideReason
{
    None,
    NoTaskbar,
    AutoHide,
    Fullscreen,
    Presentation,
    ShellSurface,
    Busy
}

public sealed record VisibilityInput(
    bool HasTaskbar,
    bool IsAutoHide,
    bool IsTaskbarHidden,
    bool IsFullscreen,
    bool IsPresentation,
    bool IsBusy,
    string? ForegroundProcess,
    string? ForegroundClass);

public static class VisibilityPolicy
{
    private static readonly HashSet<string> ShellProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "StartMenuExperienceHost",
        "SearchHost",
        "ShellExperienceHost",
        "LockApp"
    };

    private static readonly HashSet<string> ShellClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "MultitaskingViewFrame"
    };

    public static HideReason Evaluate(VisibilityInput input)
    {
        if (!input.HasTaskbar)
        {
            return HideReason.NoTaskbar;
        }

        if (input.IsPresentation)
        {
            return HideReason.Presentation;
        }

        if (input.IsFullscreen)
        {
            return HideReason.Fullscreen;
        }

        if (input.ForegroundProcess is not null && ShellProcesses.Contains(input.ForegroundProcess))
        {
            return HideReason.ShellSurface;
        }

        if (input.ForegroundClass is not null && ShellClasses.Contains(input.ForegroundClass))
        {
            return HideReason.ShellSurface;
        }

        if (input.IsAutoHide && input.IsTaskbarHidden)
        {
            return HideReason.AutoHide;
        }

        return HideReason.None;
    }
}
