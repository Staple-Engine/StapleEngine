using MessagePack;
using System.Text.Json.Serialization;

namespace Staple.Internal;

[MessagePackObject]
public class AssetHolder
{
    [Key(0)]
    public string guid;

    [Key(1)]
    public string typeName;
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(AssetHolder))]
internal partial class AssetHolderSerializationContext : JsonSerializerContext
{
}
