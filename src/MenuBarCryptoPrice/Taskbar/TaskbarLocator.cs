using System.Runtime.InteropServices;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class TaskbarLocator
{
    private readonly ILog _log;
    private readonly TaskbarButtonProbe _probe;
    private DateTimeOffset _lastMissingLog = DateTimeOffset.MinValue;

    public TaskbarLocator(ILog? log = null, TaskbarButtonProbe? probe = null)
    {
        _log = log ?? NullLog.Instance;
        _probe = probe ?? new TaskbarButtonProbe(_log);
    }

    public TaskbarGeometry? TryGetGeometry()
    {
        try
        {
            var taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (taskbar == IntPtr.Zero)
            {
                LogMissingTaskbar();
                return null;
            }

            if (!NativeMethods.GetWindowRect(taskbar, out var rect))
            {
                return null;
            }

            var taskbarRect = new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            var trayNotify = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
            var trayLeft = trayNotify != IntPtr.Zero && NativeMethods.GetWindowRect(trayNotify, out var trayRect)
                ? trayRect.Left
                : taskbarRect.Right;

            var dpi = (int)Math.Max(96, NativeMethods.GetDpiForWindow(taskbar));
            var isAutoHide = GetAutoHideState();
            var screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            var isHidden = isAutoHide && (taskbarRect.Top >= screenHeight - 2 || taskbarRect.Height <= 2);
            var buttons = _probe.TryGetButtonGeometry(taskbar, taskbarRect, trayLeft);

            return new TaskbarGeometry(
                taskbarRect,
                trayLeft,
                buttons?.IconRight,
                buttons?.GroupLeft,
                buttons?.WidgetsRight,
                dpi,
                isAutoHide,
                isHidden);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to read taskbar geometry.", ex);
            return null;
        }
    }

    private static bool GetAutoHideState()
    {
        var data = new NativeMethods.APPBARDATA
        {
            cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>()
        };
        var state = NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETSTATE, ref data);
        return ((uint)state.ToUInt64() & NativeMethods.ABS_AUTOHIDE) != 0;
    }

    private void LogMissingTaskbar()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastMissingLog < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastMissingLog = now;
        _log.Warn("Shell_TrayWnd was not found; falling back to the working-area anchor.");
    }
}
