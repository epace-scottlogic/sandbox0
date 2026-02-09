using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Logging;

namespace DataServer.Connectors.Blockchain;

public class StubBlockchainDataClient : IBlockchainDataClient
{
    private readonly ILogger<StubBlockchainDataClient> _logger;
    private bool _isConnected;

    public event EventHandler<TradeUpdate>? TradeReceived;
    public event EventHandler<TradeResponse>? SubscriptionConfirmed;

    public bool IsConnected => _isConnected;

    public StubBlockchainDataClient(ILogger<StubBlockchainDataClient> logger)
    {
        _logger = logger;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        _logger.LogInformation("StubBlockchainDataSource connected (stub implementation)");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        _logger.LogInformation("StubBlockchainDataSource disconnected (stub implementation)");
        return Task.CompletedTask;
    }

    public Task SubscribeToTradesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StubBlockchainDataSource subscribed to trades for {Symbol} (stub implementation)",
            symbol
        );

        var response = new TradeResponse(
            Seqnum: 0,
            Event: Event.Subscribed,
            Channel: Channel.Trades,
            Symbol: symbol
        );
        SubscriptionConfirmed?.Invoke(this, response);

        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "StubBlockchainDataSource unsubscribed from trades for {Symbol} (stub implementation)",
            symbol
        );

        var response = new TradeResponse(
            Seqnum: 0,
            Event: Event.Unsubscribed,
            Channel: Channel.Trades,
            Symbol: symbol
        );
        SubscriptionConfirmed?.Invoke(this, response);

        return Task.CompletedTask;
    }
}
