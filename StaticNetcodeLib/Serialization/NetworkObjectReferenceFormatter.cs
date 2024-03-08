using OdinSerializer;
using StaticNetcodeLib.Serialization;

[assembly: RegisterFormatter(typeof(NetworkObjectReferenceFormatter))]

namespace StaticNetcodeLib.Serialization;

using OdinSerializer;
using Unity.Netcode;

/// <summary>
///     Custom formatter for the <see cref="NetworkObject"/> type.
/// </summary>
internal class NetworkObjectReferenceFormatter : MinimalBaseFormatter<NetworkObjectReference>
{
    private static readonly Serializer<ulong> UInt64Serializer = Serializer.Get<ulong>();

    protected override void Read(ref NetworkObjectReference value, IDataReader reader) =>
        value.NetworkObjectId = UInt64Serializer.ReadValue(reader);

    protected override void Write(ref NetworkObjectReference value, IDataWriter writer) =>
        UInt64Serializer.WriteValue(value.NetworkObjectId, writer);
}
