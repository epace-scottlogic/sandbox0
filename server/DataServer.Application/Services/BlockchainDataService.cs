using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;

namespace DataServer.Application.Services;

public class BlockchainDataService : IBlockchainDataService
{
    private readonly IBlockchainDataClient _dataClient;
    private readonly IBlockchainDataRepository _repository;
    private readonly ISubscriptionManager _subscriptionManager;
    private EventHandler<TradeUpdate>? _tradeReceivedHandler;
    private EventHandler? _connectionLostHandler;
    private EventHandler? _connectionRestoredHandler;

    public event EventHandler<TradeUpdate>? TradeReceived;
    public event EventHandler? ConnectionLost;
    public event EventHandler? ConnectionRestored;

    public BlockchainDataService(
        IBlockchainDataClient dataClient,
        IBlockchainDataRepository repository,
        ISubscriptionManager subscriptionManager
    )
    {
        _dataClient = dataClient;
        _repository = repository;
        _subscriptionManager = subscriptionManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _tradeReceivedHandler = (sender, trade) => _ = OnTradeReceivedAsync(trade);
        _connectionLostHandler = (sender, args) => ConnectionLost?.Invoke(this, EventArgs.Empty);
        _connectionRestoredHandler = (sender, args) =>
            ConnectionRestored?.Invoke(this, EventArgs.Empty);

        _dataClient.TradeReceived += _tradeReceivedHandler;
        _dataClient.ConnectionLost += _connectionLostHandler;
        _dataClient.ConnectionRestored += _connectionRestoredHandler;
        await _dataClient.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _dataClient.TradeReceived -= _tradeReceivedHandler;
        _dataClient.ConnectionLost -= _connectionLostHandler;
        _dataClient.ConnectionRestored -= _connectionRestoredHandler;
        await _dataClient.DisconnectAsync(cancellationToken);
    }

    public async Task SubscribeToTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        if (_subscriptionManager.ShouldSubscribeDownstream(symbol))
        {
            await _dataClient.SubscribeToTradesAsync(symbol, cancellationToken);
        }
    }

    public async Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        if (_subscriptionManager.ShouldUnsubscribeDownstream(symbol))
        {
            await _dataClient.UnsubscribeFromTradesAsync(symbol, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        return await _repository.GetRecentTradesAsync(symbol, count, cancellationToken);
    }

    private async Task OnTradeReceivedAsync(TradeUpdate trade)
    {
        await _repository.AddTradeAsync(trade);
        TradeReceived?.Invoke(this, trade);
    }
}
