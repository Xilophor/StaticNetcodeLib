namespace StaticNetcodeLib.Messaging;

using System.Reflection;
using Enums;
using OdinSerializer;

public record MessageData(
    [property: OdinSerialize] MessageType MessageType,
    [property: OdinSerialize] MethodBase MethodBase,
    [property: OdinSerialize] object[]? Data
)
{
    public (MessageType, MethodBase, object[]?) AsValueTuple() => (this.MessageType, this.MethodBase, this.Data);
}
