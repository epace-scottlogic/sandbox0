using Microsoft.Extensions.Options;

namespace DataServer.Common.Backoff;

public class ExponentialBackoffStrategy(IOptions<BackoffOptions> options) : IBackoffStrategy
{
    private readonly BackoffOptions _options = options.Value; 
    public TimeSpan GetDelay(int attemptNumber)
    {
        var delay = TimeSpan.FromTicks(
            (long)(_options.InitialDelay.Ticks * Math.Pow(_options.Multiplier, attemptNumber))
        );

        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}
