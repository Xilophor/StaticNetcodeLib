namespace StaticNetcodeLib;

using System.Reflection;
using OdinSerializer;

public readonly struct RpcIdentifier(
    MethodBase rpcMethod
) : IIdentifier
{
    [OdinSerialize]
    public MethodBase RpcMethod { get; } = rpcMethod;

    public string Identifier => this.RpcMethod.Name;
}
