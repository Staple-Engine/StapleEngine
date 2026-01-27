using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(PluginAsset))]
internal class PluginAssetEditor : AssetEditor
{
    private AppPlatform[] platformList = [];
    private bool isAssembly = false;

    public PluginAssetEditor()
    {
        isMetaEditor = true;
    }

    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if (target is not PluginAsset plugin)
        {
            return true;
        }

        switch (name)
        {
            case nameof(PluginAsset.autoReferenced):

                return !isAssembly;

            case nameof(PluginAsset.platforms):

                {
                    if (plugin.anyPlatform)
                    {
                        return true;
                    }

                    if (getter() is List<AppPlatform> list)
                    {
                        EditorGUI.Label(name.ExpandCamelCaseName());

                        for (var i = 0; i < platformList.Length; i++)
                        {
                            var value = platformList[i];

                            var add = EditorGUI.Toggle(value.ToString(), $"{name}.Item{i}", list.Contains(value) || list.Count == 0);

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
        }

        return base.DrawProperty(type, name, getter, setter, attributes);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (platformList.Length == 0)
        {
            platformList = Enum.GetValues<AppPlatform>();

            isAssembly = true;

            try
            {
                AssemblyName.GetAssemblyName(path);
            }
            catch (Exception)
            {
                isAssembly = false;

                if(target is PluginAsset plugin)
                {
                    plugin.autoReferenced = false;
                }
            }
        }

        ShowAssetUI(null);
    }
}
