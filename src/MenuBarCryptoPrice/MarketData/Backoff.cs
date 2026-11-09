namespace MenuBarCryptoPrice.MarketData;

public static class Backoff
{
    private static readonly int[] Sequence = { 1_000, 2_000, 4_000, 8_000, 15_000, 30_000 };

    public static IReadOnlyList<int> SequenceMilliseconds => Sequence;

    public static TimeSpan GetDelay(int attempt, Random? random = null)
    {
        var index = Math.Clamp(attempt, 0, Sequence.Length - 1);
        var jitter = random is null ? 0 : random.Next(0, 251);
        return TimeSpan.FromMilliseconds(Sequence[index] + jitter);
    }
}
