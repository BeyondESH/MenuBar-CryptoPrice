using System.Diagnostics;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class VisibilityCoordinator : IDisposable
{
    private readonly TaskbarLocator _locator;
    private readonly ILog _log;
    private readonly DispatcherTimer _timer;
    private readonly NativeMethods.WinEventDelegate _foregroundCallback;
    private readonly NativeMethods.WinEventDelegate _desktopCallback;
    private readonly IntPtr _foregroundHook;
    private readonly IntPtr _desktopHook;

    private bool _disposed;
    private bool _lastVisible;
    private HideReason _lastReason = (HideReason)(-1);

    public VisibilityCoordinator(TaskbarLocator locator, ILog? log = null)
    {
        _locator = locator;
        _log = log ?? NullLog.Instance;
        _foregroundCallback = (_, _, _, _, _, _, _) => Evaluate();
        _desktopCallback = (_, _, _, _, _, _, _) => DesktopSwitched?.Invoke();

        _foregroundHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _foregroundCallback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        _desktopHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_DESKTOPSWITCH,
            NativeMethods.EVENT_SYSTEM_DESKTOPSWITCH,
            IntPtr.Zero,
            _desktopCallback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (_, _) => Evaluate();
        _timer.Start();
    }

    public event Action<bool, HideReason>? VisibilityChanged;

    public event Action? GeometryChanged;

    public event Action? DesktopSwitched;

    public void Evaluate()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var geometry = _locator.TryGetGeometry();
            var notificationState = NativeMethods.SHQueryUserNotificationState(out var state) == 0 ? state : -1;
            var (processName, className) = GetForegroundInfo();

            var input = new VisibilityInput(
                HasTaskbar: geometry is not null,
                IsAutoHide: geometry?.IsAutoHide ?? false,
                IsTaskbarHidden: geometry?.IsHidden ?? false,
                IsFullscreen: notificationState == NativeMethods.QUNS_RUNNING_D3D_FULL_SCREEN || IsForegroundFullscreen(),
                IsPresentation: notificationState == NativeMethods.QUNS_PRESENTATION_MODE,
                IsBusy: notificationState == NativeMethods.QUNS_BUSY,
                ForegroundProcess: processName,
                ForegroundClass: className);

            var reason = VisibilityPolicy.Evaluate(input);
            var visible = reason == HideReason.None;

            GeometryChanged?.Invoke();

            if (visible != _lastVisible || reason != _lastReason)
            {
                _lastVisible = visible;
                _lastReason = reason;
                VisibilityChanged?.Invoke(visible, reason);
            }
        }
        catch (Exception ex)
        {
            _log.Warn("Visibility evaluation failed.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        if (_foregroundHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_foregroundHook);
        }

        if (_desktopHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_desktopHook);
        }
    }

    private static (string? Process, string? Class) GetForegroundInfo()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return (null, null);
        }

        string? processName = null;
        try
        {
            NativeMethods.GetWindowThreadProcessId(foreground, out var processId);
            if (processId != 0)
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
        }
        catch
        {
            // The process may have exited between calls.
        }

        string? className = null;
        try
        {
            var buffer = new StringBuilder(256);
            if (NativeMethods.GetClassName(foreground, buffer, buffer.Capacity) > 0)
            {
                className = buffer.ToString();
            }
        }
        catch
        {
        }

        return (processName, className);
    }

    private static bool IsForegroundFullscreen()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero || !NativeMethods.GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        var screenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        var screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        return rect.Left <= 0 && rect.Top <= 0 && rect.Right >= screenWidth && rect.Bottom >= screenHeight;
    }
}
