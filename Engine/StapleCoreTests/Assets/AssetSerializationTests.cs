using Staple;
using Staple.Internal;

namespace CoreTests
{
    internal class AssetSerializationTests
    {
        internal enum NewEnum
        {
            A,
            B
        }

        internal class SimplePathAsset : IGuidAsset
        {
            internal string guid = "a/b/c";

            public string Guid
            {
                get => guid;

                set => guid = value;
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
            public IGuidAsset pathAsset = new SimplePathAsset();
            public NewEnum enumValue = NewEnum.A;

            internal int notSerialized = 0;

            [NonSerialized]
            public int notSerialized2 = 0;
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

        [Test]
        public void TestSerialize()
        {
            TypeCacheRegistration.RegisterAll();

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
            TypeCacheRegistration.RegisterAll();

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
            TypeCacheRegistration.RegisterAll();

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
            TypeCacheRegistration.RegisterAll();

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
            TypeCacheRegistration.RegisterAll();

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
    }
}
