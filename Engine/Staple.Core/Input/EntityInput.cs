namespace Staple;

public sealed class EntityInput : IComponent
{
    public InputActions actions;

    internal InputActions prevActions;
    internal int[] cachedActionIDs = [];
}
