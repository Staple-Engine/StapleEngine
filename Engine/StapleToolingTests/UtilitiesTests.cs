using Staple.Tooling;

namespace StapleToolingTests
{
    internal class UtilitiesTests
    {
        [Test]
        public void TestCombinations()
        {
            List<string> items = [
                "a",
                "b",
                "c"
            ];

            var combinations = Utilities.Combinations(items);

            Assert.That(combinations.Count, Is.EqualTo(8));
        }
    }
}
