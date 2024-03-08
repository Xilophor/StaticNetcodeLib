namespace StaticNetcodeLib;

using OdinSerializer;

public readonly struct Identifier(
    string modGuid,
    string type,
    string name
)
{
    [OdinSerialize]
    public string ModGuid { get; init; } = modGuid;
    [OdinSerialize]
    public string Type { get; init; } = type;
    [OdinSerialize]
    public string Name { get; init; } = name;
}
