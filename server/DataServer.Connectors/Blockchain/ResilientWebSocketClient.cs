using System.Net.WebSockets;
using DataServer.Application.Interfaces;
using DataServer.Common.Backoff;
using Serilog;

namespace DataServer.Connectors.Blockchain;

public class ResilientWebSocketClient(
    RetryConnector retryConnector,
    Func<IWebSocketClient> socketFactory,
    ILogger logger)
    : IWebSocketClient
{
    private IWebSocketClient? _inner;

    public ResilientWebSocketClient(RetryConnector retryConnector, ILogger logger)
        : this(retryConnector, () => new WebSocketClientWrapper(), logger) { }

    public WebSocketState State => _inner?.State ?? WebSocketState.None;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        logger.Information("Attempting to connect to WebSocket at {Uri} with retry", uri);

        await retryConnector.ExecuteWithRetryAsync(
            () =>
            {
                _inner?.Dispose();
                _inner = socketFactory();
                return _inner.ConnectAsync(uri, cancellationToken);
            },
            cancellationToken
        );

        logger.Information("Successfully connected to WebSocket at {Uri}", uri);
    }

    public Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        logger.Information("{client}: Disconnecting from websocket", nameof(ResilientWebSocketClient));
        return GetConnectedClient().CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken
    )
    {
        // logger.Information("Sent message: {Message}", @buffer);
        return GetConnectedClient().SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }

    public Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
    {
        // logger.Information("Received message: {Message}", @buffer);
        return GetConnectedClient().ReceiveAsync(buffer, cancellationToken);
    }

    public void Dispose()
    {
        _inner?.Dispose();
    }

    private IWebSocketClient GetConnectedClient()
    {
        return _inner ?? throw new InvalidOperationException("WebSocket is not connected.");
    }
}
