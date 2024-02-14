using System;

namespace Staple;

/// <summary>
/// Sets a category for a staple asset
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
