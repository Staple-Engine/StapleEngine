using Staple.Internal;

namespace CoreTests;

internal class MiscTests
{
    [Test]
    public void TestGuidGeneration()
    {
        var guids = new List<Guid>();

        for (var i = 0; i < 1000; i++)
        {
            guids.Add(GuidGenerator.Generate());
        }

        for (var i = 0; i < guids.Count; i++)
        {
            for (var j = 0; j < guids.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Assert.That(guids[j], Is.Not.EqualTo(guids[i]));
            }
        }
    }
}
