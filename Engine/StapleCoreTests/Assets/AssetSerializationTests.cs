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

        internal class SimplePathAsset : IPathAsset
        {
            internal string path = "a/b/c";

            public string Path => path;

            public static object Create(string path)
            {
                return new SimplePathAsset()
                {
                    path = path,
                };
            }
        }

        internal class SimpleAsset : IStapleAsset
        {
            public int intValue = 1;
            public string stringValue = "test";
            public List<int> numbers = new(new int[] { 1, 2, 3 });
            public IPathAsset pathAsset = new SimplePathAsset();
            public NewEnum enumValue = NewEnum.A;

            public void OnAfterDeserialize()
            {
            }

            public void OnAfterSerialize()
            {
            }

            public void OnBeforeDeserialize()
            {
            }

            public void OnBeforeSerialize()
            {
            }
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

            var result = AssetSerialization.Serialize(asset);

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
                Assert.That(result.parameters[nameof(SimpleAsset.pathAsset)].value, Is.EqualTo(asset.pathAsset.Path));
                Assert.That(result.parameters[nameof(SimpleAsset.enumValue)].value, Is.EqualTo(asset.enumValue));
            });

            asset.pathAsset = (SimplePathAsset)SimplePathAsset.Create("/abc/Cache/Staging/Windows/valid path");

            result = AssetSerialization.Serialize(asset);

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

            var result = AssetSerialization.Serialize(asset);

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
            Assert.That(newAsset.pathAsset.Path, Is.EqualTo("valid path"));
            Assert.That(newAsset.enumValue, Is.EqualTo(asset.enumValue));
        }
    }
}
