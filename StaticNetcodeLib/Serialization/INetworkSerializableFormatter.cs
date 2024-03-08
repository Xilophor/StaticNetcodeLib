using OdinSerializer;
using StaticNetcodeLib.Serialization;

[assembly: RegisterFormatter(typeof(INetworkSerializableFormatter))]

namespace StaticNetcodeLib.Serialization;

using Unity.Collections;
using Unity.Netcode;

// ReSharper disable once InconsistentNaming
/// <summary>
///     Custom formatter for the <see cref="INetworkSerializable"/> interface.
/// </summary>
public class INetworkSerializableFormatter : MinimalBaseFormatter<INetworkSerializable>
{
    private static readonly Serializer<byte[]> ByteArraySerializer = Serializer.Get<byte[]>();

    protected override void Read(ref INetworkSerializable value, IDataReader reader)
    {
        var byteArray = ByteArraySerializer.ReadValue(reader);

        var ngoReader = new FastBufferReader(byteArray, Allocator.Temp);
        var ngoSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ngoReader));

        value.NetworkSerialize(ngoSerializer);
    }

    protected override void Write(ref INetworkSerializable value, IDataWriter writer)
    {
        var ngoWriter = new FastBufferWriter(1024, Allocator.Temp, 65536);
        var ngoSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ngoWriter));

        value.NetworkSerialize(ngoSerializer);

        ByteArraySerializer.WriteValue(ngoSerializer.GetFastBufferWriter().ToArray(), writer);
    }
}
