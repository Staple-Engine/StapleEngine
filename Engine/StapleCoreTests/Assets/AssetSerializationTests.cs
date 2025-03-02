﻿using Staple;
using Staple.Internal;

namespace CoreTests
{
    internal class AssetSerializationTests
    {
        public enum NewEnum
        {
            A,
            B
        }

        internal class SimplePathAsset : IGuidAsset
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

            public static object Create(string guid)
            {
                return new SimplePathAsset()
                {
                    guid = guid,
                };
            }
        }

        internal class SimpleAsset : IStapleAsset
        {
            public int intValue = 1;
            public string stringValue = "test";
            public List<int> numbers = new(new int[] { 1, 2, 3 });

            public IGuidAsset pathAsset = new SimplePathAsset()
            {
                Guid = "a/b/c",
            };

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

            var result = AssetSerialization.Serialize(asset, false);

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
                Assert.That(result.parameters[nameof(SimpleAsset.pathAsset)].value, Is.EqualTo(asset.pathAsset.Guid));
                Assert.That(result.parameters[nameof(SimpleAsset.enumValue)].value, Is.EqualTo(asset.enumValue));
            });

            asset.pathAsset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path");

            result = AssetSerialization.Serialize(asset, false);

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

            var result = AssetSerialization.Serialize(asset, false);

            Assert.That(result, Is.Not.EqualTo(null));

            var newResult = AssetSerialization.Deserialize(result);

            Assert.That(newResult, Is.Not.EqualTo(null));

            Assert.That(newResult, Is.TypeOf<SimpleAsset>());

            var newAsset = newResult as SimpleAsset;

            Assert.That(newAsset, Is.Not.EqualTo(null));

            Assert.That(newAsset.intValue, Is.EqualTo(asset.intValue));
            Assert.That(newAsset.stringValue, Is.EqualTo(asset.stringValue));
            Assert.That(newAsset.numbers, Is.EqualTo(asset.numbers));
            Assert.That(newAsset.pathAsset != null);
            Assert.That(newAsset.pathAsset.Guid, Is.EqualTo("valid path"));
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

            var result = AssetSerialization.Serialize(asset, false);

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
                        Assert.That(container.parameters, Has.Count.EqualTo(0));
                    }
                }
            });

            asset.container.container = new();

            result = AssetSerialization.Serialize(asset, false);

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
                        Assert.That(container.parameters, Has.Count.EqualTo(1));

                        Assert.That(container.parameters.ContainsKey(nameof(SerializableAsset.InnerClass.container)), Is.True);

                        Assert.That(container.parameters[nameof(SerializableAsset.InnerClass.container)].value, Is.TypeOf<SerializableStapleAssetContainer>());

                        if (container.parameters[nameof(SerializableAsset.InnerClass.container)].value is SerializableStapleAssetContainer innerContainer)
                        {
                            Assert.That(innerContainer.typeName, Is.EqualTo(typeof(SerializableAsset.InnerClass.InnerInnerClass).FullName));

                            Assert.That(innerContainer.parameters.Count, Is.EqualTo(1));

                            Assert.That(innerContainer.parameters.ContainsKey(nameof(SerializableAsset.InnerClass.InnerInnerClass.value)));

                            if (innerContainer.parameters.TryGetValue(nameof(SerializableAsset.InnerClass.InnerInnerClass), out var innerParameter))
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

            var result = AssetSerialization.Serialize(asset, false);

            Assert.That(result, Is.Not.Null);

            Assert.That(result.typeName, Is.EqualTo(typeof(PrimitiveAsset).FullName));

            Assert.That(result.parameters.Count, Is.EqualTo(5));

            var deserialized = AssetSerialization.Deserialize(result);

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

            var result = AssetSerialization.Serialize(asset, true);

            Assert.That(result, Is.Not.Null);

            Assert.That(result.typeName, Is.EqualTo(typeof(PrimitiveAsset).FullName));

            Assert.That(result.parameters.Count, Is.EqualTo(5));

            var deserialized = AssetSerialization.Deserialize(result);

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

            var result = AssetSerialization.Serialize(asset, true);

            Assert.That(result, Is.Not.Null);

            Assert.That(result.typeName, Is.EqualTo(typeof(Base64ListAsset).FullName));

            Assert.That(result.parameters.Count, Is.EqualTo(1));

            var deserialized = AssetSerialization.Deserialize(result);

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

            var result = AssetSerialization.Serialize(asset, false);

            Assert.That(result, Is.Not.Null);

            Assert.That(result.typeName, Is.EqualTo(typeof(Base64ListAsset).FullName));

            Assert.That(result.parameters.Count, Is.EqualTo(1));

            var deserialized = AssetSerialization.Deserialize(result);

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

            var result = AssetSerialization.Serialize(asset, false);

            Assert.That(result, Is.Not.Null);

            Assert.That(result.typeName, Is.EqualTo(typeof(SerializeFieldAsset).FullName));

            Assert.That(result.parameters.Count, Is.EqualTo(1));

            var deserialized = AssetSerialization.Deserialize(result);

            Assert.That(deserialized, Is.Not.Null);

            Assert.That(deserialized, Is.TypeOf<SerializeFieldAsset>());

            if (deserialized is SerializeFieldAsset newAsset)
            {
                Assert.That(newAsset.GetHiddenField(), Is.EqualTo(0));
                Assert.That(newAsset.GetSerializedField(), Is.EqualTo(2));
            }
        }
    }
}
