using System.Collections.Generic;

namespace Staple.Internal;

internal class MultidrawEntry
{
    public BufferAttributeContainer.Entries entries;

    public readonly List<Transform> transforms = [];
}
