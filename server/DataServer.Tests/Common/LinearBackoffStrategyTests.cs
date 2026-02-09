using DataServer.Common.Backoff;

namespace DataServer.Tests.Common;

public class LinearBackoffStrategyTests
{
    [Fact]
    public void GetDelay_AttemptZero_ReturnsInitialDelay()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Increment = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(0);

        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }

    [Fact]
    public void GetDelay_AttemptOne_ReturnsInitialDelayPlusIncrement()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Increment = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(1);

        Assert.Equal(TimeSpan.FromSeconds(3), delay);
    }

    [Fact]
    public void GetDelay_AttemptTwo_ReturnsInitialDelayPlusTwoIncrements()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Increment = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(2);

        Assert.Equal(TimeSpan.FromSeconds(5), delay);
    }

    [Fact]
    public void GetDelay_ExceedsMaxDelay_ReturnsCappedMaxDelay()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Increment = TimeSpan.FromSeconds(5),
            MaxDelay = TimeSpan.FromSeconds(10),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(10);

        Assert.Equal(TimeSpan.FromSeconds(10), delay);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    public void GetDelay_CalculatesLinearDelayCorrectly(int attempt, int expectedSeconds)
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            Increment = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(60),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(attempt);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void GetDelay_WithDifferentIncrement_CalculatesCorrectly()
    {
        var options = new BackoffOptions
        {
            InitialDelay = TimeSpan.FromSeconds(2),
            Increment = TimeSpan.FromSeconds(3),
            MaxDelay = TimeSpan.FromSeconds(100),
        };
        var strategy = new LinearBackoffStrategy(options);

        var delay = strategy.GetDelay(3);

        Assert.Equal(TimeSpan.FromSeconds(11), delay);
    }
}
