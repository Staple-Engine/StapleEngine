namespace Staple;

/// <summary>
/// Manages a Guid Hash for faster Guid comparisons
/// </summary>
public class GuidHasher
{
    private int guidHash;
    private string guid;

    public int GuidHash => guidHash;

    public string Guid
    {
        get => guid;

        set
        {
            guid = value;

            guidHash = guid?.GetHashCode() ?? 0;
        }
    }

    public override string ToString()
    {
        return guid;
    }
}
