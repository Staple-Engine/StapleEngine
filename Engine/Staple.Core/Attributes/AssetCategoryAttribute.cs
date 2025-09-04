using System;

namespace Staple;

/// <summary>
/// Sets a category for a staple asset. Can use `/` in the category name to make sub-categories.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AssetCategoryAttribute : Attribute
{
    public string categoryName;

    public AssetCategoryAttribute(string categoryName)
    {
        this.categoryName = categoryName;
    }
}
