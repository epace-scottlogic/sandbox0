namespace DataServer.Domain.Blockchain;

public record BaseRequest(SubscriptionAction Action, Channel Channel);

public record TradeRequest(SubscriptionAction Action, Channel Channel, Symbol Symbol)
    : BaseRequest(Action, Channel);
