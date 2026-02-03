using System.Text.Json.Serialization;

namespace DataServer.Api.Models.JsonRpc;

public record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] JsonRpcParams? Params,
    [property: JsonPropertyName("id")] string? Id
)
{
    public const string Version = "2.0";

    public bool IsValid() => JsonRpc == Version && !string.IsNullOrEmpty(Method);
}

public record JsonRpcParams(
    [property: JsonPropertyName("channel")] string? Channel,
    [property: JsonPropertyName("symbol")] string? Symbol
);
