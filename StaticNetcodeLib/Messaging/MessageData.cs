namespace StaticNetcodeLib.Messaging;

using OdinSerializer;

public readonly struct MessageData(
    MessageType messageType,
    Identifier identifier,
    object? data
)
{
    [OdinSerialize]
    public MessageType MessageType { get; init; } = messageType;
    [OdinSerialize]
    public Identifier Identifier { get; init; } = identifier;
    [OdinSerialize]
    public object? Data { get; init; } = data;
}
