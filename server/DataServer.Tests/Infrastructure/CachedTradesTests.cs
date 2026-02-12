using DataServer.Domain.Blockchain;
using DataServer.Infrastructure.Blockchain;
using static DataServer.Tests.Shared.TestTradeFactory;

namespace DataServer.Tests.Infrastructure;

public class CachedTradesTests
{
    [Fact]
    public void TryAdd_ReturnsTrueForNewTrade()
    {
        var cachedTrades = new CachedTrades();
        var trade = CreateTestTrade(Symbol.BtcUsd, "trade-1");

        var result = cachedTrades.TryAdd(trade);

        Assert.True(result);
    }

    [Fact]
    public void TryAdd_ReturnsFalseForDuplicateTradeId()
    {
        var cachedTrades = new CachedTrades();
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "same-id");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "same-id");

        cachedTrades.TryAdd(trade1);
        var result = cachedTrades.TryAdd(trade2);

        Assert.False(result);
    }

    [Fact]
    public void TryAdd_IncrementsCountForNewTrade()
    {
        var cachedTrades = new CachedTrades();
        var trade = CreateTestTrade(Symbol.BtcUsd, "trade-1");

        Assert.Equal(0, cachedTrades.Count);

        cachedTrades.TryAdd(trade);

        Assert.Equal(1, cachedTrades.Count);
    }

    [Fact]
    public void TryAdd_DoesNotIncrementCountForDuplicateTradeId()
    {
        var cachedTrades = new CachedTrades();
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "same-id");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "same-id");

        cachedTrades.TryAdd(trade1);
        cachedTrades.TryAdd(trade2);

        Assert.Equal(1, cachedTrades.Count);
    }

    [Fact]
    public void GetRecentTrades_ReturnsTradesInMostRecentFirstOrder()
    {
        var cachedTrades = new CachedTrades();
        var trade1 = CreateTestTrade(
            Symbol.BtcUsd,
            "trade-1",
            DateTimeOffset.UtcNow.AddMinutes(-2)
        );
        var trade2 = CreateTestTrade(
            Symbol.BtcUsd,
            "trade-2",
            DateTimeOffset.UtcNow.AddMinutes(-1)
        );
        var trade3 = CreateTestTrade(Symbol.BtcUsd, "trade-3", DateTimeOffset.UtcNow);

        cachedTrades.TryAdd(trade1);
        cachedTrades.TryAdd(trade2);
        cachedTrades.TryAdd(trade3);

        var result = cachedTrades.GetRecentTrades(10);

        Assert.Equal("trade-3", result[0].TradeId);
        Assert.Equal("trade-2", result[1].TradeId);
        Assert.Equal("trade-1", result[2].TradeId);
    }

    [Fact]
    public void GetRecentTrades_LimitsResultsToRequestedCount()
    {
        var cachedTrades = new CachedTrades();
        for (int i = 0; i < 10; i++)
        {
            cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, $"trade-{i}"));
        }

        var result = cachedTrades.GetRecentTrades(5);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void GetRecentTrades_ReturnsEmptyListWhenNoTrades()
    {
        var cachedTrades = new CachedTrades();

        var result = cachedTrades.GetRecentTrades(10);

        Assert.Empty(result);
    }

    [Fact]
    public void GetRecentTrades_ReturnsAllTradesWhenCountExceedsAvailable()
    {
        var cachedTrades = new CachedTrades();
        cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, "trade-1"));
        cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, "trade-2"));

        var result = cachedTrades.GetRecentTrades(100);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Count_ReturnsZeroForNewInstance()
    {
        var cachedTrades = new CachedTrades();

        Assert.Equal(0, cachedTrades.Count);
    }

    [Fact]
    public void Count_ReturnsCorrectCountAfterMultipleAdds()
    {
        var cachedTrades = new CachedTrades();
        cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, "trade-1"));
        cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, "trade-2"));
        cachedTrades.TryAdd(CreateTestTrade(Symbol.BtcUsd, "trade-3"));

        Assert.Equal(3, cachedTrades.Count);
    }
}
