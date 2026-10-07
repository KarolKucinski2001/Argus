namespace _bootstrap_scaffold.MarketData;

public interface IMarketDataRequestBudget
{
    bool TryAcquire(int limit, TimeSpan window);
}

public sealed class InMemoryMarketDataRequestBudget : IMarketDataRequestBudget
{
    private readonly Queue<DateTimeOffset> requests = new();
    private readonly Lock gate = new();

    public bool TryAcquire(int limit, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window;

        lock (gate)
        {
            while (requests.Count > 0 && requests.Peek() <= cutoff)
            {
                requests.Dequeue();
            }

            if (requests.Count >= limit)
            {
                return false;
            }

            requests.Enqueue(now);
            return true;
        }
    }
}
