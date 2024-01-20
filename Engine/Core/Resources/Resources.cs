using Staple.Internal;
using System;

namespace Staple
{
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

            switch(nativeType)
            {
                case Type t when t == typeof(Scene):

                    //Scenes are not loadable this way
                    return null;

                case Type t when t == typeof(Material):

                    return ResourceManager.instance.LoadMaterial(guid);

                case Type t when t == typeof(Shader):

                    return ResourceManager.instance.LoadShader(guid);

                case Type t when t == typeof(Texture):

                    return ResourceManager.instance.LoadTexture(guid);

                case Type t when t == typeof(AudioClip):

                    return ResourceManager.instance.LoadAudioClip(guid);

                case Type t when t == typeof(MeshAsset):

                    return ResourceManager.instance.LoadMesh(guid);

                case Type t when t.GetInterface(typeof(IStapleAsset).FullName) != null:

                    return ResourceManager.instance.LoadAsset(guid);

                default:

                    return null;
            }
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

            if(result == null || result.GetType() != typeof(T))
            {
                return null;
            }

            return (T)result;
        }
    }
}
