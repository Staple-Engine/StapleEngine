using System;
using System.Collections.Generic;

namespace Staple.PackageManagement;

[Serializable]
public class PackageList
{
    public Dictionary<string, string> dependencies = [];
}
