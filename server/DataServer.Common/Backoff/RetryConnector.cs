namespace DataServer.Common.Backoff;

public class RetryConnector(IBackoffStrategy backoffStrategy)
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
            catch
            {
                attemptNumber++;
                var delay = backoffStrategy.GetDelay(attemptNumber);
                await Task.Delay(delay, token);
            }
        }

        token.ThrowIfCancellationRequested();
    }
}
