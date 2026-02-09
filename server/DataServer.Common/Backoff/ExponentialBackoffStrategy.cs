namespace DataServer.Common.Backoff;

public class ExponentialBackoffStrategy(BackoffOptions options) : IBackoffStrategy
{
    public TimeSpan GetDelay(int attemptNumber)
    {
        var delay = TimeSpan.FromTicks(
            (long)(options.InitialDelay.Ticks * Math.Pow(options.Multiplier, attemptNumber))
        );

        return delay > options.MaxDelay ? options.MaxDelay : delay;
    }
}
