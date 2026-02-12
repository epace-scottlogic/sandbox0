using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Connectors.Blockchain;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;

namespace DataServer.Tests.Connectors;

public class BlockchainDataClientTests
{
    private readonly Mock<IWebSocketClient> _mockWebSocketClient;
    private readonly Mock<ILogger> _mockLogger;
    private readonly BlockchainSettings _settings;
    private readonly BlockchainDataClient _dataClient;

    public BlockchainDataClientTests()
    {
        _mockWebSocketClient = new Mock<IWebSocketClient>();
        _mockLogger = new Mock<ILogger>();
        _settings = new BlockchainSettings { ApiUrl = "ws://localhost:8765", ApiToken = null };

        _mockWebSocketClient.Setup(ws => ws.State).Returns(WebSocketState.None);

        var mockOptions = new Mock<IOptions<BlockchainSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_settings);

        _dataClient = new BlockchainDataClient(
            mockOptions.Object,
            _mockWebSocketClient.Object,
            _mockLogger.Object
        );
    }

    private void SetupWebSocketForConnect()
    {
        _mockWebSocketClient
            .Setup(ws => ws.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback(() => _mockWebSocketClient.Setup(ws => ws.State).Returns(WebSocketState.Open))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(ws =>
                ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())
            )
            .Returns<ArraySegment<byte>, CancellationToken>(
                async (_, ct) =>
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, ct);
                    }
                    catch (OperationCanceledException) { }
                    return new WebSocketReceiveResult(
                        0,
                        WebSocketMessageType.Close,
                        true,
                        WebSocketCloseStatus.NormalClosure,
                        "Closed"
                    );
                }
            );
    }

    private void SetupWebSocketForDisconnect()
    {
        _mockWebSocketClient
            .Setup(ws =>
                ws.CloseAsync(
                    It.IsAny<WebSocketCloseStatus>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback(() =>
                _mockWebSocketClient.Setup(ws => ws.State).Returns(WebSocketState.Closed)
            )
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ConnectAsync_CallsWebSocketConnectWithCorrectUri()
    {
        SetupWebSocketForConnect();

        await _dataClient.ConnectAsync();

        _mockWebSocketClient.Verify(
            ws => ws.ConnectAsync(new Uri("ws://localhost:8765"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ConnectAsync_SetsIsConnectedToTrue()
    {
        SetupWebSocketForConnect();

        Assert.False(_dataClient.IsConnected);

        await _dataClient.ConnectAsync();

        Assert.True(_dataClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_CallsWebSocketCloseAsync()
    {
        SetupWebSocketForConnect();
        SetupWebSocketForDisconnect();

        await _dataClient.ConnectAsync();
        await _dataClient.DisconnectAsync();

        _mockWebSocketClient.Verify(
            ws =>
                ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task DisconnectAsync_SetsIsConnectedToFalse()
    {
        SetupWebSocketForConnect();
        SetupWebSocketForDisconnect();

        await _dataClient.ConnectAsync();
        Assert.True(_dataClient.IsConnected);

        await _dataClient.DisconnectAsync();
        Assert.False(_dataClient.IsConnected);
    }

    [Fact]
    public async Task SubscribeToTradesAsync_SendsCorrectJsonMessage()
    {
        SetupWebSocketForConnect();

        string? sentMessage = null;
        _mockWebSocketClient
            .Setup(ws =>
                ws.SendAsync(
                    It.IsAny<ArraySegment<byte>>(),
                    WebSocketMessageType.Text,
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>(
                (buffer, _, _, _) =>
                {
                    sentMessage = Encoding.UTF8.GetString(
                        buffer.Array!,
                        buffer.Offset,
                        buffer.Count
                    );
                }
            )
            .Returns(Task.CompletedTask);

        await _dataClient.ConnectAsync();
        await _dataClient.SubscribeToTradesAsync(Symbol.BtcUsd);

        Assert.NotNull(sentMessage);
        var json = JsonDocument.Parse(sentMessage);
        Assert.Equal("subscribe", json.RootElement.GetProperty("action").GetString());
        Assert.Equal("trades", json.RootElement.GetProperty("channel").GetString());
        Assert.Equal("BTC-USD", json.RootElement.GetProperty("symbol").GetString());
    }

    [Fact]
    public async Task UnsubscribeFromTradesAsync_SendsCorrectJsonMessage()
    {
        SetupWebSocketForConnect();

        string? sentMessage = null;
        _mockWebSocketClient
            .Setup(ws =>
                ws.SendAsync(
                    It.IsAny<ArraySegment<byte>>(),
                    WebSocketMessageType.Text,
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>(
                (buffer, _, _, _) =>
                {
                    sentMessage = Encoding.UTF8.GetString(
                        buffer.Array!,
                        buffer.Offset,
                        buffer.Count
                    );
                }
            )
            .Returns(Task.CompletedTask);

        await _dataClient.ConnectAsync();
        await _dataClient.UnsubscribeFromTradesAsync(Symbol.EthUsd);

        Assert.NotNull(sentMessage);
        var json = JsonDocument.Parse(sentMessage);
        Assert.Equal("unsubscribe", json.RootElement.GetProperty("action").GetString());
        Assert.Equal("trades", json.RootElement.GetProperty("channel").GetString());
        Assert.Equal("ETH-USD", json.RootElement.GetProperty("symbol").GetString());
    }

    [Fact]
    public async Task SubscribeToTradesAsync_IncludesApiTokenWhenConfigured()
    {
        var settingsWithToken = new BlockchainSettings
        {
            ApiUrl = "ws://localhost:8765",
            ApiToken = "test-api-token",
        };
        var mockOptions = new Mock<IOptions<BlockchainSettings>>();
        mockOptions.Setup(o => o.Value).Returns(settingsWithToken);

        var mockLogger = new Mock<ILogger>();
        var dataSourceWithToken = new BlockchainDataClient(
            mockOptions.Object,
            _mockWebSocketClient.Object,
            mockLogger.Object
        );

        SetupWebSocketForConnect();

        string? sentMessage = null;
        _mockWebSocketClient
            .Setup(ws =>
                ws.SendAsync(
                    It.IsAny<ArraySegment<byte>>(),
                    WebSocketMessageType.Text,
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>(
                (buffer, _, _, _) =>
                {
                    sentMessage = Encoding.UTF8.GetString(
                        buffer.Array!,
                        buffer.Offset,
                        buffer.Count
                    );
                }
            )
            .Returns(Task.CompletedTask);

        await dataSourceWithToken.ConnectAsync();
        await dataSourceWithToken.SubscribeToTradesAsync(Symbol.BtcUsd);

        Assert.NotNull(sentMessage);
        var json = JsonDocument.Parse(sentMessage);
        Assert.Equal("test-api-token", json.RootElement.GetProperty("token").GetString());
    }

    [Fact]
    public async Task SubscribeToTradesAsync_OmitsTokenWhenNotConfigured()
    {
        SetupWebSocketForConnect();

        string? sentMessage = null;
        _mockWebSocketClient
            .Setup(ws =>
                ws.SendAsync(
                    It.IsAny<ArraySegment<byte>>(),
                    WebSocketMessageType.Text,
                    true,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<ArraySegment<byte>, WebSocketMessageType, bool, CancellationToken>(
                (buffer, _, _, _) =>
                {
                    sentMessage = Encoding.UTF8.GetString(
                        buffer.Array!,
                        buffer.Offset,
                        buffer.Count
                    );
                }
            )
            .Returns(Task.CompletedTask);

        await _dataClient.ConnectAsync();
        await _dataClient.SubscribeToTradesAsync(Symbol.BtcUsd);

        Assert.NotNull(sentMessage);
        var json = JsonDocument.Parse(sentMessage);
        Assert.False(json.RootElement.TryGetProperty("token", out _));
    }

    [Fact]
    public void IsConnected_ReturnsFalseWhenNotConnected()
    {
        Assert.False(_dataClient.IsConnected);
    }

    [Fact]
    public async Task IsConnected_ReturnsTrueWhenWebSocketIsOpen()
    {
        SetupWebSocketForConnect();

        await _dataClient.ConnectAsync();

        Assert.True(_dataClient.IsConnected);
    }
}
