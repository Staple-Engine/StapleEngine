using System;
using System.Collections.Generic;

namespace Staple.Editor;

[Serializable]
internal class Package
{
    [Serializable]
    public class Dependency
    {
        public string name;
        public string version;
    }

    public string name;
    public string version;
    public string description;
    public string displayName;
    public string minStapleVersion;
    public string author;
    public List<Dependency> dependencies = [];
    public string keywords;
    public string license;
}
