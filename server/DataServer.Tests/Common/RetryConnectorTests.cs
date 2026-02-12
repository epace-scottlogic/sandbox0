using DataServer.Common.Backoff;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;

namespace DataServer.Tests.Common;

public class RetryConnectorTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessOnFirstAttempt_DoesNotRetry()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var callCount = 0;

        await connector.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        Assert.Equal(1, callCount);
        mockStrategy.Verify(s => s.GetDelay(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FailsThenSucceeds_RetriesOnce()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        mockStrategy.Setup(s => s.GetDelay(It.IsAny<int>())).Returns(TimeSpan.FromMilliseconds(1));
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var callCount = 0;

        await connector.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("First attempt failed");
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        Assert.Equal(2, callCount);
        mockStrategy.Verify(s => s.GetDelay(1), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_CancellationRequested_StopsRetrying()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        mockStrategy
            .Setup(s => s.GetDelay(It.IsAny<int>()))
            .Returns(TimeSpan.FromMilliseconds(100));
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();
        var callCount = 0;

        var task = connector.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount == 2)
                    cts.Cancel();
                throw new InvalidOperationException("Always fails");
            },
            cts.Token
        );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_CancellationDuringAction_ThrowsOperationCanceledException()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connector.ExecuteWithRetryAsync(
                () => throw new OperationCanceledException(cts.Token),
                cts.Token
            )
        );
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_MultipleFailures_IncrementsAttemptNumber()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        mockStrategy.Setup(s => s.GetDelay(It.IsAny<int>())).Returns(TimeSpan.FromMilliseconds(1));
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var callCount = 0;

        await connector.ExecuteWithRetryAsync(
            () =>
            {
                callCount++;
                if (callCount < 4)
                    throw new InvalidOperationException($"Attempt {callCount} failed");
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        Assert.Equal(4, callCount);
        mockStrategy.Verify(s => s.GetDelay(1), Times.Once);
        mockStrategy.Verify(s => s.GetDelay(2), Times.Once);
        mockStrategy.Verify(s => s.GetDelay(3), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_UsesBackoffStrategyDelay()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(10),
            Multiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(1),
        };
        var mockOptions = new Mock<IOptions<BackoffOptions>>();
        mockOptions.Setup(s => s.Value).Returns(options);
        var strategy = new ExponentialBackoffStrategy(mockOptions.Object);
        var mockLogger = new Mock<ILogger>();
        var connector = new RetryConnector(strategy, mockLogger.Object);
        var callCount = 0;
        var timestamps = new List<DateTimeOffset>();

        await connector.ExecuteWithRetryAsync(
            () =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                callCount++;
                if (callCount < 3)
                    throw new InvalidOperationException("Retry");
                return Task.CompletedTask;
            },
            CancellationToken.None
        );

        Assert.Equal(3, callCount);
        Assert.True(timestamps[1] - timestamps[0] >= TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_PreCancelledToken_ThrowsImmediately()
    {
        var mockStrategy = new Mock<IBackoffStrategy>();
        var mockLogger = new Mock<ILogger>();
        var connector = new RetryConnector(mockStrategy.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var callCount = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connector.ExecuteWithRetryAsync(
                () =>
                {
                    callCount++;
                    return Task.CompletedTask;
                },
                cts.Token
            )
        );

        Assert.Equal(0, callCount);
    }
}
