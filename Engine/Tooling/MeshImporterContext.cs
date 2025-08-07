using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Tooling;

public class MeshImporterContext
{
    public MeshAssetMetadata metadata;
    public string meshFileName;
    public string inputPath;
    public SerializableShader standardShader;
    public Func<string, bool> ShaderHasParameter;
    public Lock materialLock;
    public Dictionary<string, string> processedTextures;
}
