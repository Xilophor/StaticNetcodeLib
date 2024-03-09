# StaticNetcodeLib

[![Build](https://img.shields.io/github/actions/workflow/status/Xilophor/StaticNetcodeLib/build.yml?style=for-the-badge&logo=github)](https://github.com/Xilophor/StaticNetcodeLib/actions/workflows/build.yml)
[![Latest Version](https://img.shields.io/thunderstore/v/xilophor/StaticNetcodeLib?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/xilophor/StaticNetcodeLib)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/xilophor/StaticNetcodeLib?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/xilophor/StaticNetcodeLib)
[![NuGet Version](https://img.shields.io/nuget/v/Xilophor.StaticNetcodeLib?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Xilophor.StaticNetcodeLib)

This lib allows BepInEx mods to use Netcode for GameObjects in a static context.

## Usage

Add the appropriate BepInDependency attribute to your plugin class, like so:
```cs
[BepInDependency(StaticNetcodeLib.Guid, DependencyFlags.HardDependency)]
public class ExampleMod : BaseUnityPlugin
```

Then add the StaticNetcode attribute to any classes that have static rpcs.
```cs
[StaticNetcode]
public class ExampleNetworkingClass
```

After that, you can simply use Server & Client Rpcs as you normally would (even outside NetworkBehaviours), but in a static context, like so:
```cs
[ClientRpc]
public static void ExampleClientRpc(string exampleString)
{
    ExampleMod.Logger.LogDebug(exampleString);
}

/* ... */

ExampleClientRpc("Hello, world!");
```

Note: The Rpc attribute params are not respected for this. For example, ServerRpcs are static and thus cannot have an owner, and are used as if it had the `RequireOwnership = false` parameter.

## Acknowledgements

Thank you [@Lordfirespeed](https://github.com/Lordfirespeed) for being my rubber ducky.
