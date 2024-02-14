namespace Staple;

/// <summary>
/// Represents an entity
/// </summary>
public struct Entity
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

    /// <summary>
    /// An empty entity
    /// </summary>
    public readonly static Entity Empty = new()
    {
        ID = -1,
        generation = 0,
    };

    public static bool operator==(Entity a, Entity b)
    {
        return a.ID == b.ID && a.generation == b.generation;
    }

    public static bool operator!=(Entity a, Entity b)
    {
        return (a == b) == false;
    }

    public override bool Equals(object obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is Entity entity)
        {
            return this == entity;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode() * 17 + generation.GetHashCode();
    }

    public override string ToString()
    {
        return $"Entity ({ID} {generation})";
    }
}
