namespace StaticNetcodeLib.Serialization;

using System;
using System.IO;
using OdinSerializer;

internal static class StaticNetcodeSerializer
{
    public static readonly SerializationContext DefaultSerializationContext = new()
    {
        Config = new SerializationConfig
        {
            SerializationPolicy = SerializationPolicies.Everything
        }
    };

    public static readonly DeserializationContext DefaultDeserializationContext = new()
    {
        Config = new SerializationConfig
        {
            SerializationPolicy = SerializationPolicies.Everything
        }
    };

    public static byte[] SerializeObject(object? data) =>
        SerializationUtility.SerializeValueWeak(data, DataFormat.Binary, DefaultSerializationContext);

    public static byte[] Serialize<T>(T? data) =>
        SerializationUtility.SerializeValue(data, DataFormat.Binary, DefaultSerializationContext);

    public static object DeserializeObjectWithType(byte[] serializedData, Type type)
    {
        var reader = SerializationUtility.CreateReader(new MemoryStream(serializedData), DefaultDeserializationContext, DataFormat.Binary);
        return Serializer.Get(type).ReadValueWeak(reader);
    }

    public static T Deserialize<T>(byte[] serializedData) =>
        SerializationUtility.DeserializeValue<T>(serializedData, DataFormat.Binary, DefaultDeserializationContext);
}
