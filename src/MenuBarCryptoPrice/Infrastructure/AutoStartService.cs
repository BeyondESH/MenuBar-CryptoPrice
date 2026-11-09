using Microsoft.Win32;

namespace MenuBarCryptoPrice.Infrastructure;

public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MenuBarCryptoPrice";

    private readonly ILog _log;

    public AutoStartService(ILog? log = null)
    {
        _log = log ?? NullLog.Instance;
    }

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(ValueName) is string value && value.Length > 0;
            }
            catch (Exception ex)
            {
                _log.Warn("Failed to read the autostart registry value.", ex);
                return false;
            }
        }
    }

    public void SetEnabled(bool enabled, string? executablePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (key is null)
            {
                _log.Warn("Autostart registry key could not be created.");
                return;
            }

            if (enabled)
            {
                var executable = executablePath
                    ?? Environment.ProcessPath
                    ?? throw new InvalidOperationException("Executable path is unavailable.");
                key.SetValue(ValueName, $"\"{executable}\" --autostart", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }
        }
        catch (Exception ex)
        {
            _log.Error("Failed to update the autostart registry value.", ex);
        }
    }
}
