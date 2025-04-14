using System;
using System.Collections.Generic;

namespace Staple.Editor;

[CustomEditor(typeof(AssemblyDefinition))]
internal class AssemblyDefinitionEditor : AssetEditor
{
    private AppPlatform[] platformList = [];
    private AssemblyDefinition[] referencedAssemblies = [];
    private string[][] ignoredGuids = [];

    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if(name == nameof(AssemblyDefinition.includedPlatforms) ||
            name == nameof(AssemblyDefinition.excludedPlatforms))
        {
            if(getter() is List<AppPlatform> list)
            {
                EditorGUI.Label(name.ExpandCamelCaseName());

                for (var i = 0; i < platformList.Length; i++)
                {
                    var value = platformList[i];

                    var add = EditorGUI.Toggle(value.ToString(), $"{name}.Item{i}", list.Contains(value) ||
                        (list.Count == 0 && name == nameof(AssemblyDefinition.includedPlatforms)));

                    if(add)
                    {
                        if(name == nameof(AssemblyDefinition.includedPlatforms))
                        {
                            if (list.Count != 0 && list.Contains(value) == false)
                            {
                                list.Add(value);
                            }
                        }
                        else
                        {
                            list.Add(value);
                        }
                    }
                    else
                    {
                        if (name == nameof(AssemblyDefinition.includedPlatforms))
                        {
                            if (list.Count != 0)
                            {
                                list.Remove(value);
                            }
                            else
                            {
                                foreach (var v in platformList)
                                {
                                    if (v == value)
                                    {
                                        continue;
                                    }

                                    list.Add(v);
                                }
                            }
                        }
                        else
                        {
                            list.Remove(value);
                        }
                    }
                }
            }
        }
        else if(name == nameof(AssemblyDefinition.referencedAssemblies))
        {
            if(getter() is List<string> list)
            {
                EditorGUI.TreeNode(name.ExpandCamelCaseName(), name, false, () =>
                {
                    if(referencedAssemblies.Length != list.Count)
                    {
                        referencedAssemblies = new AssemblyDefinition[list.Count];

                        for (var i = 0; i < list.Count; i++)
                        {
                            referencedAssemblies[i] = (AssemblyDefinition)AssemblyDefinition.Create(list[i] ?? "");
                        }
                    }

                    if(ignoredGuids.Length != list.Count)
                    {
                        ignoredGuids = new string[referencedAssemblies.Length][];

                        for (var i = 0; i < list.Count; i++)
                        {
                            ignoredGuids[i] = new string[referencedAssemblies.Length];

                            for (var j = 0; j < list.Count; j++)
                            {
                                if (i == j)
                                {
                                    ignoredGuids[i][j] = (target as AssemblyDefinition).guid;

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

                        if(newValue != referencedAssemblies[i])
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
