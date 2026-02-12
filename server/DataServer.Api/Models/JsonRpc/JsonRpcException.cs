namespace DataServer.Api.Models.JsonRpc;

public class JsonRpcException : Exception
{
    public JsonRpcError Error { get; }

    public JsonRpcException(JsonRpcError error)
        : base(error.Message)
    {
        Error = error;
    }

    public JsonRpcException(JsonRpcError error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }
}
