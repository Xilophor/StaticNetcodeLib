namespace StaticNetcodeLib.Messaging;

using System;
using System.Linq;
using System.Text;
using OdinSerializer;
using Unity.Collections;
using Unity.Netcode;

internal class UnnamedMessageHandler
{
    internal static UnnamedMessageHandler? Instance { get; private set; }

    private NetworkManager NetworkManager { get; }
    private CustomMessagingManager CustomMessagingManager { get; }

    private const string LibIdentifier = "LethalNetworkAPI";

    internal UnnamedMessageHandler()
    {
        Instance = this;

        this.NetworkManager = NetworkManager.Singleton;
        this.CustomMessagingManager = this.NetworkManager.CustomMessagingManager;
    }

    #region Helper Methods

    private static byte[] Serialize(object? data) => SerializationUtility.SerializeValue(data, DataFormat.Binary);

    private static T Deserialize<T>(byte[] serializedData) =>
        SerializationUtility.DeserializeValue<T>(serializedData, DataFormat.Binary);

    private static void WriteMessageData(out FastBufferWriter writer, MessageData messageData)
    {
        var (serializedMessage, size) = SerializeDataAndGetSize(messageData);

        writer = new FastBufferWriter(size, Allocator.Temp);

        writer.WriteValueSafe(LibIdentifier);
        writer.WriteValueSafe(serializedMessage);
    }

    private static Tuple<byte[], int> SerializeDataAndGetSize(MessageData messageData)
    {
        var size = 0;
        var serializedData = Serialize(messageData);

        size += Encoding.UTF8.GetByteCount(LibIdentifier);

        size += serializedData.Length;

        return new Tuple<byte[], int>(serializedData, size);
    }

    #endregion
}
