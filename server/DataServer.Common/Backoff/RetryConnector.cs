using Serilog;

namespace DataServer.Common.Backoff;

public class RetryConnector(IBackoffStrategy backoffStrategy, ILogger logger)
{
    public async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken token)
    {
        var attemptNumber = 0;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await action();
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                attemptNumber++;
                var delay = backoffStrategy.GetDelay(attemptNumber);
                logger.Information(
                    "Attempt {attempt} failed with {error}. Retrying after {delay}...",
                    attemptNumber,
                    ex.Message,
                    delay
                );
                await Task.Delay(delay, token);
            }
        }

        token.ThrowIfCancellationRequested();
    }
}