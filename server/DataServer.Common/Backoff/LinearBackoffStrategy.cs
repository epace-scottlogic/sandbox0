namespace DataServer.Common.Backoff;

public class LinearBackoffStrategy : IBackoffStrategy
{
    private readonly BackoffOptions _options;

    public LinearBackoffStrategy(BackoffOptions options)
    {
        _options = options;
    }

    public TimeSpan GetDelay(int attemptNumber)
    {
        var delay =
            _options.InitialDelay + TimeSpan.FromTicks(_options.Increment.Ticks * attemptNumber);

        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}
