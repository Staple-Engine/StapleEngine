namespace Staple;

public sealed class EntityInput : Component
{
    public InputActions actions;

    internal InputActions prevActions;
    internal int[] cachedActionIDs = [];
}
