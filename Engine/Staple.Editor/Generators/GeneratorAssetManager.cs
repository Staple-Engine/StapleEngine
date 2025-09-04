using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

internal static class GeneratorAssetManager
{
    internal class GeneratorAssetInfo(IGeneratorAsset instance)
    {
        public readonly IGeneratorAsset instance = instance;
        public readonly string extension = instance.Extension;
        public readonly bool isText = instance.IsText;
    }

    private static Type[] generatorTypes = [];

    internal static Dictionary<string, GeneratorAssetInfo> generators = [];

    internal static void UpdateGeneratorAssets()
    {
        generatorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsAssignableTo(typeof(IGeneratorAsset)) && x != typeof(IGeneratorAsset))
                .Distinct()
                .ToArray();

        generators.Clear();

        foreach (var type in generatorTypes)
        {
            if(ObjectCreation.CreateObject(type) is IGeneratorAsset generator)
            {
                generators.AddOrSetKey(type.FullName, new(generator));
            }
        }
    }

    public static bool TryGetGeneratorAsset(string guid, out IGeneratorAsset generator)
    {
        generator = default;

        var path = AssetDatabase.GetAssetPath(guid);

        if(path == null)
        {
            return false;
        }

        foreach(var pair in generators)
        {
            if(path.EndsWith(pair.Value.extension))
            {
                try
                {
                    var methods = pair.Value.instance.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public);

                    foreach (var method in methods)
                    {
                        if (method.Name == nameof(IGeneratorAsset.Load))
                        {
                            var parameters = method.GetParameters();

                            if (parameters.Length != 2 ||
                                parameters[0].ParameterType != typeof(string) ||
                                parameters[1].ParameterType.Name != $"{nameof(IGeneratorAsset)}&" ||
                                parameters[1].IsOut == false)
                            {
                                continue;
                            }

                            object[] args = [guid, generator];

                            method.Invoke(null, args);

                            if (args[1] is IGeneratorAsset g)
                            {
                                generator = g;

                                return true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"[Generator] Failed to attempt to load a generator {pair.Value.GetType().FullName} for asset {guid} (at {path}): {e}");
                }
            }
        }

        return false;
    }
}