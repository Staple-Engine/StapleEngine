using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// ID from a String, using <see cref="object.GetHashCode"/>.
/// Useful for debugging strings while storing only ints internally, so useful for dictionaries
/// </summary>
public readonly struct StringID : IEquatable<StringID>
{
    private static readonly Dictionary<int, string> stringCache = [];

    public readonly int ID;

    public StringID(string name)
    {
        ID = name.GetHashCode();

        stringCache.AddOrSetKey(ID, name);
    }

    public static implicit operator StringID(string s) => new(s);

    public override readonly string ToString()
    {
        var name = stringCache.TryGetValue(ID, out var n) ? n : "(invalid)";

        return $"{name} ({ID})";
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is StringID s && ID == s.ID;
    }

    public override readonly int GetHashCode()
    {
        return ID.GetHashCode();
    }

    public bool Equals(StringID other)
    {
        return ID == other.ID;
    }

    public static bool operator ==(StringID left, StringID right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringID left, StringID right)
    {
        return left != right;
    }
}
