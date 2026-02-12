using DataServer.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Moq;
using Serilog;

namespace DataServer.Tests.Api.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_Returns500StatusCode()
    {
        RequestDelegate next = _ => throw new Exception("Test exception");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_LogsError()
    {
        var exception = new Exception("Test exception");
        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        _mockLogger.Verify(
            x => x.Error(exception, It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ReturnsJsonErrorResponse()
    {
        RequestDelegate next = _ => throw new Exception("Test exception");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();

        Assert.Contains("error", responseBody);
        Assert.Equal("application/json", _httpContext.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400StatusCode()
    {
        RequestDelegate next = _ => throw new ArgumentException("Invalid argument");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns401StatusCode()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Unauthorized");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenOperationCanceledException_Returns499StatusCode()
    {
        RequestDelegate next = _ => throw new OperationCanceledException("Request cancelled");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        Assert.Equal(499, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_IncludesRequestPathInLog()
    {
        _httpContext.Request.Path = "/test/path";
        var exception = new Exception("Test exception");
        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        _mockLogger.Verify(
            x => x.Error(exception, It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Once
        );
    }

    [Fact]
    public async Task InvokeAsync_IncludesRequestMethodInLog()
    {
        _httpContext.Request.Method = "POST";
        var exception = new Exception("Test exception");
        RequestDelegate next = _ => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        await middleware.InvokeAsync(_httpContext);

        _mockLogger.Verify(
            x => x.Error(exception, It.IsAny<string>(), It.IsAny<object[]>()),
            Times.Once
        );
    }
}
