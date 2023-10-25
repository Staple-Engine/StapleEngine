using MessagePack;
using System;

namespace Staple.Internal
{
    [MessagePackObject]
    public class AudioClipMetadata
    {
        [HideInInspector]
        [Key(0)]
        public string guid = Guid.NewGuid().ToString();

        [HideInInspector]
        [Key(1)]
        public string typeName = typeof(AudioClip).FullName;

        public AudioClipMetadata Clone()
        {
            return new AudioClipMetadata()
            {
                guid = guid,
                typeName = typeName,
            };
        }

        public static bool operator ==(AudioClipMetadata lhs, AudioClipMetadata rhs)
        {
            return lhs.guid == rhs.guid &&
                lhs.typeName == rhs.typeName;
        }

        public static bool operator !=(AudioClipMetadata lhs, AudioClipMetadata rhs)
        {
            return lhs.guid != rhs.guid ||
                lhs.typeName != rhs.typeName;
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
    }
}
