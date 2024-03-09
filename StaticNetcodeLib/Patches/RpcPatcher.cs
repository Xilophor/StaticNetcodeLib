namespace StaticNetcodeLib.Patches;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enums;
using Messaging;
using Unity.Netcode;

internal class RpcPatcher
{
    internal static Dictionary<MethodBase, RpcExecStage> RpcExecStageLookup { get; } = [];

    public static bool PatchServerRpc(MethodBase __originalMethod, ref object[] __args)
    {
        if (!IsListening(out var networkManager)) return false;

        var execStage = RpcExecStageLookup[__originalMethod];

        // If the execStage is server, return true, else if the "client" is a server, return false
        if (execStage == RpcExecStage.Server)
            return true;
        if (!(networkManager!.IsClient || networkManager.IsHost))
            return false;

        SendServerRpc(__originalMethod, ref __args);
        return false;
    }

    public static bool PatchClientRpc(MethodBase __originalMethod, object[]? __args)
    {
        if (!IsListening(out var networkManager)) return false;

        var execStage = RpcExecStageLookup[__originalMethod];

        // If the execStage is client, return true, else if the calling type is a client, return false
        if (execStage == RpcExecStage.Client)
            return true;
        if (!(networkManager!.IsHost || networkManager.IsServer))
            return false;

        SendClientRpc(__originalMethod, __args);
        return false;
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

        var messageData = new MessageData(MessageType.ServerRpc, new RpcIdentifier(__originalMethod), __args);

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

    private static void SendClientRpc(MethodBase __originalMethod, object[]? __args)
    {
        if (!IsListening(out _)) return;

        var messageData = new MessageData(MessageType.ClientRpc, new RpcIdentifier(__originalMethod), __args);

        if (__args is not { Length: not 0 } || !__args.Any(arg => arg is ClientRpcParams))
        {
            UnnamedMessageHandler.Instance!.SendMessageToClient(messageData);
            return;
        }

        var clientRpcParams = (ClientRpcParams)__args.FirstOrDefault(arg => arg is ClientRpcParams);

        UnnamedMessageHandler.Instance!.SendMessageToClient(messageData, clientRpcParams);
    }

    #endregion
}
