namespace DataServer.Application.Configuration;

public class BlockchainSettings : IBlockchainSettings
{
    public const string SectionName = "Blockchain";

    public string ApiUrl { get; set; } = string.Empty;
    public string? ApiToken { get; set; }
}
