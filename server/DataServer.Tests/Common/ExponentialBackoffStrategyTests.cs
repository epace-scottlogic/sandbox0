using DataServer.Common.Backoff;

namespace DataServer.Tests.Common;

public class ExponentialBackoffStrategyTests
{
    [Fact]
    public void GetDelay_AttemptZero_ReturnsInitialDelay()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(0);

        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }

    [Fact]
    public void GetDelay_AttemptOne_ReturnsInitialDelayTimesMultiplier()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(1);

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void GetDelay_AttemptTwo_ReturnsInitialDelayTimesMultiplierSquared()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(2);

        Assert.Equal(TimeSpan.FromSeconds(4), delay);
    }

    [Fact]
    public void GetDelay_ExceedsMaxDelay_ReturnsCappedMaxDelay()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(10),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(10);

        Assert.Equal(TimeSpan.FromSeconds(10), delay);
    }

    [Theory]
    [InlineData(2.0, 0, 1)]
    [InlineData(2.0, 1, 2)]
    [InlineData(2.0, 2, 4)]
    [InlineData(2.0, 3, 8)]
    [InlineData(2.0, 4, 16)]
    [InlineData(3.0, 0, 1)]
    [InlineData(3.0, 1, 3)]
    [InlineData(3.0, 2, 9)]
    [InlineData(3.0, 3, 27)]
    [InlineData(3.0, 4, 64)]
    public void GetDelay_CalculatesExponentialDelayCorrectly(double multiplier, int attempt, int expectedSeconds)
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = multiplier,
            MaxDelay = TimeSpan.FromSeconds(64),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(attempt);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void GetDelay_WithDifferentMultiplier_CalculatesCorrectly()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Multiplier = 3.0,
            MaxDelay = TimeSpan.FromSeconds(100),
        };
        var strategy = new ExponentialBackoffStrategy(options);

        var delay = strategy.GetDelay(2);

        Assert.Equal(TimeSpan.FromSeconds(9), delay);
    }
}
