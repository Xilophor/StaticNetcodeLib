namespace StaticNetcodeLib.Patches;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enums;
using Messaging;
using Unity.Netcode;

internal class RpcPatcher
{
    internal static Dictionary<MethodBase, Tuple<Identifier, RpcExecStage>> RpcLookup { get; } = [];

    public static bool PatchServerRpc(MethodBase __originalMethod, ref object[] __args)
    {
        if (!IsListening(out var networkManager)) return false;

        var (_, execStage) = RpcLookup[__originalMethod];

        // If the execStage is server, or the calling type is a server, return true
        if (execStage == RpcExecStage.Server || !(networkManager!.IsClient || networkManager.IsHost))
            return true;

        SendServerRpc(__originalMethod, ref __args);

        // Return false if the client is not the host.
        return networkManager.IsHost;
    }

    public static bool PatchClientRpc(MethodBase __originalMethod, object[] __args)
    {
        if (!IsListening(out var networkManager)) return false;

        var (_, execStage) = RpcLookup[__originalMethod];

        // If the execStage is client, or the calling type is a client, return true
        if (execStage == RpcExecStage.Client || !(networkManager!.IsHost || networkManager.IsServer))
            return true;

        SendClientRpc(__originalMethod, __args);

        // Return false if the "client" is a server
        return networkManager.IsHost;
    }

    #region Helper Methods

    private static bool IsListening(out NetworkManager? networkManager)
    {
        networkManager = NetworkManager.Singleton;
        return networkManager != null && networkManager.IsListening && UnnamedMessageHandler.Instance != null;
    }

    private static void SendServerRpc(MethodBase __originalMethod, ref object[] __args)
    {
        if (!IsListening(out var networkManager)) return;

        var (identifier, _) = RpcLookup[__originalMethod];
        var messageData = new MessageData(MessageType.ServerRpc, identifier, __args);

        // Reverse as ServerRpcParams is typically the last param, slight performance edge
        for (var i = __args.Length - 1; i >= 0; i--)
        {
            if (__args[i] is not ServerRpcParams serverRpcParams) continue;

            serverRpcParams.Receive.SenderClientId = networkManager!.LocalClientId;
            __args[i] = serverRpcParams;

            break;
        }

        UnnamedMessageHandler.Instance!.SendMessageToServer(messageData);
    }

    private static void SendClientRpc(MethodBase __originalMethod, object[] __args)
    {
        if (!IsListening(out _)) return;

        var (identifier, _) = RpcLookup[__originalMethod];
        var messageData = new MessageData(MessageType.ClientRpc, identifier, __args);

        var clientRpcParams = (ClientRpcParams)__args.FirstOrDefault(arg => arg is ClientRpcParams);

        UnnamedMessageHandler.Instance!.SendMessageToClient(messageData, clientRpcParams);
    }

    #endregion
}
