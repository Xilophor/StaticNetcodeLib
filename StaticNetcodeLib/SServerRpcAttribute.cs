namespace StaticNetcodeLib;

using System;

/// <summary>
///     A static server rpc.
/// </summary>
/// <remarks>
///     The static rpc is considered unowned, as it is static and thus cannot have an instanced owner.
///     Therefore, any client can call the method.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class SServerRpcAttribute : Attribute;
