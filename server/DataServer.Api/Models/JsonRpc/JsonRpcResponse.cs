using System.Text.Json.Serialization;

namespace DataServer.Api.Models.JsonRpc;

public record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("result")] object? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error,
    [property: JsonPropertyName("id")] string? Id
)
{
    public const string Version = "2.0";

    public static JsonRpcResponse Success(object? result, string? id) =>
        new(Version, result, null, id);

    public static JsonRpcResponse Failure(JsonRpcError error, string? id) =>
        new(Version, null, error, id);
}

public record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data = null
)
{
    public static JsonRpcError ParseError() => new(-32700, "Parse error");

    public static JsonRpcError InvalidRequest() => new(-32600, "Invalid Request");

    public static JsonRpcError MethodNotFound() => new(-32601, "Method not found");

    public static JsonRpcError InvalidParams(string? details = null) =>
        new(-32602, "Invalid params", details);

    public static JsonRpcError InternalError(string? details = null) =>
        new(-32603, "Internal error", details);
}
