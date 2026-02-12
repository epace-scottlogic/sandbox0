using DataServer.Domain.Blockchain;

namespace DataServer.Application.Interfaces;

public interface IBlockchainDataClient
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }

    Task SubscribeToTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);
    Task UnsubscribeFromTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);

    event EventHandler<TradeUpdate>? TradeReceived;
    event EventHandler<TradeResponse>? SubscriptionConfirmed;
    event EventHandler? ConnectionLost;
    event EventHandler? ConnectionRestored;
}
