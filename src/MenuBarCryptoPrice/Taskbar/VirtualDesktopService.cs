using System.Runtime.InteropServices;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class VirtualDesktopService
{
    private readonly ILog _log;
    private IVirtualDesktopManager? _manager;

    public VirtualDesktopService(ILog? log = null)
    {
        _log = log ?? NullLog.Instance;
    }

    public bool TryMoveToCurrentDesktop(IntPtr window)
    {
        try
        {
            _manager ??= (IVirtualDesktopManager)new VirtualDesktopManager();
            var reference = NativeMethods.GetForegroundWindow();
            if (reference == IntPtr.Zero)
            {
                reference = NativeMethods.FindWindow("Shell_TrayWnd", null);
            }

            if (reference == IntPtr.Zero)
            {
                return false;
            }

            if (_manager.GetWindowDesktopId(reference, out var desktopId) != 0)
            {
                return false;
            }

            return _manager.MoveWindowToDesktop(window, ref desktopId) == 0;
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to move the ticker window to the current virtual desktop.", ex);
            return false;
        }
    }

    [ComImport]
    [Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
    private class VirtualDesktopManager
    {
    }

    [ComImport]
    [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);

        [PreserveSig]
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

        [PreserveSig]
        int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }
}
