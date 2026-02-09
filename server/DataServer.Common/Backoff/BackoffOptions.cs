namespace DataServer.Common.Backoff;

public class BackoffOptions
{
    public const string SectionName = "Backoff";

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    public double Multiplier { get; set; } = 2.0;

    public TimeSpan Increment { get; set; } = TimeSpan.FromSeconds(1);
}
