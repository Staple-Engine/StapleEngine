using MessagePack;
using System;

namespace Staple.Internal;

internal static class SerializationUtils
{
    public static T MessagePackDeserialize<T>(ReadOnlyMemory<byte> data, out int read)
    {
        var result = MessagePackSerializer.Deserialize<T>(data, out read);

        MessagePackSerializer.DefaultOptions.SequencePool.Clear();

        return result;
    }
}
