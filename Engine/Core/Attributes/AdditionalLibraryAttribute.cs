using System;

namespace Staple;

/// <summary>
/// Marks a class with requiring an additional native library to work.
/// Usually used with Android.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AdditionalLibraryAttribute : Attribute
{
    /// <summary>
    /// The platform this additional library is for
    /// </summary>
    public AppPlatform platform;

    /// <summary>
    /// The path for the additional library
    /// </summary>
    public string path;

    public AdditionalLibraryAttribute(AppPlatform platform, string path)
    {
        this.platform = platform;
        this.path = path;
    }
}
