using OdinSerializer;
using StaticNetcodeLib.Serialization;

[assembly: RegisterFormatter(typeof(MethodBaseFormatter))]

namespace StaticNetcodeLib.Serialization;

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OdinSerializer.Utilities;

/// <summary>
///     Custom formatter for the <see cref="MethodBase"/> abstract class.
/// </summary>
public class MethodBaseFormatter : MinimalBaseFormatter<MethodBase>
{
    private static readonly Serializer<string> StringSerializer = Serializer.Get<string>();
    private static readonly Serializer<Type[]> TypeArraySerializer = Serializer.Get<Type[]>();

    protected override void Read(ref MethodBase value, IDataReader reader) =>
        value = AccessTools.Method(
            StringSerializer.ReadValue(reader),
            TypeArraySerializer.ReadValue(reader),
            TypeArraySerializer.ReadValue(reader)
        );

    protected override void Write(ref MethodBase value, IDataWriter writer)
    {
        StringSerializer.WriteValue($"{value.DeclaringType!.FullName}:{value.Name}", writer);
        TypeArraySerializer.WriteValue(value.GetParameters().Types(), writer);
        TypeArraySerializer.WriteValue(value.GetGenericArguments().ToArray(), writer);
    }
}
