using System.Windows;
using System.Windows.Interop;
using MenuBarCryptoPrice.Core;
using Forms = System.Windows.Forms;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class WindowPlacementService
{
    public bool Place(
        Window window,
        TaskbarGeometry? geometry,
        double widthDip,
        double heightDip,
        int offsetX,
        PositionMode mode,
        AnchorMode anchor)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        if (geometry is null)
        {
            var workArea = Forms.Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
            var widthPx = (int)Math.Ceiling(widthDip);
            var heightPx = (int)Math.Ceiling(heightDip);
            Move(handle, workArea.Right - widthPx - 8 + offsetX, workArea.Bottom - heightPx, widthPx, heightPx);
            return true;
        }

        var value = geometry.Value;
        var scale = value.Scale;
        var width = (int)Math.Ceiling(widthDip * scale);
        var height = (int)Math.Ceiling(heightDip * scale);

        int x;
        int y;
        if (mode == PositionMode.AboveTaskbar)
        {
            x = anchor == AnchorMode.Left
                ? value.Taskbar.Left + 8 + offsetX
                : value.Taskbar.Right - width - 8 + offsetX;
            y = value.Taskbar.Top - height;
        }
        else if (anchor == AnchorMode.Left)
        {
            x = value.LeftAnchorStartPixels + offsetX;
            y = value.Taskbar.Top + (value.Taskbar.Height - height) / 2;
            var rightGuard = (value.GroupLeft ?? value.TrayLeft) - 8;
            if (x + width > rightGuard)
            {
                // The left slot shrank between layout and placement; fall back to the tray anchor.
                x = value.TrayLeft - 8 - width + offsetX;
            }
        }
        else
        {
            x = value.TrayLeft - 8 - width + offsetX;
            y = value.Taskbar.Top + (value.Taskbar.Height - height) / 2;
            var leftGuard = (value.IconRight ?? value.Taskbar.Left) + 8;
            if (x < leftGuard)
            {
                x = leftGuard;
            }
        }

        window.Width = widthDip;
        window.Height = heightDip;
        Move(handle, x, y, width, height);
        return true;
    }

    public void BringToTop(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private static void Move(IntPtr handle, int x, int y, int width, int height)
    {
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            x,
            y,
            Math.Max(1, width),
            Math.Max(1, height),
            NativeMethods.SWP_NOACTIVATE);
    }
}
