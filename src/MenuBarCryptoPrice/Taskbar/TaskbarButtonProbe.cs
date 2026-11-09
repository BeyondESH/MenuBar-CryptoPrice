using System.Windows.Automation;
using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Taskbar;

public readonly record struct TaskbarButtonGeometry(int? IconRight, int? GroupLeft, int? WidgetsRight);

public sealed class TaskbarButtonProbe
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(2);

    private readonly ILog _log;
    private readonly object _gate = new();

    private DateTimeOffset _lastAttempt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastErrorLog = DateTimeOffset.MinValue;
    private TaskbarButtonGeometry? _lastValue;
    private bool _running;

    public TaskbarButtonProbe(ILog? log = null)
    {
        _log = log ?? NullLog.Instance;
    }

    /// <summary>
    /// Returns the geometry of the visible taskbar buttons. The probe runs on a background thread
    /// and always returns the last cached value so the UI thread never blocks.
    /// </summary>
    public TaskbarButtonGeometry? TryGetButtonGeometry(IntPtr taskbar, PixelRect taskbarRect, int trayLeft)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (!_running && now - _lastAttempt >= CacheDuration)
            {
                _lastAttempt = now;
                _running = true;
                _ = Task.Run(() =>
                {
                    try
                    {
                        var value = Probe(taskbar, taskbarRect, trayLeft);
                        lock (_gate)
                        {
                            _lastValue = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_gate)
                        {
                            _lastValue = null;
                        }

                        LogProbeFailure(now, ex);
                    }
                    finally
                    {
                        lock (_gate)
                        {
                            _running = false;
                        }
                    }
                });
            }

            return _lastValue;
        }
    }

    private static TaskbarButtonGeometry? Probe(IntPtr taskbar, PixelRect taskbarRect, int trayLeft)
    {
        var root = AutomationElement.FromHandle(taskbar);
        if (root is null)
        {
            return null;
        }

        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
        var buttons = root.FindAll(TreeScope.Descendants, condition);

        int? iconRight = null;
        int? groupLeft = null;
        int? widgetsRight = null;
        var widgetsBoundary = taskbarRect.Left + 20;

        for (var index = 0; index < buttons.Count; index++)
        {
            AutomationElement button;
            try
            {
                button = buttons[index];
            }
            catch (ElementNotAvailableException)
            {
                continue;
            }

            if (button is null || button.Current.IsOffscreen)
            {
                continue;
            }

            var rect = button.Current.BoundingRectangle;
            if (rect.IsEmpty || rect.Width <= 0)
            {
                continue;
            }

            if (rect.Top < taskbarRect.Top - 2 || rect.Bottom > taskbarRect.Bottom + 2)
            {
                continue;
            }

            if (rect.Left < taskbarRect.Left || rect.Left >= trayLeft - 1)
            {
                continue;
            }

            var left = (int)Math.Round(rect.Left);
            var right = (int)Math.Round(rect.Right);

            if (left <= widgetsBoundary)
            {
                widgetsRight = widgetsRight is null ? right : Math.Max(widgetsRight.Value, right);
                continue;
            }

            groupLeft = groupLeft is null ? left : Math.Min(groupLeft.Value, left);
            iconRight = iconRight is null ? right : Math.Max(iconRight.Value, right);
        }

        if (iconRight is null && widgetsRight is null)
        {
            return null;
        }

        return new TaskbarButtonGeometry(iconRight, groupLeft, widgetsRight);
    }

    private void LogProbeFailure(DateTimeOffset now, Exception exception)
    {
        lock (_gate)
        {
            if (now - _lastErrorLog < TimeSpan.FromMinutes(1))
            {
                return;
            }

            _lastErrorLog = now;
        }

        _log.Warn("UI Automation taskbar probe failed; falling back to the tray anchor.", exception);
    }
}
