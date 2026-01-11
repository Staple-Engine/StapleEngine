namespace Staple;

/// <summary>
/// Manages a Guid Hash for faster Guid comparisons
/// </summary>
public class GuidHasher
{
    private string guid;

    public int GuidHash { get; private set; }

    public string Guid
    {
        get => guid;

        set
        {
            guid = value;

            GuidHash = guid?.GetHashCode() ?? 0;
        }
    }

    public override string ToString()
    {
        return guid;
    }
}
