using OdinSerializer;
using StaticNetcodeLib.Serialization;

[assembly: RegisterFormatter(typeof(RpcIdentifierFormatter))]

namespace StaticNetcodeLib.Serialization;

using System;
using HarmonyLib;
using Messaging;

/// <summary>
///     Custom formatter for the <see cref="RpcIdentifier"/> readonly struct.
/// </summary>
public class RpcIdentifierFormatter : MinimalBaseFormatter<RpcIdentifier>
{
    private static readonly Serializer<string> StringSerializer = Serializer.Get<string>();
    private static readonly Serializer<Type[]> TypeArraySerializer = Serializer.Get<Type[]>();

    protected override void Read(ref RpcIdentifier value, IDataReader reader) =>
        value = new RpcIdentifier(AccessTools.Method(
            StringSerializer.ReadValue(reader),
            TypeArraySerializer.ReadValue(reader)
        ));

    protected override void Write(ref RpcIdentifier value, IDataWriter writer)
    {
        var methodBase = value.RpcMethod;

        StringSerializer.WriteValue($"{methodBase.DeclaringType!.FullName}:{methodBase.Name}", writer);
        TypeArraySerializer.WriteValue(methodBase.GetParameters().Types(), writer);
    }
}
