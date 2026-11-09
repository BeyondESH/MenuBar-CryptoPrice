using MenuBarCryptoPrice.Infrastructure;

namespace MenuBarCryptoPrice.Tests;

public sealed class HostLifetimeWatcherTests
{
    [Fact]
    public async Task HostExited_Fires_WhenHostReleasesMutex()
    {
        var name = CreateMutexName();
        using var host = new Mutex(initiallyOwned: true, name, out var createdNew);
        Assert.True(createdNew);
        using var watcher = HostLifetimeWatcher.TryAttach(name);
        Assert.NotNull(watcher);

        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher!.HostExited += (_, _) => exited.TrySetResult();

        host.ReleaseMutex();

        await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HostExited_Fires_WhenHostThreadDiesWithoutReleasingMutex()
    {
        var name = CreateMutexName();
        using var ready = new ManualResetEventSlim();
        Mutex? hostMutex = null;
        var hostThread = new Thread(() =>
        {
            hostMutex = new Mutex(initiallyOwned: true, name, out _);
            ready.Set();
        });
        hostThread.Start();
        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(hostThread.Join(TimeSpan.FromSeconds(5)));

        using var watcher = HostLifetimeWatcher.TryAttach(name);
        Assert.NotNull(watcher);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher!.HostExited += (_, _) => exited.TrySetResult();

        await exited.Task.WaitAsync(TimeSpan.FromSeconds(5));
        hostMutex?.Dispose();
    }

    [Fact]
    public void TryAttach_ReturnsNull_WhenHostIsNotRunning()
    {
        Assert.Null(HostLifetimeWatcher.TryAttach(CreateMutexName()));
    }

    private static string CreateMutexName() =>
        "Local\\MenuBarCryptoPrice.Tests." + Guid.NewGuid().ToString("N");
}
