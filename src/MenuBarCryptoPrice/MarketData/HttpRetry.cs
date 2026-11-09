using System.Net.Http;

namespace MenuBarCryptoPrice.MarketData;

public static class HttpRetry
{
    private const int DefaultMaxAttempts = 2;
    private static readonly int[] RetryDelaysMilliseconds = { 300, 800 };

    public static Task<string> GetStringAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
        => GetStringAsync(client, url, DefaultMaxAttempts, cancellationToken);

    public static async Task<string> GetStringAsync(
        HttpClient client,
        string url,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        maxAttempts = Math.Max(1, maxAttempts);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await client.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= maxAttempts - 1)
                {
                    throw new TimeoutException(
                        $"Bitget request timed out after {maxAttempts} attempt(s): {url}",
                        ex);
                }
            }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
            }

            var delay = RetryDelaysMilliseconds[Math.Min(attempt, RetryDelaysMilliseconds.Length - 1)];
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}
