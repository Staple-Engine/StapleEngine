using System;

namespace Staple;

/// <summary>
/// Sets a category for a component. Can use `/` in the category name to make sub-categories.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class ComponentCategoryAttribute : Attribute
{
    public string categoryName;

    public ComponentCategoryAttribute(string categoryName)
    {
        this.categoryName = categoryName;
    }
}
