namespace Staple;

/// <summary>
/// Represents an entity ID
/// </summary>
public struct EntityID
{
    /// <summary>
    /// The entity's ID
    /// </summary>
    public int ID;

    /// <summary>
    /// The entity's generation.
    /// This gets increased internally as an entity with the same ID is destroyed, thus making old references fail to reference it again.
    /// </summary>
    public int generation;

    public static bool operator ==(EntityID a, EntityID b)
    {
        return a.ID == b.ID;
    }

    public static bool operator !=(EntityID a, EntityID b)
    {
        return (a == b) == false;
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is EntityID ID)
        {
            return this == ID;
        }

        return false;
    }

    public override readonly int GetHashCode()
    {
        return ID.GetHashCode() * 17 + generation.GetHashCode();
    }

    public override readonly string ToString()
    {
        return $"{ID}:{generation}";
    }
}
