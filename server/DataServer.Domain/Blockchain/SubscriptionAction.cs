using System.Runtime.Serialization;

namespace DataServer.Domain.Blockchain;

public enum SubscriptionAction
{
    [EnumMember(Value = "subscribe")]
    Subscribe,

    [EnumMember(Value = "unsubscribe")]
    Unsubscribe,
}
