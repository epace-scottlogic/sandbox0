using DataServer.Domain.Blockchain;

namespace DataServer.Infrastructure.Blockchain;

public class CachedTrades
{
    private readonly List<TradeUpdate> _trades = new();
    private readonly HashSet<string> _tradeIds = new();

    public bool TryAdd(TradeUpdate trade)
    {
        if (_tradeIds.Add(trade.TradeId))
        {
            _trades.Add(trade);
            return true;
        }
        return false;
    }

    public IReadOnlyList<TradeUpdate> GetRecentTrades(int count)
    {
        return _trades.OrderByDescending(t => t.Timestamp).Take(count).ToList();
    }

    public int Count => _trades.Count;
}
