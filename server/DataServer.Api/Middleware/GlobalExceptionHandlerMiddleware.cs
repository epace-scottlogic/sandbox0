using System.Text.Json;
using DataServer.Api.Models.JsonRpc;

namespace DataServer.Api.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, Serilog.ILogger logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var errorResponse = CreateErrorResponse(exception, statusCode);

        logger.Error(
            exception,
            "An unhandled exception occurred while processing {Method} {Path}. Status: {StatusCode}, TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            context.TraceIdentifier
        );

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            JsonRpcException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            OperationCanceledException => 499,
            _ => StatusCodes.Status500InternalServerError,
        };
    }

    private static object CreateErrorResponse(Exception exception, int statusCode)
    {
        return new
        {
            error = new
            {
                message = GetErrorMessage(exception, statusCode),
                type = exception.GetType().Name,
                statusCode,
            },
        };
    }

    private static string GetErrorMessage(Exception exception, int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status500InternalServerError => "An internal server error occurred.",
            _ => exception.Message,
        };
    }
}
