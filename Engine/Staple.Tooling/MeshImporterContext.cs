using Staple.Internal;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Tooling;

public class MeshImporterContext
{
    public delegate bool ShaderHasParameterCallback(string name);
    public delegate string ResolveTexturePathCallback(string path, string meshFileName);

    public MeshAssetMetadata metadata;
    public string meshFileName;
    public string inputPath;
    public SerializableShader standardShader;
    public ShaderHasParameterCallback shaderHasParameter;
    public Lock materialLock;
    public Dictionary<string, string> processedTextures;
    public ResolveTexturePathCallback resolveTexturePath;
}
