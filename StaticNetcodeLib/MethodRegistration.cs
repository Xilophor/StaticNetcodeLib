namespace StaticNetcodeLib;

using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using Enums;
using HarmonyLib;
using Patches;
using Unity.Netcode;
using UnityEngine;

internal class MethodRegistration : MonoBehaviour
{
    private static readonly HarmonyMethod ServerRpcPatch = new(typeof(RpcPatcher), nameof(RpcPatcher.PatchServerRpc));
    private static readonly HarmonyMethod ClientRpcPatch = new(typeof(RpcPatcher), nameof(RpcPatcher.PatchClientRpc));

    private void Start()
    {
        var pluginsToPatch = Chainloader.PluginInfos.Select(pair => pair.Value.Instance.GetType())
            .Where(type => type.GetCustomAttributes<BepInDependency>()
                .Any(attr => attr.DependencyGUID == MyPluginInfo.PLUGIN_GUID));

        var classesToPatch = pluginsToPatch.SelectMany(plugin =>
            plugin.Assembly.GetTypes().Where(type => type.GetCustomAttributes<StaticNetcodeAttribute>().Any())).ToArray();

        var methodsInClasses = classesToPatch.SelectMany(type =>
            type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).ToArray();

        var serverRpcsToPatch = methodsInClasses.Where(method => method.GetCustomAttributes<ServerRpcAttribute>().Any() && method.IsStatic);
        var clientRpcsToPatch = methodsInClasses.Where(method => method.GetCustomAttributes<ClientRpcAttribute>().Any() && method.IsStatic);

        serverRpcsToPatch.Do(method =>
        {
            if (!method.Name.EndsWith("ServerRpc"))
            {
                StaticNetcodeLib.Logger.LogError($"Method {method.FullDescription()} must end with ServerRpc.");
                return;
            }
            try
            {
                StaticNetcodeLib.Harmony?.Patch(method, prefix: ServerRpcPatch);
                RpcPatcher.RpcExecStageLookup[method] = RpcExecStage.None;
            }
            catch
            {
                StaticNetcodeLib.Logger.LogError($"Unable to patch the method {method.FullDescription()}!");
            }
        });
        clientRpcsToPatch.Do(method =>
        {
            if (!method.Name.EndsWith("ClientRpc"))
            {
                StaticNetcodeLib.Logger.LogError($"Method {method.FullDescription()} must end with ClientRpc.");
                return;
            }
            try
            {
                StaticNetcodeLib.Harmony?.Patch(method, prefix: ClientRpcPatch);
                RpcPatcher.RpcExecStageLookup[method] = RpcExecStage.None;
            }
            catch
            {
                StaticNetcodeLib.Logger.LogError($"Unable to patch the method {method.FullDescription()}!");
            }
        });

        Destroy(this);
    }
}
