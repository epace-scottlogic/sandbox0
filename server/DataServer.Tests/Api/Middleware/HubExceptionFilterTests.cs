using DataServer.Api.Middleware;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Serilog;

namespace DataServer.Tests.Api.Middleware;

public class HubExceptionFilterTests
{
    private readonly Mock<ILogger> _mockLogger;

    public HubExceptionFilterTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenNoException_ReturnsResult()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockContext = CreateMockHubInvocationContext("connection-123");
        var expectedResult = "success";

        var result = await filter.InvokeMethodAsync(
            mockContext,
            _ => new ValueTask<object?>(expectedResult)
        );

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenExceptionThrown_LogsError()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockContext = CreateMockHubInvocationContext("connection-123");
        var exception = new Exception("Test exception");

        await Assert.ThrowsAsync<Exception>(() =>
            filter.InvokeMethodAsync(mockContext, _ => throw exception).AsTask()
        );

        _mockLogger.Verify(
            x =>
                x.Error(
                    exception,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenExceptionThrown_LogsConnectionId()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockContext = CreateMockHubInvocationContext("test-connection-id");
        var exception = new Exception("Test exception");

        await Assert.ThrowsAsync<Exception>(() =>
            filter.InvokeMethodAsync(mockContext, _ => throw exception).AsTask()
        );

        _mockLogger.Verify(
            x =>
                x.Error(
                    exception,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenExceptionThrown_RethrowsException()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockContext = CreateMockHubInvocationContext("connection-123");
        var exception = new InvalidOperationException("Test exception");

        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.InvokeMethodAsync(mockContext, _ => throw exception).AsTask()
        );

        Assert.Same(exception, thrownException);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenNoException_CompletesSuccessfully()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockLifetimeContext = CreateMockHubLifetimeContext("connection-123");

        await filter.OnConnectedAsync(mockLifetimeContext, _ => Task.CompletedTask);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenExceptionThrown_LogsError()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockLifetimeContext = CreateMockHubLifetimeContext("connection-123");
        var exception = new Exception("Connection failed");

        await Assert.ThrowsAsync<Exception>(() =>
            filter.OnConnectedAsync(mockLifetimeContext, _ => throw exception)
        );

        _mockLogger.Verify(
            x => x.Error(exception, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenNoException_CompletesSuccessfully()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockLifetimeContext = CreateMockHubLifetimeContext("connection-123");

        await filter.OnDisconnectedAsync(mockLifetimeContext, null, (_, _) => Task.CompletedTask);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenExceptionThrown_LogsError()
    {
        var filter = new HubExceptionFilter(_mockLogger.Object);
        var mockLifetimeContext = CreateMockHubLifetimeContext("connection-123");
        var exception = new Exception("Disconnection failed");

        await Assert.ThrowsAsync<Exception>(() =>
            filter.OnDisconnectedAsync(mockLifetimeContext, null, (_, _) => throw exception)
        );

        _mockLogger.Verify(
            x =>
                x.Error(
                    exception,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    private static HubInvocationContext CreateMockHubInvocationContext(string connectionId)
    {
        var mockHubCallerContext = new Mock<HubCallerContext>();
        mockHubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId);

        var mockServiceProvider = new Mock<IServiceProvider>();

        return new HubInvocationContext(
            mockHubCallerContext.Object,
            mockServiceProvider.Object,
            new TestHub(),
            typeof(Hub).GetMethod(nameof(Hub.OnConnectedAsync))!,
            new List<object?>()
        );
    }

    private static HubLifetimeContext CreateMockHubLifetimeContext(string connectionId)
    {
        var mockHubCallerContext = new Mock<HubCallerContext>();
        mockHubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId);

        var mockServiceProvider = new Mock<IServiceProvider>();

        return new HubLifetimeContext(
            mockHubCallerContext.Object,
            mockServiceProvider.Object,
            new TestHub()
        );
    }

    private class TestHub : Hub { }
}
