namespace StaticNetcodeLib;

using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Enums;
using HarmonyLib;
using Patches;
using Unity.Netcode;
using UnityEngine;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class StaticNetcodeLib : BaseUnityPlugin
{
    public const string Guid = MyPluginInfo.PLUGIN_GUID;

    public static StaticNetcodeLib Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Patch();

        GameObject patcherObject = new("StaticNetcodePatcherObject", typeof(MethodRegistration));
        DontDestroyOnLoad(patcherObject);

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }
}
