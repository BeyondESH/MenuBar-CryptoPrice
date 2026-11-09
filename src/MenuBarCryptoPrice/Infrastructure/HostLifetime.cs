using System;
using System.Threading;

namespace MenuBarCryptoPrice.Infrastructure;

/// <summary>
/// Shared lifetime contract between the WPF taskbar host and the WinUI settings client.
/// The host owns <see cref="MutexName"/> while it runs; the settings client waits on the
/// same mutex and shuts itself down when the host releases it or dies.
/// </summary>
public static class HostLifetime
{
    public const string ApplicationName = "MenuBarCryptoPrice";

    public static string MutexName => $"Local\\{ApplicationName}.Mutex";
}

/// <summary>
/// Watches the taskbar host and raises <see cref="HostExited"/> once the host process
/// releases its single-instance mutex or terminates without releasing it.
/// </summary>
public sealed class HostLifetimeWatcher : IDisposable
{
    private readonly Mutex _mutex;
    private readonly ManualResetEvent _stop = new(false);
    private readonly Thread _thread;
    private int _disposed;

    private HostLifetimeWatcher(Mutex mutex)
    {
        _mutex = mutex;
        _thread = new Thread(Watch)
        {
            IsBackground = true,
            Name = "HostLifetimeWatcher"
        };
        _thread.Start();
    }

    public event EventHandler? HostExited;

    /// <summary>
    /// Attaches to a running host. Returns <c>null</c> when no host is running so a
    /// settings window opened on its own stays usable.
    /// </summary>
    public static HostLifetimeWatcher? TryAttach(string mutexName)
    {
        if (!Mutex.TryOpenExisting(mutexName, out var mutex))
        {
            return null;
        }

        return new HostLifetimeWatcher(mutex);
    }

    private void Watch()
    {
        try
        {
            if (WaitHandle.WaitAny(new WaitHandle[] { _mutex, _stop }) != 0)
            {
                return;
            }
        }
        catch (AbandonedMutexException)
        {
            // The host died without releasing the mutex; it is gone either way.
        }

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // The wait ended because of Dispose() rather than acquiring the mutex.
        }

        HostExited?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _stop.Set();
        if (Thread.CurrentThread != _thread)
        {
            _thread.Join(TimeSpan.FromSeconds(2));
        }

        _stop.Dispose();
        _mutex.Dispose();
    }
}
