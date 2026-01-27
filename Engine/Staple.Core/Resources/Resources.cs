using Staple.Internal;
using System;

namespace Staple;

public static class Resources
{
    /// <summary>
    /// Attempts to load a resource from a path or guid
    /// </summary>
    /// <param name="pathOrGuid">The path or guid</param>
    /// <returns>The object, or null</returns>
    public static object Load(string pathOrGuid)
    {
        if(pathOrGuid == null)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(pathOrGuid) ?? pathOrGuid;

        if(guid == null)
        {
            return null;
        }

        var assetType = AssetDatabase.GetAssetType(guid);

        if(assetType == null)
        {
            return null;
        }

        var nativeType = TypeCache.GetType(assetType);

        if(nativeType == null)
        {
            return null;
        }

        return nativeType switch
        {
            Type t when t == typeof(Scene) => null, //Scenes are not loadable this way
            Type t when t == typeof(Material) => ResourceManager.instance.LoadMaterial(guid),
            Type t when t == typeof(Shader) => ResourceManager.instance.LoadShader(guid),
            Type t when t == typeof(ComputeShader) => ResourceManager.instance.LoadComputeShader(guid),
            Type t when t == typeof(Texture) => ResourceManager.instance.LoadTexture(guid),
            Type t when t == typeof(AudioClip) => ResourceManager.instance.LoadAudioClip(guid),
            Type t when t == typeof(Mesh) => ResourceManager.instance.LoadMesh(guid),
            Type t when t == typeof(MeshAsset) => ResourceManager.instance.LoadMeshAsset(guid),
            Type t when t == typeof(FontAsset) => ResourceManager.instance.LoadFont(guid),
            Type t when t == typeof(TextAsset) => ResourceManager.instance.LoadTextAsset(guid),
            Type t when t.GetInterface(typeof(IStapleAsset).FullName) != null => ResourceManager.instance.LoadAsset(guid),
            _ => null,
        };
    }

    /// <summary>
    /// Attempts to load an asset from a path or guid
    /// </summary>
    /// <typeparam name="T">The asset type. Must be a class.</typeparam>
    /// <param name="pathOrGuid">The path or guid to the asset</param>
    /// <returns>The asset, or null</returns>
    public static T Load<T>(string pathOrGuid) where T: class
    {
        var result = Load(pathOrGuid);

        if(result == null || !result.GetType().IsAssignableTo(typeof(T)))
        {
            return null;
        }

        return (T)result;
    }
}
