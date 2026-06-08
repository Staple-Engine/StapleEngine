using Staple.ProjectManagement;
using System;
using System.Collections.Generic;

namespace Staple.Editor;

[CustomEditor(typeof(AssemblyDefinition))]
internal class AssemblyDefinitionEditor : AssetEditor
{
    private AppPlatform[] platformList = [];
    private AssemblyDefinition[] referencedAssemblies = [];
    private PluginAsset[] referencedPlugins = [];
    private string[][] ignoredGuids = [];

    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if(target is not AssemblyDefinition asmDef)
        {
            return true;
        }

        switch(name)
        {
            case nameof(AssemblyDefinition.platforms):
            case nameof(AssemblyDefinition.excludedPlatforms):

                {
                    if(asmDef.anyPlatform)
                    {
                        if (name == nameof(AssemblyDefinition.platforms))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if(name == nameof(AssemblyDefinition.excludedPlatforms))
                        {
                            return true;
                        }
                    }

                    if (getter() is List<AppPlatform> list)
                    {
                        EditorGUI.Label(name.ExpandCamelCaseName());

                        for (var i = 0; i < platformList.Length; i++)
                        {
                            var value = platformList[i];

                            var add = EditorGUI.Toggle(value.ToString(), $"{name}.Item{i}", list.Contains(value) ||
                                (list.Count == 0 && name == nameof(AssemblyDefinition.platforms)));

                            if (add)
                            {
                                if (!list.Contains(value))
                                {
                                    list.Add(value);
                                }
                            }
                            else
                            {
                                list.Remove(value);
                            }
                        }
                    }
                }

                return true;

            case nameof(AssemblyDefinition.referencedAssemblies):
                {
                    if (getter() is List<string> list)
                    {
                        EditorGUI.TreeNode(name.ExpandCamelCaseName(), name, false, () =>
                        {
                            if (referencedAssemblies.Length != list.Count)
                            {
                                referencedAssemblies = new AssemblyDefinition[list.Count];

                                for (var i = 0; i < list.Count; i++)
                                {
                                    referencedAssemblies[i] = (AssemblyDefinition)AssemblyDefinition.Create(list[i] ?? "");
                                }
                            }

                            if (ignoredGuids.Length != list.Count)
                            {
                                ignoredGuids = new string[referencedAssemblies.Length][];

                                for (var i = 0; i < list.Count; i++)
                                {
                                    ignoredGuids[i] = new string[referencedAssemblies.Length];

                                    for (var j = 0; j < list.Count; j++)
                                    {
                                        if (i == j)
                                        {
                                            ignoredGuids[i][j] = (target as AssemblyDefinition)?.guid;

                                            continue;
                                        }

                                        ignoredGuids[i][j] = referencedAssemblies[j]?.guid;
                                    }
                                }
                            }

                            for (var i = 0; i < list.Count; i++)
                            {
                                var newValue = (AssemblyDefinition)EditorGUI.ObjectPicker(typeof(AssemblyDefinition), $"Assembly {i + 1}", referencedAssemblies[i], $"{name}.Item{i}",
                                    ignoredGuids[i]);

                                if (newValue != referencedAssemblies[i])
                                {
                                    referencedAssemblies[i] = newValue;

                                    ignoredGuids = [];

                                    list[i] = newValue?.guid;
                                }

                                EditorGUI.SameLine();

                                EditorGUI.Button("-", $"{name}.Item{i}.Remove", () =>
                                {
                                    list.RemoveAt(i);
                                });
                            }

                        }, null,
                        () =>
                        {
                            EditorGUI.SameLine();

                            EditorGUI.Button("+", $"{name}.Add", () =>
                            {
                                list.Add("");
                            });
                        });
                    }
                }

                return true;

            case nameof(AssemblyDefinition.referencedPlugins):

                if(!asmDef.overrideReferences)
                {
                    return true;
                }

                {
                    if (getter() is List<string> list)
                    {
                        EditorGUI.TreeNode(name.ExpandCamelCaseName(), name, false, () =>
                        {
                            if (referencedPlugins.Length != list.Count)
                            {
                                referencedPlugins = new PluginAsset[list.Count];

                                for (var i = 0; i < list.Count; i++)
                                {
                                    var plugin = referencedPlugins[i] = (PluginAsset)PluginAsset.Create(list[i] ?? "");

                                    var assetPath = AssetDatabase.GetAssetPath(list[i] ?? "");

                                    if(assetPath != null)
                                    {
                                        assetPath = EditorUtils.GetRootPath(assetPath);
                                    }

                                    assetPath ??= list[i] ?? "";

                                    if (plugin != null &&
                                        (!PluginAsset.IsAssembly(assetPath) ||
                                        plugin.autoReferenced))
                                    {
                                        list.RemoveAt(i);

                                        //Will force a recreation
                                        return;
                                    }
                                }
                            }

                            if (ignoredGuids.Length != list.Count)
                            {
                                ignoredGuids = new string[referencedPlugins.Length][];

                                for (var i = 0; i < list.Count; i++)
                                {
                                    ignoredGuids[i] = new string[referencedPlugins.Length];

                                    for (var j = 0; j < list.Count; j++)
                                    {
                                        if (i == j)
                                        {
                                            ignoredGuids[i][j] = (target as PluginAsset)?.guid;

                                            continue;
                                        }

                                        ignoredGuids[i][j] = referencedPlugins[j]?.guid;
                                    }
                                }
                            }

                            for (var i = 0; i < list.Count; i++)
                            {
                                var newValue = (PluginAsset)EditorGUI.ObjectPicker(typeof(PluginAsset), $"Reference {i + 1}", referencedPlugins[i], $"{name}.Item{i}",
                                    ignoredGuids[i], (guid) =>
                                    {
                                        var path = AssetDatabase.GetAssetPath(guid);

                                        if(path == null)
                                        {
                                            return false;
                                        }

                                        return PluginAsset.IsAssembly(EditorUtils.GetRootPath(path));
                                    });

                                if (newValue != referencedPlugins[i])
                                {
                                    referencedPlugins[i] = newValue;

                                    ignoredGuids = [];

                                    list[i] = newValue?.guid;
                                }

                                EditorGUI.SameLine();

                                EditorGUI.Button("-", $"{name}.Item{i}.Remove", () =>
                                {
                                    list.RemoveAt(i);
                                });
                            }

                        }, null,
                        () =>
                        {
                            EditorGUI.SameLine();

                            EditorGUI.Button("+", $"{name}.Add", () =>
                            {
                                list.Add("");
                            });
                        });
                    }
                }

                return true;
        }

        return base.DrawProperty(type, name, getter, setter, attributes);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(platformList.Length == 0)
        {
            platformList = Enum.GetValues<AppPlatform>();
        }

        ShowAssetUI(null);
    }
}
