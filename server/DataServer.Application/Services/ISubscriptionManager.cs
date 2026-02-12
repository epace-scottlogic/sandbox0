using DataServer.Domain.Blockchain;

namespace DataServer.Application.Services;

public interface ISubscriptionManager
{
    bool ShouldSubscribeDownstream(Symbol symbol);
    bool ShouldUnsubscribeDownstream(Symbol symbol);
}
