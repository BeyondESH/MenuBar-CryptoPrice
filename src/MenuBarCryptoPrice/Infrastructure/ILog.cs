namespace MenuBarCryptoPrice.Infrastructure;

public interface ILog
{
    void Info(string message);

    void Warn(string message, Exception? exception = null);

    void Error(string message, Exception? exception = null);
}

public sealed class NullLog : ILog
{
    public static NullLog Instance { get; } = new();

    private NullLog()
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message, Exception? exception = null)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
