using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Staple.Internal;

public interface IStapleTypeSerializer<T>
{
    void Serialize(StapleSerializerWriter writer, T value, StapleSerializationMode mode);

    T Deserialize(StapleSerializerReader reader, StapleSerializationMode mode);
}
