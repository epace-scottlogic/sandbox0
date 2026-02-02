using DataServer.Domain.Blockchain;

namespace DataServer.Application.Interfaces;

public interface IBlockchainDataRepository
{
    Task AddTradeAsync(TradeUpdate trade, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    );
    Task ClearTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
