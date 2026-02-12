using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Caching.Memory;

namespace DataServer.Infrastructure.Blockchain;

public class InMemoryBlockchainDataRepository(IMemoryCache memoryCache) : IBlockchainDataRepository
{
    private readonly Lock _lock = new();

    public Task AddTradeAsync(TradeUpdate trade, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(trade.Symbol);

        lock (_lock)
        {
            var cachedTrades = memoryCache.GetOrCreate(cacheKey, entry => new CachedTrades())!;
            cachedTrades.TryAdd(trade);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = GetCacheKey(symbol);

        lock (_lock)
        {
            if (
                memoryCache.TryGetValue<CachedTrades>(cacheKey, out var cachedTrades)
                && cachedTrades != null
            )
            {
                return Task.FromResult(cachedTrades.GetRecentTrades(count));
            }
        }

        return Task.FromResult<IReadOnlyList<TradeUpdate>>(Array.Empty<TradeUpdate>());
    }

    public Task ClearTradesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(symbol);

        lock (_lock)
        {
            memoryCache.Remove(cacheKey);
        }

        return Task.CompletedTask;
    }

    private static string GetCacheKey(Symbol symbol) => $"trades_{symbol}";
}
