using System.Text.Json;
using DataServer.Api.Models.JsonRpc;
using DataServer.Api.Utilities;
using DataServer.Application.Services;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Hubs;

public class BlockchainHub(
    IBlockchainDataService blockchainDataService,
    ILogger<BlockchainHub> logger)
    : Hub
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Client disconnected: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId,
            exception?.Message
        );
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        JsonRpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(message, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Failed to parse JSON-RPC request: {Error}", ex.Message);
            await SendErrorResponse(JsonRpcError.ParseError(), null);
            return;
        }

        if (request == null || !request.IsValid())
        {
            await SendErrorResponse(JsonRpcError.InvalidRequest(), request?.Id);
            return;
        }

        await HandleRequest(request);
    }

    private async Task HandleRequest(JsonRpcRequest request)
    {
        switch (request.Method.ToLowerInvariant())
        {
            case "subscribe":
                await HandleSubscribe(request);
                break;
            case "unsubscribe":
                await HandleUnsubscribe(request);
                break;
            default:
                await SendErrorResponse(JsonRpcError.MethodNotFound(), request.Id);
                break;
        }
    }

    private async Task HandleSubscribe(JsonRpcRequest request)
    {
        if (request.Params?.Channel?.ToLowerInvariant() != "trades")
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams("Only 'trades' channel is supported"),
                request.Id
            );
            return;
        }

        if (string.IsNullOrEmpty(request.Params?.Symbol))
        {
            await SendErrorResponse(JsonRpcError.InvalidParams("Symbol is required"), request.Id);
            return;
        }

        if (!SymbolMapper.TryParse(request.Params.Symbol, out var symbol))
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams($"Invalid symbol: {request.Params.Symbol}"),
                request.Id
            );
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTradesGroupName(symbol));
            await blockchainDataService.SubscribeToTradesAsync(symbol);

            var result = new
            {
                channel = "trades",
                symbol = request.Params.Symbol,
                @event = "subscribed",
            };

            await SendSuccessResponse(result, request.Id);
            logger.LogInformation(
                "Client {ConnectionId} subscribed to trades for {Symbol}",
                Context.ConnectionId,
                symbol
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to subscribe to trades for {Symbol}", symbol);
            await SendErrorResponse(JsonRpcError.InternalError(ex.Message), request.Id);
        }
    }

    private async Task HandleUnsubscribe(JsonRpcRequest request)
    {
        if (request.Params?.Channel?.ToLowerInvariant() != "trades")
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams("Only 'trades' channel is supported"),
                request.Id
            );
            return;
        }

        if (string.IsNullOrEmpty(request.Params?.Symbol))
        {
            await SendErrorResponse(JsonRpcError.InvalidParams("Symbol is required"), request.Id);
            return;
        }

        if (!SymbolMapper.TryParse(request.Params.Symbol, out var symbol))
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams($"Invalid symbol: {request.Params.Symbol}"),
                request.Id
            );
            return;
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTradesGroupName(symbol));
            await blockchainDataService.UnsubscribeFromTradesAsync(symbol);

            var result = new
            {
                channel = "trades",
                symbol = request.Params.Symbol,
                @event = "unsubscribed",
            };

            await SendSuccessResponse(result, request.Id);
            logger.LogInformation(
                "Client {ConnectionId} unsubscribed from trades for {Symbol}",
                Context.ConnectionId,
                symbol
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unsubscribe from trades for {Symbol}", symbol);
            await SendErrorResponse(JsonRpcError.InternalError(ex.Message), request.Id);
        }
    }

    private async Task SendSuccessResponse(object result, string? id)
    {
        var response = JsonRpcResponse.Success(result, id);
        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            JsonSerializer.Serialize(response, JsonOptions)
        );
    }

    private async Task SendErrorResponse(JsonRpcError error, string? id)
    {
        var response = JsonRpcResponse.Failure(error, id);
        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            JsonSerializer.Serialize(response, JsonOptions)
        );
    }

    public static string GetTradesGroupName(Symbol symbol) => $"trades:{symbol}";
}
