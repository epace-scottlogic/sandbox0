using DataServer.Domain.Blockchain;

namespace DataServer.Application.Services;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly Dictionary<Symbol, int> _referenceCounts = new();
    private readonly Lock _lock = new();

    public bool ShouldSubscribeDownstream(Symbol symbol)
    {
        lock (_lock)
        {
            _referenceCounts.TryGetValue(symbol, out var count);
            _referenceCounts[symbol] = count + 1;
            return count == 0;
        }
    }

    public bool ShouldUnsubscribeDownstream(Symbol symbol)
    {
        lock (_lock)
        {
            if (!_referenceCounts.TryGetValue(symbol, out var count) || count <= 0)
            {
                return false;
            }

            count--;
            if (count == 0)
            {
                _referenceCounts.Remove(symbol);
                return true;
            }

            _referenceCounts[symbol] = count;
            return false;
        }
    }
}
