using System.Runtime.Serialization;

namespace DataServer.Domain.Blockchain;

public enum Symbol
{
    [EnumMember(Value = "ETH-USD")]
    EthUsd,

    [EnumMember(Value = "BTC-USD")]
    BtcUsd,
}
