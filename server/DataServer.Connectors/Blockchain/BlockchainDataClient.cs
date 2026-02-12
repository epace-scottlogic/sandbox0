using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Options;
using Serilog;

namespace DataServer.Connectors.Blockchain;

public class BlockchainDataClient : IBlockchainDataClient, IDisposable
{
    private readonly BlockchainSettings _options;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private readonly Uri _uri;
    private readonly IWebSocketClient _webSocketClient;
    private readonly ILogger _logger;
    private readonly HashSet<Symbol> _activeSubscriptions = [];

    public BlockchainDataClient(
        IOptions<BlockchainSettings> options,
        IWebSocketClient webSocketClient,
        ILogger logger
    )
    {
        _webSocketClient = webSocketClient;
        _logger = logger;
        _options = options.Value;
        _uri = new Uri(_options.ApiUrl);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public event EventHandler<TradeUpdate>? TradeReceived;
    public event EventHandler<TradeResponse>? SubscriptionConfirmed;
    public event EventHandler? ConnectionLost;
    public event EventHandler? ConnectionRestored;

    public bool IsConnected => _webSocketClient.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _webSocketClient.ConnectAsync(_uri, cancellationToken);
        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = ReceiveMessagesAsync(_receiveCts.Token);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _receiveCts?.Cancel();

        if (_webSocketClient.State == WebSocketState.Open)
        {
            await _webSocketClient.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnecting",
                cancellationToken
            );
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException) { }
        }

        _logger.Information("Disconnected from Blockchain API at {Uri}", _uri);
    }

    public async Task SubscribeToTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Subscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        _activeSubscriptions.Add(symbol);
        _logger.Information("Subscribed to trades: {Symbol}", symbol.ToEnumMemberValue());
    }

    public async Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Unsubscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        _activeSubscriptions.Remove(symbol);
        _logger.Information("Unsubscribed from trades: {Symbol}", symbol.ToEnumMemberValue());
    }

    private object CreateSubscriptionRequest(SubscriptionAction action, Symbol symbol)
    {
        var actionString = action == SubscriptionAction.Subscribe ? "subscribe" : "unsubscribe";
        var symbolString = symbol.ToEnumMemberValue();

        if (!string.IsNullOrEmpty(_options.ApiToken))
        {
            return new
            {
                action = actionString,
                channel = "trades",
                symbol = symbolString,
                token = _options.ApiToken,
            };
        }

        return new
        {
            action = actionString,
            channel = "trades",
            symbol = symbolString,
        };
    }

    private async Task SendMessageAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        await _webSocketClient.SendAsync(
            buffer,
            WebSocketMessageType.Text,
            true,
            cancellationToken
        );

        _logger.Information("Sent message: {Message}", @message);
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var result = await _webSocketClient.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.Information("WebSocket closed by server at {Uri}", _uri);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await HandleConnectionLostAsync(cancellationToken);
                    }
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex)
        {
            _logger.Error(ex, "Websocket error occured on {Uri}", _uri);
            if (!cancellationToken.IsCancellationRequested)
            {
                await HandleConnectionLostAsync(cancellationToken);
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            _logger.Information("Message received: {message}", @message);

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("event", out var eventElement))
            {
                return;
            }

            var eventString = eventElement.GetString();

            if (eventString == "subscribed" || eventString == "unsubscribed")
            {
                ProcessSubscriptionResponse(root, eventString);
            }
            else if (eventString == "updated")
            {
                ProcessTradeUpdate(root);
            }
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to parse message: {@message}", message);
        }
    }

    private void ProcessSubscriptionResponse(JsonElement root, string eventString)
    {
        var seqnum = root.TryGetProperty("seqnum", out var seqnumElement)
            ? seqnumElement.GetInt32()
            : 0;

        var symbolString = root.TryGetProperty("symbol", out var symbolElement)
            ? symbolElement.GetString()
            : null;

        if (symbolString == null)
        {
            return;
        }

        symbolString.TryParseEnumMember<Symbol>(out var symbol);

        var eventType = eventString == "subscribed" ? Event.Subscribed : Event.Unsubscribed;

        var response = new TradeResponse(seqnum, eventType, Channel.Trades, symbol);
        SubscriptionConfirmed?.Invoke(this, response);
    }

    private void ProcessTradeUpdate(JsonElement root)
    {
        var seqnum = root.TryGetProperty("seqnum", out var seqnumElement)
            ? seqnumElement.GetInt32()
            : 0;
        var symbolString = root.TryGetProperty("symbol", out var symbolElement)
            ? symbolElement.GetString()
            : null;

        if (symbolString == null)
        {
            return;
        }

        var timestamp = root.TryGetProperty("timestamp", out var timestampElement)
            ? DateTimeOffset.Parse(timestampElement.GetString()!)
            : DateTimeOffset.UtcNow;
        var sideString = root.TryGetProperty("side", out var sideElement)
            ? sideElement.GetString()
            : "buy";
        var qty = root.TryGetProperty("qty", out var qtyElement) ? qtyElement.GetDecimal() : 0m;
        var price = root.TryGetProperty("price", out var priceElement)
            ? priceElement.GetDecimal()
            : 0m;
        var tradeId = root.TryGetProperty("trade_id", out var tradeIdElement)
            ? tradeIdElement.GetString()
            : Guid.NewGuid().ToString();

        symbolString.TryParseEnumMember<Symbol>(out var symbol);

        var side = sideString?.ToLowerInvariant() == "sell" ? Side.Sell : Side.Buy;

        var trade = new TradeUpdate(
            seqnum,
            Event.Updated,
            Channel.Trades,
            symbol,
            timestamp,
            side,
            qty,
            price,
            tradeId!
        );

        TradeReceived?.Invoke(this, trade);
    }

    private async Task HandleConnectionLostAsync(CancellationToken cancellationToken)
    {
        ConnectionLost?.Invoke(this, EventArgs.Empty);

        await ConnectAsync(cancellationToken);
        await ResubscribeAsync(cancellationToken);

        ConnectionRestored?.Invoke(this, EventArgs.Empty);
    }

    private async Task ResubscribeAsync(CancellationToken cancellationToken)
    {
        foreach (var symbol in _activeSubscriptions)
        {
            var request = CreateSubscriptionRequest(SubscriptionAction.Subscribe, symbol);
            await SendMessageAsync(request, cancellationToken);
            _logger.Information("Resubscribed to trades: {Symbol}", symbol.ToEnumMemberValue());
        }
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _webSocketClient.Dispose();
    }
}
