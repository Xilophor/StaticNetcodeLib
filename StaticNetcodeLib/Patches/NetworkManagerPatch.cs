namespace StaticNetcodeLib.Patches;

using HarmonyLib;
using Messaging;
using Unity.Netcode;

[HarmonyPatch(typeof(NetworkManager))]
[HarmonyPriority(Priority.HigherThanNormal)]
[HarmonyWrapSafe]
internal static class NetworkManagerPatch
{
    [HarmonyPatch(nameof(NetworkManager.Initialize))]
    [HarmonyPostfix]
    public static void InitializePatch() => _ = new UnnamedMessageHandler();

    [HarmonyPatch(nameof(NetworkManager.Shutdown))]
    [HarmonyPrefix]
    public static void ShutdownPatch() => UnnamedMessageHandler.Instance?.Dispose();
}
