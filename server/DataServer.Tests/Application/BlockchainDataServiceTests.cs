using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Domain.Blockchain;
using Moq;

namespace DataServer.Tests.Application;

public class BlockchainDataServiceTests
{
    private readonly Mock<IBlockchainDataSource> _mockDataSource;
    private readonly Mock<IBlockchainDataRepository> _mockRepository;
    private readonly BlockchainDataService _service;

    public BlockchainDataServiceTests()
    {
        _mockDataSource = new Mock<IBlockchainDataSource>();
        _mockRepository = new Mock<IBlockchainDataRepository>();
        _service = new BlockchainDataService(_mockDataSource.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task StartAsync_CallsConnectAsyncOnDataSource()
    {
        _mockDataSource.Verify(ds => ds.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);

        await _service.StartAsync();

        _mockDataSource.Verify(ds => ds.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_CallsDisconnectAsyncOnDataSource()
    {
        await _service.StartAsync();

        _mockDataSource.Verify(
            ds => ds.DisconnectAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.StopAsync();

        _mockDataSource.Verify(ds => ds.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscribeToTradesAsync_CallsSubscribeOnDataSourceWithCorrectSymbol()
    {
        const Symbol symbol = Symbol.BtcUsd;

        _mockDataSource.Verify(
            ds => ds.SubscribeToTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.SubscribeToTradesAsync(symbol);

        _mockDataSource.Verify(
            ds => ds.SubscribeToTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromTradesAsync_CallsUnsubscribeOnDataSourceWithCorrectSymbol()
    {
        const Symbol symbol = Symbol.BtcUsd;

        _mockDataSource.Verify(
            ds => ds.UnsubscribeFromTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.UnsubscribeFromTradesAsync(symbol);

        _mockDataSource.Verify(
            ds => ds.UnsubscribeFromTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenTradeReceived_CallsAddTradeAsyncOnRepository()
    {
        var trade = CreateTestTrade(Symbol.BtcUsd);

        await _service.StartAsync();

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(It.IsAny<TradeUpdate>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, trade);

        await Task.Delay(50);
        _mockRepository.Verify(
            repo => repo.AddTradeAsync(trade, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenTradeReceived_RaisesTradeReceivedEvent()
    {
        var trade = CreateTestTrade(Symbol.EthUsd);
        TradeUpdate? receivedTrade = null;
        _service.TradeReceived += (sender, t) => receivedTrade = t;
        await _service.StartAsync();

        Assert.Null(receivedTrade);

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, trade);

        await Task.Delay(50);
        Assert.NotNull(receivedTrade);
        Assert.Equal(trade, receivedTrade);
    }

    [Fact]
    public async Task GetRecentTradesAsync_CallsGetRecentTradesAsyncOnRepositoryWithCorrectParameters()
    {
        const Symbol symbol = Symbol.BtcUsd;
        const int count = 50;
        var expectedTrades = new List<TradeUpdate> { CreateTestTrade(symbol) };
        _mockRepository
            .Setup(repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTrades);

        _mockRepository.Verify(
            repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()),
            Times.Never
        );

        var result = await _service.GetRecentTradesAsync(symbol, count);

        _mockRepository.Verify(
            repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Equal(expectedTrades, result);
    }

    [Fact]
    public async Task AfterStopAsync_TradeReceivedEventDoesNotCallRepository()
    {
        var tradeBeforeStop = CreateTestTrade(Symbol.BtcUsd, "trade-before-stop");
        var tradeAfterStop = CreateTestTrade(Symbol.BtcUsd, "trade-after-stop");

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(It.IsAny<TradeUpdate>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.StartAsync();

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, tradeBeforeStop);
        await Task.Delay(50);

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(tradeBeforeStop, It.IsAny<CancellationToken>()),
            Times.Once
        );

        await _service.StopAsync();

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, tradeAfterStop);
        await Task.Delay(50);

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(tradeAfterStop, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private static TradeUpdate CreateTestTrade(Symbol symbol, string tradeId = "test-trade-1")
    {
        return new TradeUpdate(
            Seqnum: 1,
            Event: Event.Updated,
            Channel: Channel.Trades,
            Symbol: symbol,
            Timestamp: DateTimeOffset.UtcNow,
            Side: Side.Buy,
            Qty: 1.5m,
            Price: 50000m,
            TradeId: tradeId
        );
    }
}
