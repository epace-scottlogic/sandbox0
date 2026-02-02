using System.Runtime.Serialization;

namespace DataServer.Domain.Blockchain;

public enum Side
{
    [EnumMember(Value = "buy")]
    Buy,

    [EnumMember(Value = "sell")]
    Sell,
}
