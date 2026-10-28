namespace Staple.Internal;

internal class MultidrawEntry
{
    public BufferAttributeContainer.Entries entries;
    public int startIndex;
    public int indexCount;

    public readonly ExpandableContainer<Transform> transforms = new(false);
}
