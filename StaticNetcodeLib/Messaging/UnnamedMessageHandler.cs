namespace StaticNetcodeLib.Messaging;

using System;
using System.Linq;
using System.Text;
using Enums;
using OdinSerializer;
using Patches;
using Unity.Collections;
using Unity.Netcode;

internal class UnnamedMessageHandler : IDisposable
{
    internal static UnnamedMessageHandler? Instance { get; private set; }

    private NetworkManager NetworkManager { get; }
    private CustomMessagingManager CustomMessagingManager { get; }

    private const string LibIdentifier = "StaticNetcodeLib";

    internal UnnamedMessageHandler()
    {
        Instance = this;

        this.NetworkManager = NetworkManager.Singleton;
        this.CustomMessagingManager = this.NetworkManager.CustomMessagingManager;

        this.CustomMessagingManager.OnUnnamedMessage += this.ReceiveMessage;
    }

    #region Messaging

    #region Send

    internal void SendMessageToClient(MessageData messageData, ClientRpcParams clientRpcParams = default)
    {
        WriteMessageData(out var writer, messageData);

        var clients = clientRpcParams.Send.TargetClientIds ??
            clientRpcParams.Send.TargetClientIdsNativeArray.GetValueOrDefault().ToArray();

        if (clients.Any())
            this.CustomMessagingManager.SendUnnamedMessage(clients, writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        else
            this.CustomMessagingManager.SendUnnamedMessageToAll(writer,
                NetworkDelivery.ReliableFragmentedSequenced);

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

    #region Receive

    private void ReceiveMessage(ulong clientId, FastBufferReader message)
    {
        message.ReadValueSafe(out string identifier);

        if (identifier != LibIdentifier) return;

        message.ReadValueSafe(out byte[] serializedMessageData);

        var messageData = Deserialize<MessageData>(serializedMessageData);

        switch (messageData.MessageType)
        {
            case MessageType.ServerRpc or MessageType.ClientRpc:
                this.ReceiveRpc(messageData);
                break;
            case MessageType.Variable:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ReceiveRpc(MessageData messageData)
    {
        var (_, identifier, parameters) = messageData.AsValueTuple();

        var methodBase = (identifier as RpcIdentifier? ?? throw new NullReferenceException()).RpcMethod;
        var objectArray = (object[]?)parameters is { Length: 0 } ? null : (object[]?)parameters;

        var execStage = messageData.MessageType == MessageType.ServerRpc ? RpcExecStage.Server : RpcExecStage.Client;

        RpcPatcher.RpcExecStageLookup[methodBase] = execStage;
        methodBase.Invoke(null, objectArray);
        RpcPatcher.RpcExecStageLookup[methodBase] = RpcExecStage.None;
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

    private static (byte[], int) SerializeDataAndGetSize(MessageData messageData)
    {
        var size = 0;
        var serializedData = Serialize(messageData);

        size += Encoding.UTF8.GetByteCount(LibIdentifier);
        size += serializedData.Length;

        return (serializedData, size);
    }

    #endregion

    public void Dispose() => this.CustomMessagingManager.OnUnnamedMessage -= this.ReceiveMessage;
}
