namespace StaticNetcodeLib.Serialization;

using OdinSerializer;
using Unity.Collections;
using Unity.Netcode;

// ReSharper disable once InconsistentNaming
/// <summary>
///     Custom formatter for the <see cref="INetworkSerializable"/> interface.
/// </summary>
public class INetworkSerializableFormatter<T> : MinimalBaseFormatter<T> where T : INetworkSerializable
{
    private readonly Serializer<byte[]> _byteArraySerializer = Serializer.Get<byte[]>();

    protected override void Read(ref T value, IDataReader reader)
    {
        var byteArray = this._byteArraySerializer.ReadValue(reader);

        var ngoReader = new FastBufferReader(byteArray, Allocator.Temp);
        var ngoSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ngoReader));

        value.NetworkSerialize(ngoSerializer);
    }

    protected override void Write(ref T value, IDataWriter writer)
    {
        var ngoWriter = new FastBufferWriter(1024, Allocator.Temp, 65536);
        var ngoSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ngoWriter));

        value.NetworkSerialize(ngoSerializer);

        this._byteArraySerializer.WriteValue(ngoSerializer.GetFastBufferWriter().ToArray(), writer);
    }
}
