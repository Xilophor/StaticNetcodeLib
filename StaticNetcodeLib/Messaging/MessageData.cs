namespace StaticNetcodeLib.Messaging;

using Enums;
using OdinSerializer;

public readonly struct MessageData(
    MessageType messageType,
    IIdentifier identifier,
    object? data
)
{
    [OdinSerialize]
    public MessageType MessageType { get; init; } = messageType;

    [OdinSerialize]
    public IIdentifier Identifier { get; init; } = identifier;

    [OdinSerialize]
    public object? Data { get; init; } = data;

    public (MessageType, IIdentifier, object?) AsValueTuple() => (this.MessageType, this.Identifier, this.Data);
}
