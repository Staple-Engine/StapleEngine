using MessagePack;

namespace Staple.Internal;

[MessagePackObject]
public class FolderAsset
{
    [HideInInspector]
    [Key(0)]
    public string guid;

    [HideInInspector]
    [Key(1)]
    public string typeName;

    [Key(2)]
    public string pakName;
}
