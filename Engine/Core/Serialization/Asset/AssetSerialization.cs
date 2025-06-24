using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Handles serialization for Staple Assets
/// </summary>
internal static partial class AssetSerialization
{
    private static readonly Regex cachePathRegex = CachePathRegex();
    private static readonly Regex assetPathRegex = AssetPathRegex();

    /// <summary>
    /// File extension for Scenes
    /// </summary>
    public static readonly string SceneExtension = "scene";

    /// <summary>
    /// File extension for Materials
    /// </summary>
    public static readonly string MaterialExtension = "material";

    /// <summary>
    /// File extension for Shaders
    /// </summary>
    public static readonly string ShaderExtension = "shader";

    /// <summary>
    /// File extension for Compute Shaders
    /// </summary>
    public static readonly string ComputeShaderExtension = "computeshader";

    /// <summary>
    /// File extension for Assets
    /// </summary>
    public static readonly string AssetExtension = "asset";

    /// <summary>
    /// File extensions for Prefabs
    /// </summary>
    public static readonly string PrefabExtension = "prefab";

    /// <summary>
    /// File extensions for Assembly Definitions
    /// </summary>
    public static readonly string AssemblyDefinitionExtension = "asmdef";

    /// <summary>
    /// GUID for the standard shader
    /// </summary>
    public static readonly string StandardShaderGUID = "1ca9a72c-161e-44db-ad76-bf0ae432f78b";

    /// <summary>
    /// All supported texture extensions
    /// </summary>
    public static readonly string[] TextureExtensions =
    [
        "bmp",
        "dds",
        "gif",
        "jpg",
        "jpeg",
        "hdr",
        "ktx",
        "png",
        "psd",
        "pvr",
        "tga"
    ];

    /// <summary>
    /// All texture extensions we can resize in code
    /// </summary>
    public static readonly string[] ResizableTextureExtensions =
    [
        "jpg",
        "jpeg",
        "png",
        "tga",
        "bmp",
        "gif",
        "hdr",
    ];

    /// <summary>
    /// All 3D model (mesh) extensions
    /// </summary>
    public static readonly string[] MeshExtensions =
    [
        "3ds",
        "ase",
        "bvh",
        "dae",
        "fbx",
        "glb",
        "gltf",
        "ms3d",
        "obj",
        "ply",
        "stl",
    ];

    /// <summary>
    /// All audio file extensions
    /// </summary>
    public static readonly string[] AudioExtensions =
    [
        "mp3",
        "ogg",
        "wav",
    ];

    /// <summary>
    /// All font file extensions
    /// </summary>
    public static readonly string[] FontExtensions =
    [
        "ttf",
    ];

    /// <summary>
    /// All plugin file extensions
    /// </summary>
    public static readonly string[] PluginExtensions =
    [
        "dll",
        "dylib",
        "so",
    ];

    /// <summary>
    /// All plugin folder extension suffixes
    /// </summary>
    public static readonly string[] PluginFolderSuffixes =
    [
        "androidlib",
        "bundle",
        "framework",
    ];

    /// <summary>
    /// Gets the asset path for an asset from a cache path
    /// </summary>
    /// <param name="path">The asset path</param>
    /// <returns>The estimatd valid path</returns>
    public static string GetAssetPathFromCache(string path)
    {
        if(path == null)
        {
            return null;
        }

        var matches = cachePathRegex.Matches(path);

        if (matches.Count > 0)
        {
            return path.Substring(matches[0].Value.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        matches = assetPathRegex.Matches(path);

        if (matches.Count > 0)
        {
            return path.Substring(matches[0].Value.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        return path;
    }

    /// <summary>
    /// Attempts to create an asset by Guid
    /// </summary>
    /// <param name="type">The asset type</param>
    /// <param name="guid">The guid to use</param>
    /// <returns>The asset, or null</returns>
    public static object GetGuidAsset(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        Type type, string guid)
    {
        if (guid == null)
        {
            return null;
        }

        var methods = type.GetMethods();

        foreach (var method in methods)
        {
            if (method.IsStatic && method.IsPublic && method.Name == "Create")
            {
                var parameters = method.GetParameters();

                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(null, [ guid ]);

                    if (result == null || (result.GetType() != type && result.GetType().GetInterface(type.FullName) == null))
                    {
                        break;
                    }

                    return result;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        return null;
    }

    [GeneratedRegex("(.*?)(\\\\|\\/)Cache(\\\\|\\/)Staging(\\\\|\\/)(.*?)(\\\\|\\/)")]
    private static partial Regex CachePathRegex();

    [GeneratedRegex("(.*?)(\\\\|\\/)Assets(\\\\|\\/)(.*?)")]
    private static partial Regex AssetPathRegex();

    /// <summary>
    /// Attempts to serialize a Staple Asset into a SerializableStapleAsset
    /// </summary>
    /// <param name="instance">The object's instance. The object must implement IStapleAsset</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The SerializableStapleAsset, or null</returns>
    public static SerializableStapleAsset Serialize(object instance, StapleSerializationMode mode)
    {
        if(instance == null || instance.GetType().GetInterface(typeof(IStapleAsset).FullName) == null)
        {
            return default;
        }

        return StapleSerializer.SerializeAssetObject(instance, mode);
    }

    /// <summary>
    /// Deserializes an asset into an instance
    /// </summary>
    /// <param name="asset">The asset data</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The asset, or null</returns>
    public static IStapleAsset Deserialize(SerializableStapleAsset asset, StapleSerializationMode mode)
    {
        if(asset == null)
        {
            return null;
        }

        var instance = StapleSerializer.DeserializeAssetObject(asset, mode);

        if(instance is IStapleAsset stapleAsset)
        {
            if(stapleAsset is IGuidAsset guidAsset)
            {
                guidAsset.Guid.Guid = asset.guid;
            }

            return stapleAsset;
        }

        return null;
    }
}
