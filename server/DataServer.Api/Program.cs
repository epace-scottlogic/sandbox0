using DataServer.Api.Hubs;
using DataServer.Api.Services;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Connectors.Blockchain;
using DataServer.Infrastructure.Blockchain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IBlockchainDataSource, StubBlockchainDataSource>();
builder.Services.AddSingleton<IBlockchainDataRepository, InMemoryBlockchainDataRepository>();
builder.Services.AddSingleton<IBlockchainDataService, BlockchainDataService>();

builder.Services.AddHostedService<BlockchainHubService>();

var app = builder.Build();

app.MapHub<BlockchainHub>("/blockchain");

app.Run();
