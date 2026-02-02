namespace DataServer.Domain.Blockchain;

public record BaseResponse(int Seqnum, Event Event, Channel Channel);

public record TradeResponse(int Seqnum, Event Event, Channel Channel, Symbol Symbol)
    : BaseResponse(Seqnum, Event, Channel);
