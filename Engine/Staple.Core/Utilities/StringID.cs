using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Staple;

/// <summary>
/// ID from a String, using <see cref="object.GetHashCode"/>.
/// Useful for debugging strings while storing only ints internally, so useful for dictionaries
/// </summary>
public readonly struct StringID : IEquatable<StringID>
{
    private static readonly Dictionary<int, string> stringCache = [];
    private static readonly Lock stringLock = new();

    public readonly int ID;

    public StringID(string name)
    {
        ID = name.GetHashCode();

        lock(stringLock)
        {
            stringCache.AddOrSetKey(ID, name);
        }
    }

    public StringID(StringID other)
    {
        ID = other.ID;
    }

    public static implicit operator StringID(string s) => new(s);

    public override readonly string ToString()
    {
        lock (stringLock)
        {
            var name = stringCache.TryGetValue(ID, out var n) ? n : "(invalid)";

            return $"{name} ({ID})";
        }
    }

    public override readonly bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is StringID s && ID == s.ID;
    }

    public override readonly int GetHashCode()
    {
        return ID;
    }

    public readonly bool Equals(StringID other)
    {
        return ID == other.ID;
    }

    public static bool operator ==(StringID left, StringID right)
    {
        return left.ID == right.ID;
    }

    public static bool operator !=(StringID left, StringID right)
    {
        return left.ID != right.ID;
    }
}
