using MenuBarCryptoPrice.Taskbar;

namespace MenuBarCryptoPrice.Tests;

public sealed class VisibilityPolicyTests
{
    private static VisibilityInput Input(
        bool hasTaskbar = true,
        bool autoHide = false,
        bool hidden = false,
        bool fullscreen = false,
        bool presentation = false,
        bool busy = false,
        string? process = "devenv",
        string? windowClass = "HwndWrapper")
        => new(hasTaskbar, autoHide, hidden, fullscreen, presentation, busy, process, windowClass);

    [Fact]
    public void NormalDesktop_IsVisible()
        => Assert.Equal(HideReason.None, VisibilityPolicy.Evaluate(Input()));

    [Fact]
    public void StartMenu_Hides()
        => Assert.Equal(HideReason.ShellSurface, VisibilityPolicy.Evaluate(Input(process: "StartMenuExperienceHost")));

    [Fact]
    public void Search_Hides()
        => Assert.Equal(HideReason.ShellSurface, VisibilityPolicy.Evaluate(Input(process: "SearchHost")));

    [Fact]
    public void TaskView_Hides()
        => Assert.Equal(HideReason.ShellSurface, VisibilityPolicy.Evaluate(Input(windowClass: "MultitaskingViewFrame")));

    [Fact]
    public void FullscreenApplication_Hides()
        => Assert.Equal(HideReason.Fullscreen, VisibilityPolicy.Evaluate(Input(fullscreen: true)));

    [Fact]
    public void PresentationMode_Hides()
        => Assert.Equal(HideReason.Presentation, VisibilityPolicy.Evaluate(Input(presentation: true)));

    [Fact]
    public void AutoHideTaskbar_OnlyHidesWhenTaskbarHidden()
    {
        Assert.Equal(HideReason.None, VisibilityPolicy.Evaluate(Input(autoHide: true, hidden: false)));
        Assert.Equal(HideReason.AutoHide, VisibilityPolicy.Evaluate(Input(autoHide: true, hidden: true)));
    }

    [Fact]
    public void MissingTaskbar_Hides()
        => Assert.Equal(HideReason.NoTaskbar, VisibilityPolicy.Evaluate(Input(hasTaskbar: false)));
}
