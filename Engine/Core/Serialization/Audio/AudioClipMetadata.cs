using MessagePack;
using System;

namespace Staple.Internal;

public enum AudioRecompression
{
    None,
    Vorbis,
}

[MessagePackObject]
public class AudioClipMetadata
{
    [HideInInspector]
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [HideInInspector]
    [Key(1)]
    public string typeName = typeof(AudioClip).FullName;

    [Key(2)]
    public bool loadInBackground = false;

    [Key(3)]
    public AudioRecompression recompression;

    [Key(4)]
    [Range(0, 1)]
    public float recompressionQuality = 1.0f;

    public AudioClipMetadata Clone()
    {
        return new AudioClipMetadata()
        {
            guid = guid,
            loadInBackground = loadInBackground,
            recompression = recompression,
            recompressionQuality = recompressionQuality,
            typeName = typeName,
        };
    }

    public static bool operator ==(AudioClipMetadata lhs, AudioClipMetadata rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.guid == rhs.guid &&
            lhs.typeName == rhs.typeName &&
            lhs.loadInBackground == rhs.loadInBackground &&
            lhs.recompression == rhs.recompression &&
            lhs.recompressionQuality == rhs.recompressionQuality;
    }

    public static bool operator !=(AudioClipMetadata lhs, AudioClipMetadata rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.guid != rhs.guid ||
            lhs.typeName != rhs.typeName ||
            lhs.loadInBackground != rhs.loadInBackground ||
            lhs.recompression != rhs.recompression ||
            lhs.recompressionQuality != rhs.recompressionQuality;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is AudioClipMetadata rhs)
        {
            return this == rhs;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(guid, typeName, loadInBackground, recompression, recompressionQuality);
    }
}
