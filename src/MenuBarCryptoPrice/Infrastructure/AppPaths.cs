namespace MenuBarCryptoPrice.Infrastructure;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MenuBarCryptoPrice");

    public static string ConfigFile => Path.Combine(Root, "config.json");

    public static string CacheDirectory => Path.Combine(Root, "cache");

    public static string LogDirectory => Path.Combine(Root, "logs");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
