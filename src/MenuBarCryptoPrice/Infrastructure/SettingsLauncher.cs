using System.Diagnostics;

namespace MenuBarCryptoPrice.Infrastructure;

public static class SettingsLauncher
{
    public static bool TryOpen(ILog log, out string error, string? arguments = null)
    {
        var path = FindExecutable();
        if (path is null)
        {
            error = "未找到 WinUI 3 设置客户端。请先构建 MenuBarCryptoPrice.Settings.WinUI，或将其发布到主程序旁的 settings 目录。";
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory
            };
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startInfo.Arguments = arguments;
            }

            Process.Start(startInfo);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to launch the WinUI settings app '{path}'.", ex);
            error = "启动设置客户端失败：" + ex.Message;
            return false;
        }
    }

    public static string? FindExecutable()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new List<string>
        {
            Path.Combine(baseDirectory, "MenuBarCryptoPrice.Settings.WinUI.exe"),
            Path.Combine(baseDirectory, "settings", "MenuBarCryptoPrice.Settings.WinUI.exe")
        };

        var settingsRoot = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "MenuBarCryptoPrice.Settings.WinUI",
            "bin"));

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            foreach (var platform in new[] { string.Empty, "x64" })
            {
                candidates.Add(Path.Combine(
                    settingsRoot,
                    platform,
                    configuration,
                    "net8.0-windows10.0.19041.0",
                    "win-x64",
                    "MenuBarCryptoPrice.Settings.WinUI.exe"));
            }
        }

        return candidates
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
