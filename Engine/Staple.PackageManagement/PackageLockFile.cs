using System;
using System.Collections.Generic;

namespace Staple.PackageManagement;

[Serializable]
public class PackageLockFile
{
    public enum Source
    {
        Builtin,
        Local,
        Git,
        Repository,
    }

    [Serializable]
    public class PackageState
    {
        public string version;
        public Source source;
        public Dictionary<string, string> dependencies = [];
        public string hash;
        public string url;

        public bool ShouldSerializehash() => source == Source.Git;

        public bool ShouldSerializeurl() => source == Source.Repository;
    }

    public Dictionary<string, PackageState> dependencies = [];
}
