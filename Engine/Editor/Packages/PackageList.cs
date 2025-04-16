using System;
using System.Collections.Generic;

namespace Staple.Editor;

[Serializable]
internal class PackageList
{
    public Dictionary<string, string> dependencies = [];
}
