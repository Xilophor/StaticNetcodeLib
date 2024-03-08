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

    #region Messaging

    #region Send

    internal void SendMessageToClient(MessageData messageData, ClientRpcParams clientRpcParams = default)
    {
        WriteMessageData(out var writer, messageData);

        var clients = clientRpcParams.Send.TargetClientIds ??
            clientRpcParams.Send.TargetClientIdsNativeArray.GetValueOrDefault().ToArray();

        if (clients.Any())
        {
            this.CustomMessagingManager.SendUnnamedMessage(clients, writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }
        else
        {
            this.CustomMessagingManager.SendUnnamedMessageToAll(writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }

        writer.Dispose();
    }

    internal void SendMessageToServer(MessageData messageData)
    {
        WriteMessageData(out var writer, messageData);

        this.CustomMessagingManager.SendUnnamedMessage(NetworkManager.ServerClientId, writer,
            NetworkDelivery.ReliableFragmentedSequenced);

        writer.Dispose();
    }

    #endregion

    #endregion

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
