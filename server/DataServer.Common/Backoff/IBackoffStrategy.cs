namespace DataServer.Common.Backoff;

public interface IBackoffStrategy
{
    TimeSpan GetDelay(int attemptNumber);
}
