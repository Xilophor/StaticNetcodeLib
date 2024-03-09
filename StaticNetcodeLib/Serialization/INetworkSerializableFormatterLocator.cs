
using OdinSerializer;
using StaticNetcodeLib.Serialization;

[assembly: RegisterFormatterLocator(typeof(INetworkSerializableFormatterLocator), -100)]

namespace StaticNetcodeLib.Serialization;

using System;
using Unity.Netcode;

// ReSharper disable once InconsistentNaming
public class INetworkSerializableFormatterLocator : IFormatterLocator
{
    public bool TryGetFormatter(Type type, FormatterLocationStep step, ISerializationPolicy policy,
        bool allowWeakFallbackFormatters, out IFormatter formatter)
    {
        if (step != FormatterLocationStep.AfterRegisteredFormatters || !typeof(INetworkSerializable).IsAssignableFrom(type))
        {
            formatter = null;
            return false;
        }

        try
        {
            formatter = (IFormatter)Activator.CreateInstance(typeof(INetworkSerializableFormatter<>).MakeGenericType(type));
        }
        catch (Exception ex)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (allowWeakFallbackFormatters && (ex is ExecutionEngineException || ex.GetBaseException() is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                formatter = new WeakSerializableFormatter(type);
            }
            else throw;
        }

        return true;
    }
}
