using System.Text.Json.Serialization;
using MenuBarCryptoPrice.Core;

namespace MenuBarCryptoPrice.Infrastructure;

public sealed class ConfigService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly string _path;
    private readonly ILog _log;
    private readonly bool _autoSave;
    private readonly object _gate = new();
    private readonly Timer? _saveTimer;
    private AppConfig _config;
    private bool _disposed;

    public ConfigService(string path, ILog? log = null, bool autoSave = true)
    {
        _path = path;
        _log = log ?? NullLog.Instance;
        _autoSave = autoSave;
        _config = LoadInternal();
        if (autoSave)
        {
            _saveTimer = new Timer(_ => SaveNow(), null, Timeout.Infinite, Timeout.Infinite);
        }
    }

    public event EventHandler<AppConfig>? Changed;

    public AppConfig Config => _config;

    public bool IsFirstRun { get; private set; }

    public static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new MarketJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static void Migrate(AppConfig config)
    {
        if (config.Version < 2)
        {
            foreach (var item in config.Items)
            {
                item.Precision = item.Market == Market.Spot ? 2 : 1;
            }

            config.Version = 2;
        }
        else if (config.Version > AppConfig.CurrentVersion)
        {
            config.Version = AppConfig.CurrentVersion;
        }
    }

    public static void Validate(AppConfig config)
    {
        config.Version = AppConfig.CurrentVersion;
        config.MaxVisibleItems = Math.Clamp(config.MaxVisibleItems, 1, 8);

        if (!Enum.IsDefined(config.Layout))
        {
            config.Layout = LayoutMode.SingleLine;
        }

        if (!Enum.IsDefined(config.FontSize))
        {
            config.FontSize = FontSizeOption.Medium;
        }

        if (!Enum.IsDefined(config.ColorScheme))
        {
            config.ColorScheme = ColorScheme.GreenUp;
        }

        if (!Enum.IsDefined(config.PositionMode))
        {
            config.PositionMode = PositionMode.Taskbar;
        }

        if (!Enum.IsDefined(config.Anchor))
        {
            config.Anchor = AnchorMode.Right;
        }

        config.OffsetX = Math.Clamp(config.OffsetX, -4000, 4000);
        config.Items ??= new List<TickerItemConfig>();

        var seen = new HashSet<InstrumentKey>();
        var valid = new List<TickerItemConfig>();
        foreach (var item in config.Items)
        {
            if (item is null)
            {
                continue;
            }

            item.Symbol = (item.Symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (item.Symbol.Length == 0)
            {
                continue;
            }

            if (!Enum.IsDefined(item.Market))
            {
                item.Market = Market.Spot;
            }

            item.Precision = item.Precision < 0
                ? (item.Market == Market.Spot ? 2 : 1)
                : Math.Clamp(item.Precision, 0, 8);

            if (seen.Add(item.Key))
            {
                valid.Add(item);
            }
        }

        config.Items = valid;
    }

    public void Update(Action<AppConfig> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        lock (_gate)
        {
            mutate(_config);
            Validate(_config);
        }

        if (_autoSave)
        {
            _saveTimer?.Change(300, Timeout.Infinite);
        }

        Changed?.Invoke(this, _config);
    }

    public void SaveNow()
    {
        lock (_gate)
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_config, JsonOptions);
                var temp = _path + ".tmp";
                File.WriteAllText(temp, json, new UTF8Encoding(false));
                File.Move(temp, _path, true);
            }
            catch (Exception ex)
            {
                _log.Error("Failed to save configuration.", ex);
            }
        }
    }

    public void Reload()
    {
        lock (_gate)
        {
            _config = LoadInternal();
        }

        Changed?.Invoke(this, _config);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _saveTimer?.Dispose();
        SaveNow();
    }

    private AppConfig LoadInternal()
    {
        try
        {
            if (!File.Exists(_path))
            {
                IsFirstRun = true;
                var defaults = ConfigDefaults.Create();
                Validate(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_path, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            if (config is null)
            {
                throw new JsonException("Configuration deserialized to null.");
            }

            Migrate(config);
            Validate(config);
            return config;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load configuration '{_path}', falling back to defaults.", ex);
            TryBackupCorrupted();
            var fallback = ConfigDefaults.Create();
            Validate(fallback);
            return fallback;
        }
    }

    private void TryBackupCorrupted()
    {
        try
        {
            if (File.Exists(_path))
            {
                File.Copy(_path, _path + ".bak", true);
            }
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to back up the corrupted configuration file.", ex);
        }
    }
}
