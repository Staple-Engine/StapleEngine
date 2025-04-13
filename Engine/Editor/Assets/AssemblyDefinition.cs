using System;
using System.Collections.Generic;

namespace Staple.Editor;

/// <summary>
/// Allows creating an assembly definition asset
/// </summary>
[Serializable]
public class AssemblyDefinition
{
    public List<string> referencedAssemblies = [];
    public string version;
    public List<AppPlatform> includedPlatforms = [];
    public List<AppPlatform> excludedPlatforms = [];
    public bool allowUnsafeCode;
    public bool autoReferenced = true;
}
