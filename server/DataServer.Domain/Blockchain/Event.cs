using System.Runtime.Serialization;

namespace DataServer.Domain.Blockchain;

public enum Event
{
    [EnumMember(Value = "subscribed")]
    Subscribed,

    [EnumMember(Value = "unsubscribed")]
    Unsubscribed,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "snapshot")]
    Snapshot,

    [EnumMember(Value = "updated")]
    Updated,
}
