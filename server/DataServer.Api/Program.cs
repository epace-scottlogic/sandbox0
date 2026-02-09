using DataServer.Api.Hubs;
using DataServer.Api.Middleware;
using DataServer.Api.Services;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Common.Backoff;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;

// Logger established here to log program loading
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Application is starting");
    var builder = WebApplication.CreateBuilder(args);

    builder
        .Configuration.AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    builder.Services.AddSignalR(options =>
    {
        options.AddFilter<HubExceptionFilter>();
    });
    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    );

    builder.Services.AddMemoryCache();

    builder.Services.Configure<BlockchainSettings>(
        builder.Configuration.GetSection(BlockchainSettings.SectionName)
    );
    builder.Services.Configure<BackoffOptions>(
        builder.Configuration.GetSection(BackoffOptions.SectionName));

    builder.Services.AddHostedService<BlockchainHubService>();
    
    builder.Services.AddScoped<IBackoffStrategy, ExponentialBackoffStrategy>();
    builder.Services.AddScoped<RetryConnector>();
    builder.Services.AddScoped<IWebSocketClient, WebSocketClientWrapper>();
    builder.Services.AddSingleton<IBlockchainDataClient, BlockchainDataClient>();
    builder.Services.AddSingleton<IBlockchainDataRepository, InMemoryBlockchainDataRepository>();
    builder.Services.AddScoped<IBlockchainDataService, BlockchainDataService>();
    
    var app = builder.Build();
    
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();
    app.MapHub<BlockchainHub>("/blockchain");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
