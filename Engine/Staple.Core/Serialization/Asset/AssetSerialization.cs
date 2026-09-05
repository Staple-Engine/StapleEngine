using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Handles serialization for Staple Assets
/// </summary>
public static partial class AssetSerialization
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
    /// Path to the standard material
    /// </summary>
    public static readonly string StandardMaterialPath = $"Hidden/Materials/Standard.{MaterialExtension}";

    /// <summary>
    /// Path to the standard shader
    /// </summary>
    public static readonly string StandardShaderPath = $"Hidden/Shaders/Default/Standard.{ShaderExtension}";

    /// <summary>
    /// All supported texture extensions
    /// </summary>
    public static readonly string[] TextureExtensions =
    [
        "bmp",
        "dds",
        "exr",
        "gif",
        "jpg",
        "jpeg",
        "hdr",
        "ktx",
        "png",
        "psd",
        "pvr",
        "tga",
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
    /// All static (can't animate) 3D model (mesh) extensions
    /// </summary>
    public static readonly string[] StaticMeshExtensions =
    [
        "3ds",
        "ase",
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
    /// All text file extensions
    /// </summary>
    public static readonly string[] TextExtensions =
    [
        "txt",
        "log",
        "json",
        "lua",
        "xml"
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
    
    /// <summary>
    /// Gets the block size for a specific texture format. A texture should always be sized by a multiple of this block size
    /// </summary>
    /// <param name="format">The format</param>
    /// <returns>The block size</returns>
    public static int GetTextureBlockSize(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8 or TextureFormat.R8U or TextureFormat.R8I or TextureFormat.R8S or TextureFormat.A8 => 1,

            TextureFormat.B5G6R5 or TextureFormat.BGRA4 or TextureFormat.BGR5A1 or TextureFormat.R16F or TextureFormat.RG8 or 
            TextureFormat.RG8S or TextureFormat.RG8U or TextureFormat.RG8I or TextureFormat.R16 or TextureFormat.R16S or TextureFormat.R16U or
            TextureFormat.R16I or TextureFormat.D16 => 2,

            TextureFormat.RGBA8 or TextureFormat.BGRA8 or TextureFormat.R32F or TextureFormat.RG16F or TextureFormat.RG11B10F or 
            TextureFormat.RGBA8S or TextureFormat.RGBA8U or TextureFormat.RGBA8I or TextureFormat.RGB10A2 or TextureFormat.RG16U or 
            TextureFormat.RG16I or TextureFormat.RG16 or TextureFormat.RG16S or TextureFormat.D24 or TextureFormat.D32F or TextureFormat.R32U or
            TextureFormat.R32I or TextureFormat.D24S8 => 4,

            TextureFormat.D32S8 => 5,

            TextureFormat.BC1 or TextureFormat.BC4 or TextureFormat.RGBA16F or TextureFormat.RGBA16 or TextureFormat.RGBA16S or
            TextureFormat.RGBA16U or TextureFormat.RGBA16I or TextureFormat.RG32F or TextureFormat.RG32U or TextureFormat.RG32I => 8,

            TextureFormat.BC2 or TextureFormat.BC3 or TextureFormat.BC5 or TextureFormat.BC6H or TextureFormat.BC7 or TextureFormat.RGBA32F or 
            TextureFormat.RGBA32I or TextureFormat.RGBA32U or TextureFormat.ASTC4x4 or TextureFormat.ASTC5x4 or TextureFormat.ASTC5x5 or
            TextureFormat.ASTC6x5 or TextureFormat.ASTC6x6 or TextureFormat.ASTC8x5 or TextureFormat.ASTC8x6 or TextureFormat.ASTC8x8 or
            TextureFormat.ASTC10x5 or TextureFormat.ASTC10x6 or TextureFormat.ASTC10x8 or TextureFormat.ASTC10x10 or TextureFormat.ASTC12x10 or
            TextureFormat.ASTC12x12 or TextureFormat.ASTC4x4F or TextureFormat.ASTC5x4F or TextureFormat.ASTC5x5F or TextureFormat.ASTC6x5F or
            TextureFormat.ASTC6x6F or TextureFormat.ASTC8x5F or TextureFormat.ASTC8x6F or TextureFormat.ASTC8x8F or TextureFormat.ASTC10x5F or
            TextureFormat.ASTC10x6F or TextureFormat.ASTC10x8F or TextureFormat.ASTC10x10F or TextureFormat.ASTC12x10F or
            TextureFormat.ASTC12x12F => 16,

            _ => -1,
        };
    }

    /// <summary>
    /// Gets the block size for a specific texture format. A texture should always be sized by a multiple of this block size
    /// </summary>
    /// <param name="format">The format</param>
    /// <returns>The block size, or -1</returns>
    public static int GetTextureBlockSize(TextureMetadataFormat format)
    {
        return format switch
        {
            TextureMetadataFormat.R8 or TextureMetadataFormat.R8U or TextureMetadataFormat.R8I or TextureMetadataFormat.R8S => 1,

            TextureMetadataFormat.B5G6R5 or TextureMetadataFormat.BGRA4 or TextureMetadataFormat.BGR5A1 or TextureMetadataFormat.R16F or 
            TextureMetadataFormat.RG8 or TextureMetadataFormat.RG8S or TextureMetadataFormat.RG8U or TextureMetadataFormat.RG8I or
            TextureMetadataFormat.R16 or TextureMetadataFormat.R16S or TextureMetadataFormat.R16U or TextureMetadataFormat.R16I => 2,

            TextureMetadataFormat.RGBA8 or TextureMetadataFormat.BGRA8 or TextureMetadataFormat.RG16F or TextureMetadataFormat.RGBA8S or
            TextureMetadataFormat.RGBA8U or TextureMetadataFormat.RGBA8I or TextureMetadataFormat.RG16U or TextureMetadataFormat.RG16I or
            TextureMetadataFormat.RG16 or TextureMetadataFormat.RG16S => 4,

            TextureMetadataFormat.BC1 or TextureMetadataFormat.BC4 or TextureMetadataFormat.RGBA16F or TextureMetadataFormat.RGBA16 or 
            TextureMetadataFormat.RGBA16S or TextureMetadataFormat.RGBA16U or TextureMetadataFormat.RGBA16I => 8,

            TextureMetadataFormat.BC2 or TextureMetadataFormat.BC3 or TextureMetadataFormat.BC5 or TextureMetadataFormat.BC6H or 
            TextureMetadataFormat.BC7 or TextureMetadataFormat.ASTC4x4 or TextureMetadataFormat.ASTC5x4 or TextureMetadataFormat.ASTC5x5 or 
            TextureMetadataFormat.ASTC6x5 or TextureMetadataFormat.ASTC6x6 or TextureMetadataFormat.ASTC8x5 or TextureMetadataFormat.ASTC8x6 or
            TextureMetadataFormat.ASTC8x8 or TextureMetadataFormat.ASTC10x5 or TextureMetadataFormat.ASTC10x6 or TextureMetadataFormat.ASTC10x8 or 
            TextureMetadataFormat.ASTC10x10 or TextureMetadataFormat.ASTC12x10 or TextureMetadataFormat.ASTC12x12 or
            TextureMetadataFormat.ASTC4x4F or TextureMetadataFormat.ASTC5x4F or TextureMetadataFormat.ASTC5x5F or TextureMetadataFormat.ASTC6x5F or
            TextureMetadataFormat.ASTC6x6F or TextureMetadataFormat.ASTC8x5F or TextureMetadataFormat.ASTC8x6F or TextureMetadataFormat.ASTC8x8F or
            TextureMetadataFormat.ASTC10x5F or TextureMetadataFormat.ASTC10x6F or TextureMetadataFormat.ASTC10x8F or
            TextureMetadataFormat.ASTC10x10F or TextureMetadataFormat.ASTC12x10F or TextureMetadataFormat.ASTC12x12F => 16,

            _ => -1,
        };
    }
}
