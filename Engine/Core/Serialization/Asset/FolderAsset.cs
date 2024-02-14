using MessagePack;

namespace Staple.Internal;

[MessagePackObject]
public class FolderAsset
{
    [Key(0)]
    public string guid;

    [Key(1)]
    public string typeName;

    [Key(2)]
    public string pakName;
}
