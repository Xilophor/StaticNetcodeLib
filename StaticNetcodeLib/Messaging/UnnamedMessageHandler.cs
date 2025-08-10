namespace StaticNetcodeLib.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Enums;
using Patches;
using Serialization;
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

        // Prevent the server from sending a message to itself and receive the message instead.
        if (clients.Any(client => client == NetworkManager.ServerClientId))
        {
            clients = clients.Where(client => client != NetworkManager.ServerClientId) as IReadOnlyList<ulong>;

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            this.ReceiveMessage(NetworkManager.ServerClientId, reader);

            if (clients is null or { Count: 0 }) { writer.Dispose(); return; }
        }

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

        message.ReadValueSafe(out MessageType messageType);
        message.ReadValueSafe(out byte[] serializedMethodBase);
        message.ReadValueSafe(out int paramCount);

        var methodBase = StaticNetcodeSerializer.Deserialize<MethodBase>(serializedMethodBase);
        var paramTypes = methodBase.GetParameters().Select(p => p.ParameterType).ToArray();

        var paramArray = new List<object>();
        for (var i = 0; i < paramCount; i++)
        {
            message.ReadValueSafe(out byte[] serializedParam);
            paramArray.Add(StaticNetcodeSerializer.DeserializeObjectWithType(serializedParam, paramTypes[i]));
        }

        MessageData messageData = new(messageType, methodBase, paramArray.ToArray());

        switch (messageType)
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
        var (_, methodBase, parameters) = messageData.AsValueTuple();

        methodBase = methodBase ?? throw new NullReferenceException("MethodBase is null.");
        var objectArray = parameters is { Length: 0 } or null ? [] : parameters;
        var execStage = messageData.MessageType == MessageType.ServerRpc ? RpcExecStage.Server : RpcExecStage.Client;

        RpcPatcher.RpcExecStageLookup[methodBase] = execStage;
        methodBase.Invoke(null, objectArray);
        RpcPatcher.RpcExecStageLookup[methodBase] = RpcExecStage.None;
    }

    #endregion

    #endregion

    #region Helper Methods

    private static void WriteMessageData(out FastBufferWriter writer, MessageData messageData)
    {
        var (serializedMessageBase, serializedMessage, size) = SerializeDataAndGetSize(messageData);

        writer = new FastBufferWriter(size, Allocator.Temp);

        writer.WriteValueSafe(LibIdentifier);
        writer.WriteValueSafe(messageData.MessageType);
        writer.WriteValueSafe(serializedMessageBase);
        writer.WriteValueSafe(messageData.Data?.Length ?? 0);
        foreach (var param in serializedMessage)
            writer.WriteValueSafe(param);
    }

    private static (byte[], byte[][], int) SerializeDataAndGetSize(MessageData messageData)
    {
        var size = 0;
        var serializedData = messageData.Data?.Select(StaticNetcodeSerializer.SerializeObject).ToArray() ?? [];
        var serializedMessageBase = StaticNetcodeSerializer.Serialize(messageData.MethodBase);

        size += Encoding.UTF8.GetByteCount(LibIdentifier);
        size += sizeof(MessageType);
        size += serializedMessageBase.Length;
        size += serializedData.Sum(byteArray => byteArray.Length);
        size += 100;

        return (serializedMessageBase, serializedData, size);
    }

    #endregion

    public void Dispose() => this.CustomMessagingManager.OnUnnamedMessage -= this.ReceiveMessage;
}
