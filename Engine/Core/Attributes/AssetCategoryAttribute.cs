using System;

namespace Staple
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AssetCategoryAttribute : Attribute
    {
        public string categoryName;

        public AssetCategoryAttribute(string categoryName)
        {
            this.categoryName = categoryName;
        }
    }
}
