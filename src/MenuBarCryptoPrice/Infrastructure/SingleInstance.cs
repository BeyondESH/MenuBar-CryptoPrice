namespace MenuBarCryptoPrice.Infrastructure;

public sealed class SingleInstance : IDisposable
{
    private readonly string _eventName;
    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activationEvent;
    private readonly EventWaitHandle? _shutdownEvent;
    private bool _disposed;

    public SingleInstance(string applicationName)
    {
        _eventName = $"Local\\{applicationName}.Activate";
        var mutexName = $"Local\\{applicationName}.Mutex";

        _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        IsFirstInstance = createdNew;

        if (createdNew)
        {
            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);
            _shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            _ = Task.Run(ListenForActivation);
        }
    }

    public bool IsFirstInstance { get; }

    public event EventHandler? ActivationRequested;

    public void SignalExistingInstance()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(_eventName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }
        catch
        {
            // The other instance may be shutting down; nothing else to do.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdownEvent?.Set();
        _activationEvent?.Dispose();
        _shutdownEvent?.Dispose();

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The mutex was owned by another instance.
        }

        _mutex.Dispose();
    }

    private void ListenForActivation()
    {
        var handles = new WaitHandle[] { _activationEvent!, _shutdownEvent! };
        while (!_disposed)
        {
            var index = WaitHandle.WaitAny(handles);
            if (index == 0)
            {
                ActivationRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                break;
            }
        }
    }
}
