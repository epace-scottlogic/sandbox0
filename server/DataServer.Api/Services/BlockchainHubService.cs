using System.Text.Json;
using DataServer.Api.Hubs;
using DataServer.Api.Models.JsonRpc;
using DataServer.Application.Services;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Services;

public class BlockchainHubService(
    IBlockchainDataService blockchainDataService,
    IHubContext<BlockchainHub> hubContext,
    Serilog.ILogger logger
) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.Information("Starting {ServiceName}", nameof(BlockchainHubService));
        blockchainDataService.TradeReceived += OnTradeReceived;
        blockchainDataService.ConnectionLost += OnConnectionLost;
        blockchainDataService.ConnectionRestored += OnConnectionRestored;
        await blockchainDataService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.Information("Stopping {ServiceName}", nameof(BlockchainHubService));
        blockchainDataService.TradeReceived -= OnTradeReceived;
        blockchainDataService.ConnectionLost -= OnConnectionLost;
        blockchainDataService.ConnectionRestored -= OnConnectionRestored;
        await blockchainDataService.StopAsync(cancellationToken);
    }

    private void OnTradeReceived(object? sender, TradeUpdate trade)
    {
        _ = BroadcastTradeAsync(trade);
    }

    private void OnConnectionLost(object? sender, EventArgs args)
    {
        _ = NotifyConnectionLostAsync();
    }

    private void OnConnectionRestored(object? sender, EventArgs args)
    {
        _ = NotifyConnectionRestoredAsync();
    }

    private async Task NotifyConnectionLostAsync()
    {
        try
        {
            var notification = JsonRpcNotification.Create(
                "connection.lost",
                new { reason = "WebSocket connection to data source was lost" }
            );

            var message = JsonSerializer.Serialize(notification, JsonOptions);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", message);

            logger.Warning("Notified all clients of connection loss");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to notify clients of connection loss");
        }
    }

    private async Task NotifyConnectionRestoredAsync()
    {
        try
        {
            var notification = JsonRpcNotification.Create(
                "connection.restored",
                new { action = "resubscribe" }
            );

            var message = JsonSerializer.Serialize(notification, JsonOptions);
            await hubContext.Clients.All.SendAsync("ReceiveMessage", message);

            logger.Information("Notified all clients to resubscribe after connection restore");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to notify clients of connection restore");
        }
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
                    symbol = trade.Symbol.ToEnumMemberValue(),
                    timestamp = trade.Timestamp,
                    side = trade.Side.ToString().ToLowerInvariant(),
                    qty = trade.Qty,
                    price = trade.Price,
                    tradeId = trade.TradeId,
                }
            );

            var message = JsonSerializer.Serialize(notification, JsonOptions);
            var groupName = BlockchainHub.GetTradesGroupName(trade.Symbol);

            await hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);

            logger.Debug(
                "Broadcasted trade {TradeId} for {Symbol} to group {GroupName}",
                trade.TradeId,
                trade.Symbol.ToEnumMemberValue(),
                groupName
            );
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to broadcast trade {TradeId}", trade.TradeId);
        }
    }
}
