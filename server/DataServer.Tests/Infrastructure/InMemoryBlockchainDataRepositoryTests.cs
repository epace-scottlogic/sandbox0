using DataServer.Domain.Blockchain;
using DataServer.Infrastructure.Blockchain;
using Microsoft.Extensions.Caching.Memory;
using static DataServer.Tests.Shared.TestTradeFactory;

namespace DataServer.Tests.Infrastructure;

public class InMemoryBlockchainDataRepositoryTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly InMemoryBlockchainDataRepository _repository;

    public InMemoryBlockchainDataRepositoryTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _repository = new InMemoryBlockchainDataRepository(_memoryCache);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task AddTradeAsync_StoresTradeInCache()
    {
        var trade = CreateTestTrade(Symbol.BtcUsd, "trade-1");

        await _repository.AddTradeAsync(trade);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        Assert.Single(trades);
        Assert.Equal(trade, trades[0]);
    }

    [Fact]
    public async Task AddTradeAsync_StoresMultipleTradesForSameSymbol()
    {
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "trade-1");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "trade-2");
        var trade3 = CreateTestTrade(Symbol.BtcUsd, "trade-3");

        await _repository.AddTradeAsync(trade1);
        await _repository.AddTradeAsync(trade2);
        await _repository.AddTradeAsync(trade3);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        Assert.Equal(3, trades.Count);
    }

    [Fact]
    public async Task AddTradeAsync_StoresTradesForDifferentSymbolsSeparately()
    {
        var btcTrade = CreateTestTrade(Symbol.BtcUsd, "btc-trade");
        var ethTrade = CreateTestTrade(Symbol.EthUsd, "eth-trade");

        await _repository.AddTradeAsync(btcTrade);
        await _repository.AddTradeAsync(ethTrade);

        var btcTrades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        var ethTrades = await _repository.GetRecentTradesAsync(Symbol.EthUsd, 10);

        Assert.Single(btcTrades);
        Assert.Single(ethTrades);
        Assert.Equal(btcTrade, btcTrades[0]);
        Assert.Equal(ethTrade, ethTrades[0]);
    }

    [Fact]
    public async Task GetRecentTradesAsync_ReturnsTradesInMostRecentFirstOrder()
    {
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

        await _repository.AddTradeAsync(trade1);
        await _repository.AddTradeAsync(trade2);
        await _repository.AddTradeAsync(trade3);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);

        Assert.Equal("trade-3", trades[0].TradeId);
        Assert.Equal("trade-2", trades[1].TradeId);
        Assert.Equal("trade-1", trades[2].TradeId);
    }

    [Fact]
    public async Task GetRecentTradesAsync_LimitsResultsToRequestedCount()
    {
        for (int i = 0; i < 10; i++)
        {
            var trade = CreateTestTrade(Symbol.BtcUsd, $"trade-{i}");
            await _repository.AddTradeAsync(trade);
        }

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 5);

        Assert.Equal(5, trades.Count);
    }

    [Fact]
    public async Task GetRecentTradesAsync_ReturnsEmptyListForSymbolWithNoTrades()
    {
        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);

        Assert.Empty(trades);
    }

    [Fact]
    public async Task GetRecentTradesAsync_ReturnsAllTradesWhenCountExceedsAvailable()
    {
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "trade-1");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "trade-2");

        await _repository.AddTradeAsync(trade1);
        await _repository.AddTradeAsync(trade2);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 100);

        Assert.Equal(2, trades.Count);
    }

    [Fact]
    public async Task ClearTradesAsync_RemovesAllTradesForSymbol()
    {
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "trade-1");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "trade-2");

        await _repository.AddTradeAsync(trade1);
        await _repository.AddTradeAsync(trade2);

        await _repository.ClearTradesAsync(Symbol.BtcUsd);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        Assert.Empty(trades);
    }

    [Fact]
    public async Task ClearTradesAsync_DoesNotAffectOtherSymbols()
    {
        var btcTrade = CreateTestTrade(Symbol.BtcUsd, "btc-trade");
        var ethTrade = CreateTestTrade(Symbol.EthUsd, "eth-trade");

        await _repository.AddTradeAsync(btcTrade);
        await _repository.AddTradeAsync(ethTrade);

        await _repository.ClearTradesAsync(Symbol.BtcUsd);

        var btcTrades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        var ethTrades = await _repository.GetRecentTradesAsync(Symbol.EthUsd, 10);

        Assert.Empty(btcTrades);
        Assert.Single(ethTrades);
    }

    [Fact]
    public async Task ClearTradesAsync_DoesNotThrowWhenSymbolHasNoTrades()
    {
        await _repository.ClearTradesAsync(Symbol.BtcUsd);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        Assert.Empty(trades);
    }

    [Fact]
    public async Task AddTradeAsync_DoesNotAddDuplicateTradeId()
    {
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "same-trade-id");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "same-trade-id");

        await _repository.AddTradeAsync(trade1);
        await _repository.AddTradeAsync(trade2);

        var trades = await _repository.GetRecentTradesAsync(Symbol.BtcUsd, 10);
        Assert.Single(trades);
    }
}
