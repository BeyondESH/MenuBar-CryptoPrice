namespace MenuBarCryptoPrice.Infrastructure;

public sealed class FileLogger : ILog
{
    private readonly object _gate = new();
    private readonly string _directory;
    private readonly long _maxBytes;

    public FileLogger(string directory, long maxBytes = 2_000_000)
    {
        _directory = directory;
        _maxBytes = maxBytes;
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warn(string message, Exception? exception = null) => Write("WARN", message, exception);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, $"app-{DateTime.Now:yyyyMMdd}.log");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > _maxBytes)
                {
                    var archive = Path.Combine(_directory, $"app-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                    File.Move(path, archive, true);
                }

                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Logging must never take the application down.
            }
        }
    }
}
