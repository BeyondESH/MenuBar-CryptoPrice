using System.Windows;
using System.Windows.Interop;

namespace MenuBarCryptoPrice.Taskbar;

public partial class TickerWindow : Window
{
    public TickerWindow(TickerStripViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Strip.EmptyStateClicked += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        SourceInitialized += (_, _) => ApplyToolWindowStyles();
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
            {
                Strip.CloseDetail();
            }
        };
        Deactivated += (_, _) => Strip.CloseDetail();
    }

    public event EventHandler? SettingsRequested;

    public event EventHandler? RepositionRequested;

    public event EventHandler? AutoStartToggleRequested;

    public event EventHandler? AboutRequested;

    public event EventHandler? ExitRequested;

    public bool AutoStartMenuChecked => AutoStartMenuItem.IsChecked;

    public bool AutoStartChecked
    {
        set => AutoStartMenuItem.IsChecked = value;
    }

    private void ApplyToolWindowStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        style |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
        => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void OnRepositionClick(object sender, RoutedEventArgs e)
        => RepositionRequested?.Invoke(this, EventArgs.Empty);

    private void OnAutoStartClick(object sender, RoutedEventArgs e)
        => AutoStartToggleRequested?.Invoke(this, EventArgs.Empty);

    private void OnAboutClick(object sender, RoutedEventArgs e)
        => AboutRequested?.Invoke(this, EventArgs.Empty);

    private void OnExitClick(object sender, RoutedEventArgs e)
        => ExitRequested?.Invoke(this, EventArgs.Empty);
}
