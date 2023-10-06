using Staple.Internal;

namespace TestGame
{
    public class TestAsset : IStapleAsset
    {
        public string name;
        public int intValue;
        public bool boolValue;
        public string stringValue;
        public float floatValue;
        public double doubleValue;

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
}