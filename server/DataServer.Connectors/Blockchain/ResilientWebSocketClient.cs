using System.Net.WebSockets;
using DataServer.Application.Interfaces;
using DataServer.Common.Backoff;
using Serilog;

namespace DataServer.Connectors.Blockchain;

public class ResilientWebSocketClient : IWebSocketClient
{
    private readonly RetryConnector _retryConnector;
    private readonly Func<IWebSocketClient> _socketFactory;
    private IWebSocketClient? _inner;

    private readonly ILogger _logger;

    public ResilientWebSocketClient(RetryConnector retryConnector, ILogger logger)
        : this(retryConnector, () => new WebSocketClientWrapper(), logger) { }

    public ResilientWebSocketClient(
        RetryConnector retryConnector,
        Func<IWebSocketClient> socketFactory,
        ILogger logger
    )
    {
        _retryConnector = retryConnector;
        _socketFactory = socketFactory;
        _logger = logger;
    }

    public WebSocketState State => _inner?.State ?? WebSocketState.None;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        _logger.Information("Attempting to connect to WebSocket at {Uri} with retry", uri);

        await _retryConnector.ExecuteWithRetryAsync(
            () =>
            {
                _inner?.Dispose();
                _inner = _socketFactory();
                return _inner.ConnectAsync(uri, cancellationToken);
            },
            cancellationToken
        );

        _logger.Information("Successfully connected to WebSocket at {Uri}", uri);
    }

    public Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        return GetConnectedClient().CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken
    )
    {
        return GetConnectedClient().SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }

    public Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
    {
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
