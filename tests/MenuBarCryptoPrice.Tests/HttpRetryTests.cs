using System.Net;
using System.Net.Http;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Tests;

public sealed class HttpRetryTests
{
    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();

        public int Calls { get; private set; }

        public void Enqueue(Func<HttpResponseMessage> response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_responses.Dequeue()());
        }
    }

    private sealed class DelayHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task GetStringAsync_RetriesTransientHttpFailures()
    {
        var handler = new SequenceHandler();
        handler.Enqueue(() => Json("unavailable", HttpStatusCode.ServiceUnavailable));
        handler.Enqueue(() => Json("unavailable", HttpStatusCode.ServiceUnavailable));
        handler.Enqueue(() => Json("ok"));
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };

        var text = await HttpRetry.GetStringAsync(http, "https://example.test", 3, CancellationToken.None);

        Assert.Equal("ok", text);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task GetStringAsync_DoesNotRetryCallerCancellation()
    {
        var handler = new DelayHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => HttpRetry.GetStringAsync(http, "https://example.test", 3, cts.Token));

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetStringAsync_ConvertsTimeoutsAfterRetries()
    {
        var handler = new DelayHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };

        await Assert.ThrowsAsync<TimeoutException>(
            () => HttpRetry.GetStringAsync(http, "https://example.test", 2, CancellationToken.None));

        Assert.Equal(2, handler.Calls);
    }
}
