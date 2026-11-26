using System;
using System.Collections;

namespace Staple.Internal;

public interface IStapleUnmanagedArraySerializer
{
    byte[] SerializeArray(Array array);

    byte[] SerializeList(IList list);

    void DeserializeArray(byte[] data, Array target);
}
