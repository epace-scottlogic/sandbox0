using DataServer.Domain.Blockchain;

namespace DataServer.Application.Services;

public interface IBlockchainDataService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    Task SubscribeToTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);
    Task UnsubscribeFromTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    );

    event EventHandler<TradeUpdate>? TradeReceived;
    event EventHandler? ConnectionLost;
    event EventHandler? ConnectionRestored;
}
