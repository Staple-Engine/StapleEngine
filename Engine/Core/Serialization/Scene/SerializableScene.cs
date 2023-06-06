using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple.Internal
{
    public enum SceneObjectKind
    {
        Entity
    }

    public enum SceneComponentParameterType
    {
        Bool,
        Int,
        Float,
        String
    }

    [Serializable]
    [MessagePackObject]
    public class SceneObjectTransform
    {
        [Key(0)]
        public Vector3Holder position = new();

        [Key(1)]
        public Vector3Holder rotation = new();

        [Key(2)]
        public Vector3Holder scale = new();
    }

    [MessagePackObject]
    public class SceneComponentParameter
    {
        [Key(0)]
        public string name;

        [Key(1)]
        public SceneComponentParameterType type;

        [Key(2)]
        public bool boolValue;

        [Key(3)]
        public int intValue;

        [Key(4)]
        public float floatValue;

        [Key(5)]
        public string stringValue;
    }

    [Serializable]
    [MessagePackObject]
    public class SceneComponent
    {
        [Key(0)]
        public string type;

        [Key(1)]
        public List<SceneComponentParameter> parameters = new();

        [IgnoreMember]
        public Dictionary<string, object> data;

        public bool ShouldSerializeparameters()
        {
            return false;
        }
    }

    [Serializable]
    [MessagePackObject]
    public class SceneObject
    {
        [Key(0)]
        public SceneObjectKind kind;

        [Key(1)]
        public string name;

        [Key(2)]
        public int ID;

        [Key(3)]
        public int parent;

        [Key(4)]
        public SceneObjectTransform transform = new();

        [Key(5)]
        public List<SceneComponent> components = new();

        [Key(6)]
        public string layer;
    }

    [MessagePackObject]
    public class SerializableSceneHeader
    {
        [IgnoreMember]
        public readonly static char[] ValidHeader = new char[]
        {
            'S', 'S', 'C', 'E'
        };

        [IgnoreMember]
        public const byte ValidVersion = 1;

        [Key(0)]
        public char[] header = ValidHeader;

        [Key(1)]
        public byte version = ValidVersion;
    }

    [MessagePackObject]
    public class SerializableScene
    {
        [Key(0)]
        public List<SceneObject> objects = new();
    }
}
