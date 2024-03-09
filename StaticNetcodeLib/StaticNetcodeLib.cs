namespace StaticNetcodeLib;

using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Enums;
using HarmonyLib;
using Patches;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class StaticNetcodeLib : BaseUnityPlugin
{
    public const string Guid = MyPluginInfo.PLUGIN_GUID;

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

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void Start() => SearchPluginsAndPatch();

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
            .Where(type => type.GetCustomAttributes<BepInDependency>()
                .Any(attr => attr.DependencyGUID == MyPluginInfo.PLUGIN_GUID));

        var classesToPatch = pluginsToPatch.SelectMany(plugin =>
            plugin.Assembly.GetTypes().Where(type => type.GetCustomAttributes<StaticNetcodeAttribute>().Any())).ToArray();

        var methodsInClasses = classesToPatch.SelectMany(type =>
            type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).ToArray();

        var serverRpcsToPatch = methodsInClasses.Where(method => method.GetCustomAttributes<SServerRpcAttribute>().Any() && method.IsStatic);
        var clientRpcsToPatch = methodsInClasses.Where(method => method.GetCustomAttributes<SClientRpcAttribute>().Any() && method.IsStatic);

        serverRpcsToPatch.Do(method =>
        {
            try
            {
                Harmony?.Patch(method, prefix: ServerRpcPatch);
                RpcPatcher.RpcExecStageLookup[method] = RpcExecStage.None;
            }
            catch
            {
                Logger.LogError($"Unable to patch the method {method.FullDescription()}!");
            }
        });
        clientRpcsToPatch.Do(method =>
        {
            try
            {
                Harmony?.Patch(method, prefix: ClientRpcPatch);
                RpcPatcher.RpcExecStageLookup[method] = RpcExecStage.None;
            }
            catch
            {
                Logger.LogError($"Unable to patch the method {method.FullDescription()}!");
            }
        });
    }
}
