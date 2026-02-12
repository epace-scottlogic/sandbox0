using DataServer.Application.Services;
using DataServer.Domain.Blockchain;

namespace DataServer.Tests.Application;

public class SubscriptionManagerTests
{
    private readonly SubscriptionManager _manager = new();

    [Fact]
    public void ShouldSubscribeDownstream_FirstSubscriber_ReturnsTrue()
    {
        var result = _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        Assert.True(result);
    }

    [Fact]
    public void ShouldSubscribeDownstream_SecondSubscriber_ReturnsFalse()
    {
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        var result = _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSubscribeDownstream_DifferentSymbols_BothReturnTrue()
    {
        var btcResult = _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);
        var ethResult = _manager.ShouldSubscribeDownstream(Symbol.EthUsd);

        Assert.True(btcResult);
        Assert.True(ethResult);
    }

    [Fact]
    public void ShouldUnsubscribeDownstream_LastSubscriber_ReturnsTrue()
    {
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        var result = _manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd);

        Assert.True(result);
    }

    [Fact]
    public void ShouldUnsubscribeDownstream_RemainingSubscribers_ReturnsFalse()
    {
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        var result = _manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd);

        Assert.False(result);
    }

    [Fact]
    public void ShouldUnsubscribeDownstream_NoSubscribers_ReturnsFalse()
    {
        var result = _manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSubscribeDownstream_AfterAllUnsubscribed_ReturnsTrue()
    {
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);
        _manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd);

        var result = _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        Assert.True(result);
    }

    [Fact]
    public void ShouldUnsubscribeDownstream_MultipleSubscribersUnsubscribeSequentially_LastReturnsTrue()
    {
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);
        _manager.ShouldSubscribeDownstream(Symbol.BtcUsd);

        Assert.False(_manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd));
        Assert.False(_manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd));
        Assert.True(_manager.ShouldUnsubscribeDownstream(Symbol.BtcUsd));
    }
}
