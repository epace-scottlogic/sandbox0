using System.Text.Json;
using DataServer.Api.Hubs;
using DataServer.Api.Models.JsonRpc;
using DataServer.Application.Services;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Services;

public class BlockchainHubService : IHostedService
{
    private readonly IBlockchainDataService _blockchainDataService;
    private readonly IHubContext<BlockchainHub> _hubContext;
    private readonly ILogger<BlockchainHubService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BlockchainHubService(
        IBlockchainDataService blockchainDataService,
        IHubContext<BlockchainHub> hubContext,
        ILogger<BlockchainHubService> logger
    )
    {
        _blockchainDataService = blockchainDataService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting BlockchainHubService");
        _blockchainDataService.TradeReceived += OnTradeReceived;
        await _blockchainDataService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping BlockchainHubService");
        _blockchainDataService.TradeReceived -= OnTradeReceived;
        await _blockchainDataService.StopAsync(cancellationToken);
    }

    private void OnTradeReceived(object? sender, TradeUpdate trade)
    {
        _ = BroadcastTradeAsync(trade);
    }

    private async Task BroadcastTradeAsync(TradeUpdate trade)
    {
        try
        {
            var notification = JsonRpcNotification.Create(
                "trades.update",
                new
                {
                    seqnum = trade.Seqnum,
                    @event = "updated",
                    channel = "trades",
                    symbol = GetSymbolString(trade.Symbol),
                    timestamp = trade.Timestamp,
                    side = trade.Side.ToString().ToLowerInvariant(),
                    qty = trade.Qty,
                    price = trade.Price,
                    tradeId = trade.TradeId,
                }
            );

            var message = JsonSerializer.Serialize(notification, JsonOptions);
            var groupName = BlockchainHub.GetTradesGroupName(trade.Symbol);

            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);

            _logger.LogDebug(
                "Broadcasted trade {TradeId} for {Symbol} to group {GroupName}",
                trade.TradeId,
                trade.Symbol,
                groupName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast trade {TradeId}", trade.TradeId);
        }
    }

    private static string GetSymbolString(Symbol symbol) =>
        symbol switch
        {
            Symbol.EthUsd => "ETH-USD",
            Symbol.BtcUsd => "BTC-USD",
            _ => symbol.ToString(),
        };
}
