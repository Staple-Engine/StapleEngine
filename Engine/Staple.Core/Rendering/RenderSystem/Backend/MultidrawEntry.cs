namespace Staple.Internal;

internal class MultidrawEntry
{
    internal class TransformVisibility(Transform transform, bool visible, Renderable renderer)
    {
        public Transform transform = transform;
        public bool visible = visible;
        public Renderable renderer = renderer;
    }

    public BufferAttributeContainer.Entries entries;
    public int startIndex;
    public int indexCount;
    public int instanceCount;

    public readonly ExpandableContainer<TransformVisibility> transforms = new(false);
}
