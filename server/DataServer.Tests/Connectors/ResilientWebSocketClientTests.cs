using System.Net.WebSockets;
using DataServer.Application.Interfaces;
using DataServer.Common.Backoff;
using DataServer.Connectors.Blockchain;
using Moq;
using Serilog;

namespace DataServer.Tests.Connectors;

public class ResilientWebSocketClientTests
{
    private readonly Mock<IBackoffStrategy> _mockStrategy;
    private readonly RetryConnector _retryConnector;
    private readonly ILogger _logger = new Mock<ILogger>().Object;
    private readonly Uri _uri = new("ws://localhost:1234");

    public ResilientWebSocketClientTests()
    {
        _mockStrategy = new Mock<IBackoffStrategy>();
        _mockStrategy.Setup(s => s.GetDelay(It.IsAny<int>())).Returns(TimeSpan.FromMilliseconds(1));
        _retryConnector = new RetryConnector(_mockStrategy.Object, _logger);
    }

    [Fact]
    public async Task ConnectAsync_SucceedsOnFirstTry_DelegatesToRetryConnector()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);

        mockSocket.Verify(s => s.ConnectAsync(_uri, CancellationToken.None), Times.Once);
        _mockStrategy.Verify(s => s.GetDelay(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConnectAsync_FailsThenSucceeds_RetriesAndConnects()
    {
        var callCount = 0;
        var mockFirst = new Mock<IWebSocketClient>();
        mockFirst
            .Setup(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("Connection failed"));

        var mockSecond = new Mock<IWebSocketClient>();
        mockSecond
            .Setup(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new ResilientWebSocketClient(
            _retryConnector,
            () =>
            {
                callCount++;
                return callCount == 1 ? mockFirst.Object : mockSecond.Object;
            },
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);

        Assert.Equal(2, callCount);
        mockFirst.Verify(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()), Times.Once);
        mockSecond.Verify(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_CreatesNewSocketPerAttempt_DisposesFailedSocket()
    {
        var callCount = 0;
        var mockFirst = new Mock<IWebSocketClient>();
        mockFirst
            .Setup(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("Connection failed"));

        var mockSecond = new Mock<IWebSocketClient>();
        mockSecond
            .Setup(s => s.ConnectAsync(_uri, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new ResilientWebSocketClient(
            _retryConnector,
            () =>
            {
                callCount++;
                return callCount == 1 ? mockFirst.Object : mockSecond.Object;
            },
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);

        mockFirst.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SendAsync_BeforeConnect_ThrowsInvalidOperationException()
    {
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => Mock.Of<IWebSocketClient>(),
            _logger
        );
        var buffer = new ArraySegment<byte>(new byte[1]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ReceiveAsync_BeforeConnect_ThrowsInvalidOperationException()
    {
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => Mock.Of<IWebSocketClient>(),
            _logger
        );
        var buffer = new ArraySegment<byte>(new byte[1]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ReceiveAsync(buffer, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CloseAsync_BeforeConnect_ThrowsInvalidOperationException()
    {
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => Mock.Of<IWebSocketClient>(),
            _logger
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None)
        );
    }

    [Fact]
    public async Task SendAsync_AfterConnect_DelegatesToInnerClient()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );
        var buffer = new ArraySegment<byte>(new byte[] { 1, 2, 3 });

        await client.ConnectAsync(_uri, CancellationToken.None);
        await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

        mockSocket.Verify(
            s => s.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None),
            Times.Once
        );
    }

    [Fact]
    public async Task ReceiveAsync_AfterConnect_DelegatesToInnerClient()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        var buffer = new ArraySegment<byte>(new byte[1024]);
        var expectedResult = new WebSocketReceiveResult(5, WebSocketMessageType.Text, true);
        mockSocket
            .Setup(s => s.ReceiveAsync(buffer, CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);
        var result = await client.ReceiveAsync(buffer, CancellationToken.None);

        Assert.Equal(expectedResult, result);
        mockSocket.Verify(s => s.ReceiveAsync(buffer, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CloseAsync_AfterConnect_DelegatesToInnerClient()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        mockSocket.Verify(
            s => s.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None),
            Times.Once
        );
    }

    [Fact]
    public async Task Dispose_DisposesInnerClient()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);
        client.Dispose();

        mockSocket.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_BeforeConnect_DoesNotThrow()
    {
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => Mock.Of<IWebSocketClient>(),
            _logger
        );
        client.Dispose();
    }

    [Fact]
    public void State_BeforeConnect_ReturnsNone()
    {
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => Mock.Of<IWebSocketClient>(),
            _logger
        );
        Assert.Equal(WebSocketState.None, client.State);
    }

    [Fact]
    public async Task State_AfterConnect_DelegatesToInnerClient()
    {
        var mockSocket = new Mock<IWebSocketClient>();
        mockSocket.Setup(s => s.State).Returns(WebSocketState.Open);
        var client = new ResilientWebSocketClient(
            _retryConnector,
            () => mockSocket.Object,
            _logger
        );

        await client.ConnectAsync(_uri, CancellationToken.None);

        Assert.Equal(WebSocketState.Open, client.State);
    }
}
