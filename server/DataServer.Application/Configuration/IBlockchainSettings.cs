namespace DataServer.Application.Configuration;

public interface IBlockchainSettings
{
    string ApiUrl { get; }
    string? ApiToken { get; }
}
