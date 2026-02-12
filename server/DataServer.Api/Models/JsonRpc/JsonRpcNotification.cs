using System.Text.Json.Serialization;

namespace DataServer.Api.Models.JsonRpc;

public record JsonRpcNotification(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] object? Params
)
{
    public const string Version = "2.0";

    public static JsonRpcNotification Create(string method, object? parameters) =>
        new(Version, method, parameters);
}
