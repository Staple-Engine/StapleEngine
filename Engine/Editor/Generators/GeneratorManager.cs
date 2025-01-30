using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

internal static class GeneratorManager
{
    internal class GeneratorInfo(IGenerator instance)
    {
        public readonly IGenerator instance = instance;
        public readonly string extension = instance.Extension;
        public readonly bool isText = instance.IsText;
    }

    private static Type[] generatorTypes = [];

    internal static Dictionary<string, GeneratorInfo> generators = [];

    internal static void UpdateGenerators()
    {
        generatorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsAssignableTo(typeof(IGenerator)) && x != typeof(IGenerator))
                .Distinct()
                .ToArray();

        generators.Clear();

        foreach (var type in generatorTypes)
        {
            if(ObjectCreation.CreateObject(type) is IGenerator generator)
            {
                generators.AddOrSetKey(type.FullName, new(generator));
            }
        }
    }

    public static bool TryGetGenerator(string guid, out IGenerator generator)
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
                        if (method.Name == nameof(IGenerator.Load))
                        {
                            var parameters = method.GetParameters();

                            if (parameters.Length != 2 ||
                                parameters[0].ParameterType != typeof(string) ||
                                parameters[1].ParameterType.Name != $"{nameof(IGenerator)}&" ||
                                parameters[1].IsOut == false)
                            {
                                continue;
                            }

                            object[] args = [guid, generator];

                            method.Invoke(null, args);

                            if (args[1] is IGenerator g)
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