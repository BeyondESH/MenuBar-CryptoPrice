using MenuBarCryptoPrice.Taskbar;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using Forms = System.Windows.Forms;

namespace MenuBarCryptoPrice.Infrastructure;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _autoStartItem;
    private readonly Drawing.Icon _icon;
    private bool _disposed;

    public TrayIconService(bool autoStartEnabled, ILog? log = null)
    {
        _icon = CreateIcon();

        var menu = new Forms.ContextMenuStrip
        {
            BackColor = Drawing.Color.FromArgb(23, 26, 29),
            ForeColor = Drawing.Color.FromArgb(242, 244, 245),
            ShowImageMargin = false,
            Padding = new Forms.Padding(4),
            Renderer = new Forms.ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false }
        };
        var settingsItem = new Forms.ToolStripMenuItem("设置…");
        settingsItem.Click += (_, _) => Raise(SettingsRequested);

        var repositionItem = new Forms.ToolStripMenuItem("重新定位");
        repositionItem.Click += (_, _) => Raise(RepositionRequested);

        _autoStartItem = new Forms.ToolStripMenuItem("开机自启") { Checked = autoStartEnabled };
        _autoStartItem.Click += (_, _) => Raise(AutoStartToggleRequested);

        var aboutItem = new Forms.ToolStripMenuItem("关于");
        aboutItem.Click += (_, _) => Raise(AboutRequested);

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Raise(ExitRequested);

        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            settingsItem,
            repositionItem,
            _autoStartItem,
            new Forms.ToolStripSeparator(),
            aboutItem,
            exitItem
        });

        foreach (Forms.ToolStripItem item in menu.Items)
        {
            item.ForeColor = Drawing.Color.FromArgb(242, 244, 245);
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = "MenuBar CryptoPrice",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => Raise(SettingsRequested);
    }

    public event EventHandler? SettingsRequested;

    public event EventHandler? RepositionRequested;

    public event EventHandler? AutoStartToggleRequested;

    public event EventHandler? AboutRequested;

    public event EventHandler? ExitRequested;

    public void SetAutoStartChecked(bool enabled) => _autoStartItem.Checked = enabled;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void Raise(EventHandler? handler) => handler?.Invoke(this, EventArgs.Empty);

    private sealed class DarkColorTable : Forms.ProfessionalColorTable
    {
        private static readonly Drawing.Color Background = Drawing.Color.FromArgb(23, 26, 29);
        private static readonly Drawing.Color Hover = Drawing.Color.FromArgb(38, 42, 46);
        private static readonly Drawing.Color Separator = Drawing.Color.FromArgb(35, 42, 53);

        public override Drawing.Color ToolStripDropDownBackground => Background;

        public override Drawing.Color ImageMarginGradientBegin => Background;

        public override Drawing.Color ImageMarginGradientMiddle => Background;

        public override Drawing.Color ImageMarginGradientEnd => Background;

        public override Drawing.Color MenuItemSelected => Hover;

        public override Drawing.Color MenuItemSelectedGradientBegin => Hover;

        public override Drawing.Color MenuItemSelectedGradientEnd => Hover;

        public override Drawing.Color MenuItemBorder => Drawing.Color.Transparent;

        public override Drawing.Color SeparatorDark => Separator;

        public override Drawing.Color SeparatorLight => Separator;

        public override Drawing.Color ToolStripBorder => Drawing.Color.FromArgb(42, 51, 64);
    }

    private static Drawing.Icon CreateIcon()
    {
        try
        {
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(executable))
            {
                var embedded = Drawing.Icon.ExtractAssociatedIcon(executable);
                if (embedded is not null)
                {
                    return embedded;
                }
            }
        }
        catch
        {
            return CreateFallbackIcon();
        }

        return CreateFallbackIcon();
    }

    private static Drawing.Icon CreateFallbackIcon()
    {
        using var bitmap = new Drawing.Bitmap(32, 32);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Drawing.Color.Transparent);

            using var background = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 242, 244, 245));
            graphics.FillEllipse(background, 0, 0, 31, 31);

            using var font = new Drawing.Font("Segoe UI", 18f, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var brush = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 28, 31, 34));
            const string text = "₿";
            var size = graphics.MeasureString(text, font);
            graphics.DrawString(text, font, brush, (32 - size.Width) / 2f, (32 - size.Height) / 2f);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Drawing.Icon.FromHandle(handle);
            return (Drawing.Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
