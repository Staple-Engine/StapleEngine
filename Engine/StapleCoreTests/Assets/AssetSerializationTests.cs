using Staple;
using Staple.Internal;
using System.Numerics;
using System.Text.Json;

namespace CoreTests;

internal class AssetSerializationTests
{
    public enum NewEnum
    {
        A,
        B
    }

    [Flags]
    public enum NewFlagEnum
    {
        A = (1 << 1),
        B = (1 << 2),
    }

    internal class StapleTypesAsset : IStapleAsset
    {
        public byte b;
        public sbyte sb;
        public ushort us;
        public short s;
        public uint ui;
        public int i;
        public ulong ul;
        public long l;
        public float f;
        public double d;
        public bool bo;
        public string str;
        public Vector2 v2;
        public Vector3 v3;
        public Vector4 v4;
        public Quaternion q;
        public Vector2Int v2i;
        public Vector3Int v3i;
        public Vector4Int v4i;
        public NewEnum ne;
        public NewFlagEnum nfe;
        public Color c;
        public Color32 c32;
        public LayerMask lm;
        public Rect r;
        public RectFloat rf;
        public IGuidAsset asset;
    }

    internal class SimplePathAsset : IGuidAsset
    {
        private readonly GuidHasher guidhasher = new();

        public GuidHasher Guid => guidhasher;

        public static object Create(string guid)
        {
            var a = new SimplePathAsset();

            a.Guid.Guid = guid;

            return a;
        }
    }

    internal class SimpleAsset : IStapleAsset
    {
        public int intValue = 1;
        public string stringValue = "test";
        public List<int> numbers = new(new int[] { 1, 2, 3 });

        public IGuidAsset pathAsset = (SimplePathAsset)SimplePathAsset.Create("a/b/c");

        public NewEnum enumValue = NewEnum.A;

        internal int notSerialized = 0;

        [NonSerialized]
        public int notSerialized2 = 0;
    }

    internal class Base64ListAsset : IStapleAsset
    {
        [SerializeAsBase64]
        public List<int> values = new();
    }

    internal class SerializableAsset : IStapleAsset
    {
        [Serializable]
        public class InnerClass
        {
            [Serializable]
            public class InnerInnerClass
            {
                public int value = 3;
            }

            public InnerInnerClass container;
        }

        public InnerClass container;
    }

    internal class PrimitiveAsset : IStapleAsset
    {
        public bool[] flags;

        [SerializeAsBase64]
        public int[] values;

        public float[] floatValues;

        public IGuidAsset[] assets;

        public string[] strings;
    }

    internal class SerializeFieldAsset : IStapleAsset
    {
        private int hiddenField;

        [SerializeField]
        private int serializedField;

        public void SetHiddenField(int value)
        {
            hiddenField = value;
        }

        public void SetSerializedField(int value)
        {
            serializedField = value;
        }

        public int GetHiddenField() => hiddenField;

        public int GetSerializedField() => serializedField;
    }

    [Test]
    public void TestSerialize()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new SimpleAsset
        {
            intValue = 2,
            stringValue = "different",
        };

