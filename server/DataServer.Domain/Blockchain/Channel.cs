using System.Runtime.Serialization;

namespace DataServer.Domain.Blockchain;

public enum Channel
{
    [EnumMember(Value = "heartbeat")]
    Heartbeat,

    [EnumMember(Value = "l2")]
    L2,

    [EnumMember(Value = "l3")]
    L3,

    [EnumMember(Value = "prices")]
    Prices,

    [EnumMember(Value = "symbols")]
    Symbols,

    [EnumMember(Value = "ticker")]
    Ticker,

    [EnumMember(Value = "trades")]
    Trades,

    [EnumMember(Value = "auth")]
    Auth,

    [EnumMember(Value = "balances")]
    Balances,

    [EnumMember(Value = "trading")]
    Trading,
}
