using MessagePack;
using System;

namespace Staple.Internal;

[MessagePackObject]
public class TextAssetMetadata
{
    [HideInInspector]
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [HideInInspector]
    [Key(1)]
    public string typeName = typeof(TextAsset).FullName;
}
