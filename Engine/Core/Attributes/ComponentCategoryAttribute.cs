using System;

namespace Staple
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class ComponentCategoryAttribute : Attribute
    {
        public string categoryName;

        public ComponentCategoryAttribute(string categoryName)
        {
            this.categoryName = categoryName;
        }
    }
}
