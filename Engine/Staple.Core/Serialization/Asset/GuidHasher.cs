using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Manages a Guid Hash for faster Guid comparisons
/// </summary>
public class GuidHasher : IEquatable<GuidHasher>
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

    public static bool operator==(GuidHasher lhs, GuidHasher rhs)
    {
        return lhs.GuidHash == rhs.GuidHash;
    }

    public static bool operator !=(GuidHasher lhs, GuidHasher rhs)
    {
        return lhs.GuidHash != rhs.GuidHash;
    }

    public override bool Equals(object obj)
    {
        return obj is GuidHasher hasher && this == hasher;
    }

    public bool Equals(GuidHasher other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Guid, GuidHash);
    }

    public override string ToString()
    {
        return guid;
    }
}
