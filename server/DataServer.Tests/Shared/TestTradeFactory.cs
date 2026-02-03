using DataServer.Domain.Blockchain;

namespace DataServer.Tests.Shared;

public static class TestTradeFactory
{
    public static TradeUpdate CreateTestTrade(
        Symbol symbol,
        string tradeId = "test-trade-1",
        DateTimeOffset? timestamp = null
    )
    {
        return new TradeUpdate(
            Seqnum: 1,
            Event: Event.Updated,
            Channel: Channel.Trades,
            Symbol: symbol,
            Timestamp: timestamp ?? DateTimeOffset.UtcNow,
            Side: Side.Buy,
            Qty: 1.5m,
            Price: 50000m,
            TradeId: tradeId
        );
    }
}
