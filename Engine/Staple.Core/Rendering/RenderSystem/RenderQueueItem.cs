using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

internal class RenderQueueItem
{
    public BufferAttributeContainer.Entries staticMeshEntries;
    public readonly List<Matrix4x4> instanceTransforms = [];
}
