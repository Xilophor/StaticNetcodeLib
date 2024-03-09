namespace StaticNetcodeLib;

using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Patches;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class StaticNetcodeLib : BaseUnityPlugin
{
    public static StaticNetcodeLib Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    private static Harmony? Harmony { get; set; }

    private static readonly HarmonyMethod ServerRpcPatch = new(typeof(RpcPatcher), nameof(RpcPatcher.PatchServerRpc));
    private static readonly HarmonyMethod ClientRpcPatch = new(typeof(RpcPatcher), nameof(RpcPatcher.PatchClientRpc));

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        SearchPluginsAndPatch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    private static void SearchPluginsAndPatch()
    {
        var pluginsToPatch = Chainloader.PluginInfos.Select(pair => pair.Value.Instance.GetType())
            .Where(type => type.GetCustomAttributes(typeof(BepInDependency), false)
                .Any(attr => ((BepInDependency)attr).DependencyGUID == MyPluginInfo.PLUGIN_GUID));

        var classesToPatch = pluginsToPatch.SelectMany(plugin =>
            plugin.Assembly.GetTypes().Where(type => type.GetCustomAttributes<StaticNetcodeAttribute>().Any())).ToArray();

        var serverRpcsToPatch = classesToPatch.SelectMany(type =>
            type.GetMethods().Where(method => method.GetCustomAttributes<SServerRpcAttribute>().Any() && method.IsStatic));
        var clientRpcsToPatch = classesToPatch.SelectMany(type =>
            type.GetMethods().Where(method => method.GetCustomAttributes<SClientRpcAttribute>().Any() && method.IsStatic));

        serverRpcsToPatch.Do(method => Harmony?.Patch(method, prefix: ServerRpcPatch));
        clientRpcsToPatch.Do(method => Harmony?.Patch(method, prefix: ClientRpcPatch));
    }
}
