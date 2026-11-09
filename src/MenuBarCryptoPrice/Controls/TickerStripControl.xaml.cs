using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MenuBarCryptoPrice.Taskbar;

namespace MenuBarCryptoPrice.Controls;

public partial class TickerStripControl : UserControl
{
    private Popup? _activePopup;
    private DispatcherTimer? _outsideClickTimer;
    private NativeMethods.LowLevelMouseProc? _mouseHookProc;
    private IntPtr _mouseHook;
    private DateTime _ignoreOutsideClicksUntil;

    public TickerStripControl()
    {
        InitializeComponent();
        Unloaded += (_, _) => CloseDetail();
    }

    public event EventHandler? EmptyStateClicked;

    private void OnEmptyClick(object sender, RoutedEventArgs e)
        => EmptyStateClicked?.Invoke(this, EventArgs.Empty);

    private void OnStripMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var item = FindDataContext<StripItemViewModel>(source);
        if (item is null || string.IsNullOrWhiteSpace(item.Tooltip))
        {
            return;
        }

        ShowDetail(source as FrameworkElement ?? this, item.Tooltip);
        e.Handled = true;
    }

    private void OnOverflowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not TickerStripViewModel viewModel
            || string.IsNullOrWhiteSpace(viewModel.OverflowTooltip))
        {
            return;
        }

        ShowDetail((FrameworkElement)sender, viewModel.OverflowTooltip);
        e.Handled = true;
    }

    public void CloseDetail()
    {
        var popup = _activePopup;
        _activePopup = null;
        StopOutsideClickMonitor();

        if (popup is not null)
        {
            popup.SetCurrentValue(Popup.IsOpenProperty, false);
        }
    }

    private void ShowDetail(FrameworkElement target, string text)
    {
        CloseDetail();

        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Top,
            VerticalOffset = -8,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            Focusable = false,
            Child = BuildPopupContent(text)
        };

        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activePopup, popup))
            {
                _activePopup = null;
                StopOutsideClickMonitor();
            }
        };

        _activePopup = popup;
        _ignoreOutsideClicksUntil = DateTime.UtcNow.AddMilliseconds(250);
        popup.IsOpen = true;
        StartOutsideClickMonitor();
    }

    private void StartOutsideClickMonitor()
    {
        _ignoreOutsideClicksUntil = DateTime.UtcNow.AddMilliseconds(200);

        if (_mouseHook == IntPtr.Zero)
        {
            _mouseHookProc ??= OnLowLevelMouse;
            _mouseHook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _mouseHookProc,
                NativeMethods.GetModuleHandle(null),
                0);
        }

        if (_mouseHook != IntPtr.Zero)
        {
            return;
        }

        if (_outsideClickTimer is null)
        {
            _outsideClickTimer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };
            _outsideClickTimer.Tick += OnOutsideClickTick;
        }

        _outsideClickTimer.Start();
    }

    private void StopOutsideClickMonitor()
    {
        _outsideClickTimer?.Stop();

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr OnLowLevelMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _activePopup is not null)
        {
            var message = wParam.ToInt32();
            if (message is NativeMethods.WM_LBUTTONDOWN
                or NativeMethods.WM_RBUTTONDOWN
                or NativeMethods.WM_MBUTTONDOWN)
            {
                var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if (DateTime.UtcNow >= _ignoreOutsideClicksUntil
                    && !IsPointInsidePopup(data.pt.X, data.pt.Y))
                {
                    Dispatcher.BeginInvoke(new Action(CloseDetail), DispatcherPriority.Input);
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool IsPointInsidePopup(int x, int y)
    {
        if (_activePopup?.Child is not FrameworkElement child
            || child.ActualWidth <= 0
            || child.ActualHeight <= 0)
        {
            return true;
        }

        try
        {
            var topLeft = child.PointToScreen(new Point(0, 0));
            var bottomRight = child.PointToScreen(new Point(child.ActualWidth, child.ActualHeight));
            return x >= topLeft.X
                && x <= bottomRight.X
                && y >= topLeft.Y
                && y <= bottomRight.Y;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private void OnOutsideClickTick(object? sender, EventArgs e)
    {
        if (_activePopup is not { IsOpen: true } popup)
        {
            StopOutsideClickMonitor();
            return;
        }

        if (DateTime.UtcNow < _ignoreOutsideClicksUntil || !IsMouseButtonDown())
        {
            return;
        }

        if (!IsCursorInsidePopup(popup))
        {
            CloseDetail();
        }
    }

    private static bool IsMouseButtonDown()
        => IsKeyDown(NativeMethods.VK_LBUTTON)
            || IsKeyDown(NativeMethods.VK_RBUTTON)
            || IsKeyDown(NativeMethods.VK_MBUTTON);

    private static bool IsKeyDown(int virtualKey)
        => (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private static bool IsCursorInsidePopup(Popup popup)
    {
        if (popup.Child is not FrameworkElement child
            || child.ActualWidth <= 0
            || child.ActualHeight <= 0
            || !NativeMethods.GetCursorPos(out var cursor))
        {
            return false;
        }

        try
        {
            var topLeft = child.PointToScreen(new Point(0, 0));
            var bottomRight = child.PointToScreen(new Point(child.ActualWidth, child.ActualHeight));
            return cursor.X >= topLeft.X
                && cursor.X <= bottomRight.X
                && cursor.Y >= topLeft.Y
                && cursor.Y <= bottomRight.Y;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Border BuildPopupContent(string text) => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(0xF2, 0x17, 0x1A, 0x1D)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x33, 0x40)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(12, 9, 12, 9),
        Child = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF2, 0xF4, 0xF5)),
            FontSize = 12,
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
            LineHeight = 18,
            TextWrapping = TextWrapping.NoWrap
        }
    };

    private static T? FindDataContext<T>(DependencyObject source) where T : class
    {
        var current = source;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.DataContext is T value)
            {
                return value;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
