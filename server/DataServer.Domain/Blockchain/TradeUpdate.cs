namespace DataServer.Domain.Blockchain;

public record TradeUpdate(
    int Seqnum,
    Event Event,
    Channel Channel,
    Symbol Symbol,
    DateTimeOffset Timestamp,
    Side Side,
    decimal Qty,
    decimal Price,
    string TradeId
);
