using Staple.Internal;
using System.Collections.Generic;

namespace Staple;

public sealed class InputActions : IStapleAsset, IGuidAsset
{
    public List<InputAction> actions = [];

    public int GuidHash { get; set; }

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<InputActions>(guid);
    }
}