        asset.numbers.Clear();
        asset.numbers.Add(123);

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.EqualTo(null));

        Assert.That(result.typeName, Is.EqualTo(typeof(SimpleAsset).FullName));

        Assert.That(result.parameters, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            Assert.That(result.parameters.ContainsKey(nameof(SimpleAsset.intValue)), Is.True);
            Assert.That(result.parameters.ContainsKey(nameof(SimpleAsset.stringValue)), Is.True);
            Assert.That(result.parameters.ContainsKey(nameof(SimpleAsset.numbers)), Is.True);
            Assert.That(result.parameters.ContainsKey(nameof(SimpleAsset.pathAsset)), Is.True);
            Assert.That(result.parameters.ContainsKey(nameof(SimpleAsset.enumValue)), Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.parameters[nameof(SimpleAsset.intValue)].typeName, Is.EqualTo(typeof(int).FullName));
            Assert.That(result.parameters[nameof(SimpleAsset.stringValue)].typeName, Is.EqualTo(typeof(string).FullName));
            Assert.That(result.parameters[nameof(SimpleAsset.numbers)].typeName, Is.EqualTo(typeof(List<int>).FullName));
            Assert.That(result.parameters[nameof(SimpleAsset.pathAsset)].typeName, Is.EqualTo(typeof(SimplePathAsset).FullName));
            Assert.That(result.parameters[nameof(SimpleAsset.enumValue)].typeName, Is.EqualTo(typeof(NewEnum).FullName));

            Assert.That(result.parameters[nameof(SimpleAsset.intValue)].value, Is.EqualTo(asset.intValue));
            Assert.That(result.parameters[nameof(SimpleAsset.stringValue)].value, Is.EqualTo(asset.stringValue));
            Assert.That(result.parameters[nameof(SimpleAsset.numbers)].value, Is.EqualTo(asset.numbers));
            Assert.That(result.parameters[nameof(SimpleAsset.pathAsset)].value, Is.EqualTo(asset.pathAsset.Guid.Guid));
            Assert.That(result.parameters[nameof(SimpleAsset.enumValue)].value, Is.EqualTo(asset.enumValue.ToString()));
        });

        asset.pathAsset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path");

        result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result.parameters[nameof(SimpleAsset.pathAsset)].value, Is.EqualTo("valid path"));
    }

    [Test]
    public void TestDeserialize()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new SimpleAsset
        {
            intValue = 2,
            stringValue = "different",
            enumValue = NewEnum.B,
        };

        asset.numbers.Clear();
        asset.numbers.Add(123);

        asset.pathAsset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path");

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.EqualTo(null));

        var newResult = AssetSerialization.Deserialize(result, StapleSerializationMode.Binary);

        Assert.That(newResult, Is.Not.EqualTo(null));

        Assert.That(newResult, Is.TypeOf<SimpleAsset>());

        var newAsset = newResult as SimpleAsset;

        Assert.That(newAsset, Is.Not.EqualTo(null));

        Assert.That(newAsset.intValue, Is.EqualTo(asset.intValue));
        Assert.That(newAsset.stringValue, Is.EqualTo(asset.stringValue));
        Assert.That(newAsset.numbers, Is.EqualTo(asset.numbers));
        Assert.That(newAsset.pathAsset != null);
        Assert.That(newAsset.pathAsset.Guid.Guid, Is.EqualTo("valid path"));
        Assert.That(newAsset.enumValue, Is.EqualTo(asset.enumValue));
    }

    [Test]
    public void TestSerializeSerializable()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new SerializableAsset
        {
            container = new()
            {
                container = null,
            }
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.EqualTo(null));

        Assert.That(result.typeName, Is.EqualTo(typeof(SerializableAsset).FullName));

        Assert.That(result.parameters, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.parameters.ContainsKey(nameof(SerializableAsset.container)), Is.True);
            Assert.That(result.parameters[nameof(SerializableAsset.container)].value, Is.TypeOf(typeof(SerializableStapleAssetContainer)));

            if (result.parameters.TryGetValue(nameof(SerializableAsset.container), out var parameter))
            {
                Assert.That(parameter.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass).FullName));
                Assert.That(parameter.value, Is.TypeOf<SerializableStapleAssetContainer>());

                if (parameter.value is SerializableStapleAssetContainer container)
                {
                    Assert.That(container.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass).FullName));
                    Assert.That(container.fields, Has.Count.EqualTo(0));
                }
            }
        });

        asset.container.container = new();

        result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.Multiple(() =>
        {
            Assert.That(result.parameters.ContainsKey(nameof(SerializableAsset.container)), Is.True);
            Assert.That(result.parameters[nameof(SerializableAsset.container)].value, Is.TypeOf(typeof(SerializableStapleAssetContainer)));

            if (result.parameters.TryGetValue(nameof(SerializableAsset.container), out var parameter))
            {
                Assert.That(parameter.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass).FullName));
                Assert.That(parameter.value, Is.TypeOf<SerializableStapleAssetContainer>());

                if (parameter.value is SerializableStapleAssetContainer container)
                {
                    Assert.That(container.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass).FullName));
                    Assert.That(container.fields, Has.Count.EqualTo(1));

                    Assert.That(container.fields.ContainsKey(nameof(SerializableAsset.InnerClass.container)), Is.True);

                    Assert.That(container.fields[nameof(SerializableAsset.InnerClass.container)].value, Is.TypeOf<SerializableStapleAssetContainer>());

                    if (container.fields[nameof(SerializableAsset.InnerClass.container)].value is SerializableStapleAssetContainer innerContainer)
                    {
                        Assert.That(innerContainer.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass.InnerInnerClass).FullName));

                        Assert.That(innerContainer.fields.Count, Is.EqualTo(1));

                        Assert.That(innerContainer.fields.ContainsKey(nameof(SerializableAsset.InnerClass.InnerInnerClass.value)));

                        if (innerContainer.fields.TryGetValue(nameof(SerializableAsset.InnerClass.InnerInnerClass), out var innerParameter))
                        {
                            Assert.That(innerParameter.typeName, Is.EqualTo(typeof(int).FullName));

                            Assert.That(innerParameter.value, Is.TypeOf<int>());

                            if (innerParameter.value is int intValue)
                            {
                                Assert.That(intValue, Is.EqualTo(asset.container.container.value));
                            }
                        }
                    }
                }
            }
        });
    }

    [Test]
    public void TestPrimitiveSerialization()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new PrimitiveAsset
        {
            assets = new IGuidAsset[10],
            flags = [false, true, false],
            floatValues = [1, 2.5f, 1.5f],
            strings = ["a", "b", "c"],
            values = [1, 2, 3]
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.Null);

        Assert.That(result.typeName, Is.EqualTo(typeof(PrimitiveAsset).FullName));

        Assert.That(result.parameters.Count, Is.EqualTo(5));

        var deserialized = AssetSerialization.Deserialize(result, StapleSerializationMode.Binary);

        Assert.That(deserialized, Is.Not.Null);

        Assert.That(deserialized, Is.TypeOf<PrimitiveAsset>());

        if(deserialized is PrimitiveAsset newAsset)
        {
            Assert.That(newAsset.assets.Length, Is.EqualTo(10));
            Assert.That(newAsset.flags, Is.EqualTo(new bool[] { false, true, false }));
            Assert.That(newAsset.floatValues, Is.EqualTo(new float[] { 1, 2.5f, 1.5f }));
            Assert.That(newAsset.strings, Is.EqualTo(new string[] { "a", "b", "c" }));
            Assert.That(newAsset.values, Is.EqualTo(new int[] { 1, 2, 3 }));
        }
    }

    [Test]
    public void TestPrimitiveSerializationText()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new PrimitiveAsset
        {
            assets = new IGuidAsset[10],
            flags = [false, true, false],
            floatValues = [1, 2.5f, 1.5f],
            strings = ["a", "b", "c"],
            values = [1, 2, 3]
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Text);

        Assert.That(result, Is.Not.Null);

        Assert.That(result.typeName, Is.EqualTo(typeof(PrimitiveAsset).FullName));

        Assert.That(result.parameters.Count, Is.EqualTo(5));

        var deserialized = AssetSerialization.Deserialize(result, StapleSerializationMode.Text);

        Assert.That(deserialized, Is.Not.Null);

        Assert.That(deserialized, Is.TypeOf<PrimitiveAsset>());

        if (deserialized is PrimitiveAsset newAsset)
        {
            Assert.That(newAsset.assets.Length, Is.EqualTo(10));
            Assert.That(newAsset.flags, Is.EqualTo(new bool[] { false, true, false }));
            Assert.That(newAsset.floatValues, Is.EqualTo(new float[] { 1, 2.5f, 1.5f }));
            Assert.That(newAsset.strings, Is.EqualTo(new string[] { "a", "b", "c" }));
            Assert.That(newAsset.values, Is.EqualTo(new int[] { 1, 2, 3 }));
        }
    }

    [Test]
    public void TestPrimitiveListSerializationText()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new Base64ListAsset
        {
            values = [1, 2, 3]
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Text);

        Assert.That(result, Is.Not.Null);

        Assert.That(result.typeName, Is.EqualTo(typeof(Base64ListAsset).FullName));

        Assert.That(result.parameters.Count, Is.EqualTo(1));

        var deserialized = AssetSerialization.Deserialize(result, StapleSerializationMode.Text);

        Assert.That(deserialized, Is.Not.Null);

        Assert.That(deserialized, Is.TypeOf<Base64ListAsset>());

        if (deserialized is Base64ListAsset newAsset)
        {
            Assert.That(newAsset.values, Is.Not.Null);
            Assert.That(newAsset.values.Count, Is.EqualTo(3));
            Assert.That(newAsset.values[0], Is.EqualTo(1));
            Assert.That(newAsset.values[1], Is.EqualTo(2));
            Assert.That(newAsset.values[2], Is.EqualTo(3));
        }
    }

    [Test]
    public void TestPrimitiveListSerialization()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new Base64ListAsset
        {
            values = [1, 2, 3]
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.Null);

        Assert.That(result.typeName, Is.EqualTo(typeof(Base64ListAsset).FullName));

        Assert.That(result.parameters.Count, Is.EqualTo(1));

        var deserialized = AssetSerialization.Deserialize(result, StapleSerializationMode.Binary);

        Assert.That(deserialized, Is.Not.Null);

        Assert.That(deserialized, Is.TypeOf<Base64ListAsset>());

        if (deserialized is Base64ListAsset newAsset)
        {
            Assert.That(newAsset.values, Is.Not.Null);
            Assert.That(newAsset.values.Count, Is.EqualTo(3));
            Assert.That(newAsset.values[0], Is.EqualTo(1));
            Assert.That(newAsset.values[1], Is.EqualTo(2));
            Assert.That(newAsset.values[2], Is.EqualTo(3));
        }
    }

    [Test]
    public void TestSerializeField()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new SerializeFieldAsset();

        asset.SetSerializedField(2);
        asset.SetHiddenField(1);

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Binary);

        Assert.That(result, Is.Not.Null);

        Assert.That(result.typeName, Is.EqualTo(typeof(SerializeFieldAsset).FullName));

        Assert.That(result.parameters.Count, Is.EqualTo(1));

        var deserialized = AssetSerialization.Deserialize(result, StapleSerializationMode.Binary);

        Assert.That(deserialized, Is.Not.Null);

        Assert.That(deserialized, Is.TypeOf<SerializeFieldAsset>());

        if (deserialized is SerializeFieldAsset newAsset)
        {
            Assert.That(newAsset.GetHiddenField(), Is.EqualTo(0));
            Assert.That(newAsset.GetSerializedField(), Is.EqualTo(2));
        }
    }

    [Test]
    public void TestDeserializeJson()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new SimpleAsset
        {
            intValue = 2,
            stringValue = "different",
            enumValue = NewEnum.B,
        };

        asset.numbers.Clear();
        asset.numbers.Add(123);

        asset.pathAsset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path");

        var result = StapleSerializer.SerializeContainer(asset, StapleSerializationMode.Text);

        Assert.That(result, Is.Not.EqualTo(null));

        var jsonText = JsonSerializer.Serialize(result, StapleSerializerContainerSerializationContext.Default.StapleSerializerContainer);

        var deserialized = JsonSerializer.Deserialize(jsonText, StapleSerializerContainerSerializationContext.Default.StapleSerializerContainer);

        var newResult = StapleSerializer.DeserializeContainer(deserialized, StapleSerializationMode.Text);

        Assert.That(newResult, Is.Not.EqualTo(null));

        Assert.That(newResult, Is.TypeOf<SimpleAsset>());

        var newAsset = newResult as SimpleAsset;

        Assert.That(newAsset, Is.Not.EqualTo(null));

        Assert.That(newAsset.intValue, Is.EqualTo(asset.intValue));
        Assert.That(newAsset.stringValue, Is.EqualTo(asset.stringValue));
        Assert.That(newAsset.numbers, Is.EqualTo(asset.numbers));
        Assert.That(newAsset.pathAsset != null);
        Assert.That(newAsset.pathAsset.Guid.Guid, Is.EqualTo("valid path"));
        Assert.That(newAsset.enumValue, Is.EqualTo(asset.enumValue));
    }

    [Test]
    public void TestDeserializeJsonTypes()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new StapleTypesAsset()
        {
            asset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path"),
            b = 1,
            bo = true,
            c = Color.White,
            c32 = Color.White,
            d = 111,
            f = 123,
            i = 321,
            l = 123321,
            lm = new(123),
            ne = NewEnum.B,
            nfe = NewFlagEnum.B | NewFlagEnum.A,
            q = new(1, 2, 3, 4),
            r = new(1, 2, 3, 4),
            rf = new(1, 2, 3, 4),
            s = 123,
            sb = 123,
            ui = 123,
            ul = 123321,
            us = 123,
            v2 = new(1, 2),
            v2i = new(2, 3),
            v3 = new(1, 2, 3),
            v3i = new(4, 5, 6),
            v4 = new(1, 2, 3, 4),
            v4i = new(5, 6, 7, 8),
            str = "asdf",
        };

        var result = StapleSerializer.SerializeContainer(asset, StapleSerializationMode.Text);

        Assert.That(result, Is.Not.EqualTo(null));

        var jsonText = JsonSerializer.Serialize(result, StapleSerializerContainerSerializationContext.Default.StapleSerializerContainer);

        var deserialized = JsonSerializer.Deserialize(jsonText, StapleSerializerContainerSerializationContext.Default.StapleSerializerContainer);

        var newResult = StapleSerializer.DeserializeContainer(deserialized, StapleSerializationMode.Text);

        Assert.That(newResult, Is.Not.EqualTo(null));

        Assert.That(newResult, Is.TypeOf<StapleTypesAsset>());

        var newAsset = newResult as StapleTypesAsset;

        Assert.That(newAsset, Is.Not.EqualTo(null));

        Assert.That(newAsset.asset?.Guid.Guid, Is.EqualTo("valid path"));
        Assert.That(newAsset.b, Is.EqualTo((byte)1));
        Assert.That(newAsset.bo, Is.EqualTo(true));
        Assert.That(newAsset.c, Is.EqualTo(Color.White));
        Assert.That(newAsset.c32, Is.EqualTo(Color32.White));
        Assert.That(newAsset.d, Is.EqualTo(111.0));
        Assert.That(newAsset.f, Is.EqualTo(123.0f));
        Assert.That(newAsset.i, Is.EqualTo(321));
        Assert.That(newAsset.l, Is.EqualTo(123321));
        Assert.That(newAsset.lm, Is.EqualTo(new LayerMask(123)));
        Assert.That(newAsset.ne, Is.EqualTo(NewEnum.B));
        Assert.That(newAsset.nfe, Is.EqualTo(NewFlagEnum.A | NewFlagEnum.B));
        Assert.That(newAsset.q, Is.EqualTo(new Quaternion(1, 2, 3, 4)));
        Assert.That(newAsset.r, Is.EqualTo(new Rect(1, 2, 3, 4)));
        Assert.That(newAsset.rf, Is.EqualTo(new RectFloat(1, 2, 3, 4)));
        Assert.That(newAsset.s, Is.EqualTo((short)123));
        Assert.That(newAsset.sb, Is.EqualTo((sbyte)123));
        Assert.That(newAsset.ui, Is.EqualTo((uint)123));
        Assert.That(newAsset.ul, Is.EqualTo((ulong)123321));
        Assert.That(newAsset.us, Is.EqualTo((ushort)123));
        Assert.That(newAsset.v2, Is.EqualTo(new Vector2(1, 2)));
        Assert.That(newAsset.v2i, Is.EqualTo(new Vector2Int(2, 3)));
        Assert.That(newAsset.v3, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(newAsset.v3i, Is.EqualTo(new Vector3Int(4, 5, 6)));
        Assert.That(newAsset.v4, Is.EqualTo(new Vector4(1, 2, 3, 4)));
        Assert.That(newAsset.v4i, Is.EqualTo(new Vector4Int(5, 6, 7, 8)));
        Assert.That(newAsset.str, Is.EqualTo("asdf"));
    }

    [Test]
    public void TestDeserializeJsonToBinaryTypes()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        var asset = new StapleTypesAsset()
        {
            asset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path"),
            b = 1,
            bo = true,
            c = Color.White,
            c32 = Color.White,
            d = 111,
            f = 123,
            i = 321,
            l = 123321,
            lm = new(123),
            ne = NewEnum.B,
            nfe = NewFlagEnum.B | NewFlagEnum.A,
            q = new(1, 2, 3, 4),
            r = new(1, 2, 3, 4),
            rf = new(1, 2, 3, 4),
            s = 123,
            sb = 123,
            ui = 123,
            ul = 123321,
            us = 123,
            v2 = new(1, 2),
            v2i = new(2, 3),
            v3 = new(1, 2, 3),
            v3i = new(4, 5, 6),
            v4 = new(1, 2, 3, 4),
            v4i = new(5, 6, 7, 8),
            str = "asdf",
        };

        var result = AssetSerialization.Serialize(asset, StapleSerializationMode.Text);

        Assert.That(result, Is.Not.EqualTo(null));

        var newResult = AssetSerialization.Deserialize(result, StapleSerializationMode.Binary);

        Assert.That(newResult, Is.Not.EqualTo(null));

        Assert.That(newResult, Is.TypeOf<StapleTypesAsset>());

        var newAsset = newResult as StapleTypesAsset;

        Assert.That(newAsset, Is.Not.EqualTo(null));

        Assert.That(newAsset.asset?.Guid.Guid, Is.EqualTo("valid path"));
        Assert.That(newAsset.b, Is.EqualTo((byte)1));
        Assert.That(newAsset.bo, Is.EqualTo(true));
        Assert.That(newAsset.c, Is.EqualTo(Color.White));
        Assert.That(newAsset.c32, Is.EqualTo(Color32.White));
        Assert.That(newAsset.d, Is.EqualTo(111.0));
        Assert.That(newAsset.f, Is.EqualTo(123.0f));
        Assert.That(newAsset.i, Is.EqualTo(321));
        Assert.That(newAsset.l, Is.EqualTo(123321));
        Assert.That(newAsset.lm, Is.EqualTo(new LayerMask(123)));
        Assert.That(newAsset.ne, Is.EqualTo(NewEnum.B));
        Assert.That(newAsset.nfe, Is.EqualTo(NewFlagEnum.A | NewFlagEnum.B));
        Assert.That(newAsset.q, Is.EqualTo(new Quaternion(1, 2, 3, 4)));
        Assert.That(newAsset.r, Is.EqualTo(new Rect(1, 2, 3, 4)));
        Assert.That(newAsset.rf, Is.EqualTo(new RectFloat(1, 2, 3, 4)));
        Assert.That(newAsset.s, Is.EqualTo((short)123));
        Assert.That(newAsset.sb, Is.EqualTo((sbyte)123));
        Assert.That(newAsset.ui, Is.EqualTo((uint)123));
        Assert.That(newAsset.ul, Is.EqualTo((ulong)123321));
        Assert.That(newAsset.us, Is.EqualTo((ushort)123));
        Assert.That(newAsset.v2, Is.EqualTo(new Vector2(1, 2)));
        Assert.That(newAsset.v2i, Is.EqualTo(new Vector2Int(2, 3)));
        Assert.That(newAsset.v3, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(newAsset.v3i, Is.EqualTo(new Vector3Int(4, 5, 6)));
        Assert.That(newAsset.v4, Is.EqualTo(new Vector4(1, 2, 3, 4)));
        Assert.That(newAsset.v4i, Is.EqualTo(new Vector4Int(5, 6, 7, 8)));
        Assert.That(newAsset.str, Is.EqualTo("asdf"));
    }
}
