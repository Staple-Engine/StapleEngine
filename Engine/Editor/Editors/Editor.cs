using Hexa.NET.ImGui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor;

/// <summary>
/// Editor class that renders the inspector for one or more targets
/// </summary>
public class Editor
{
    private static Type[] editorTypes;

    /// <summary>
    /// The original object that is being edited, if any
    /// </summary>
    public object original;

    /// <summary>
    /// The path to the asset
    /// </summary>
    public string path;

    /// <summary>
    /// The cache path to the asset
    /// </summary>
    public string cachePath;

    /// <summary>
    /// The target to edit
    /// </summary>
    public object target;

    /// <summary>
    /// All targets to edit (if supports multiple objects)
    /// </summary>
    public object[] targets;

    /// <summary>
    /// Attempts to render a field.
    /// Override this to change how a field appears.
    /// Return true if you rendered the field or false if you want to use default rendering
    /// </summary>
    /// <param name="field">The field to render</param>
    /// <returns>Whether the field was rendered, or to use default rendering</returns>
    public virtual bool RenderField(FieldInfo field)
    {
        return false;
    }

    /// <summary>
    /// Called to render the inspector UI.
    /// If not overriden, will inspect each public field and render it.
    /// </summary>
    public virtual void OnInspectorGUI()
    {
        var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if(field.GetCustomAttribute<HideInInspectorAttribute>() != null || field.GetCustomAttribute<NonSerializedAttribute>() != null)
            {
                continue;
            }

            if(RenderField(field))
            {
                continue;
            }

            var type = field.FieldType;
            var fieldName = field.Name.ExpandCamelCaseName();

            FieldInspector(type, fieldName, () => field.GetValue(target), (value) => field.SetValue(target, value), field.GetCustomAttribute);

            var tooltip = field.GetCustomAttribute<TooltipAttribute>();

            if(tooltip != null && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip.caption);
            }
        }
    }

    public virtual void Destroy()
    {
    }

    private void FieldInspector(Type type, string name, Func<object> getValue, Action<object> setValue, Func<Type, Attribute> getCustomAttribute)
    {
        switch (type)
        {
            case Type t when t.IsGenericType:

                if (t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = t.GetGenericArguments()[0];

                    if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        if (getValue() is IList list)
                        {
                            EditorGUI.Label(name);

                            EditorGUI.SameLine();

                            var changed = false;

                            EditorGUI.Button("+", () =>
                            {
                                changed = true;

                                list.Add(listType.IsValueType ? Activator.CreateInstance(listType) : null);
                            });

                            ImGui.BeginGroup();

                            for (var i = 0; i < list.Count; i++)
                            {
                                var entry = list[i];

                                var result = EditorGUI.ObjectPicker(listType, "", entry);

                                if (result != entry)
                                {
                                    changed = true;

                                    list[i] = result;
                                }

                                EditorGUI.SameLine();

                                EditorGUI.Button("-", () =>
                                {
                                    changed = true;

                                    list.RemoveAt(i);
                                });
                            }

                            ImGui.EndGroup();

                            if (changed)
                            {
                                setValue(list);
                            }
                        }
                    }
                    else if (listType.IsPrimitive)
                    {
                        if (getValue() is IList list)
                        {
                            EditorGUI.Label(name);

                            EditorGUI.SameLine();

                            var changed = false;

                            EditorGUI.Button("+", () =>
                            {
                                changed = true;

                                list.Add(listType.IsValueType ? Activator.CreateInstance(listType) : null);
                            });

                            ImGui.BeginGroup();

                            for (var i = 0; i < list.Count; i++)
                            {
                                var entry = list[i];

                                FieldInspector(listType, "", () => entry, (value) =>
                                {
                                    if (value != entry)
                                    {
                                        changed = true;

                                        list[i] = value;
                                    }
                                }, getCustomAttribute);

                                EditorGUI.SameLine();

                                EditorGUI.Button("-", () =>
                                {
                                    changed = true;

                                    list.RemoveAt(i);
                                });
                            }

                            ImGui.EndGroup();

                            if (changed)
                            {
                                setValue(list);
                            }
                        }
                    }
                }

                break;

            case Type t when t.IsEnum:

                {
                    var value = (Enum)getValue();

                    var newValue = EditorGUI.EnumDropdown(name, value, type);

                    setValue(newValue);
                }

                break;

            case Type t when t == typeof(string):

                {
                    var value = (string)getValue();

                    var newValue = EditorGUI.TextField(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector2):

                {
                    var value = (Vector2)getValue();

                    var newValue = EditorGUI.Vector2Field(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    var value = (Vector2Int)getValue();

                    var newValue = EditorGUI.Vector2IntField(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector3):

                {
                    var value = (Vector3)getValue();

                    var newValue = EditorGUI.Vector3Field(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector4):

                {
                    var value = (Vector4)getValue();

                    var newValue = EditorGUI.Vector4Field(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Quaternion):

                {
                    var quaternion = (Quaternion)getValue();

                    var value = quaternion.ToEulerAngles();

                    var newValue = EditorGUI.Vector3Field(name, value);

                    if (newValue != value)
                    {
                        quaternion = Math.FromEulerAngles(newValue);

                        setValue(quaternion);
                    }
                }

                break;

            case Type t when t == typeof(uint):

                {
                    var value = (int)(uint)getValue();

                    var newValue = (uint)value;

                    var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;
                    var range = getCustomAttribute(typeof(RangeAttribute)) as RangeAttribute;

                    if (getCustomAttribute(typeof(SortingLayerAttribute)) != null)
                    {
                        newValue = (uint)EditorGUI.Dropdown(name, LayerMask.AllSortingLayers.ToArray(), value);
                    }
                    else
                    {
                        if (range != null)
                        {
                            newValue = (uint)EditorGUI.IntSlider(name, value, (int)range.minValue, (int)range.maxValue);
                        }
                        else
                        {
                            newValue = (uint)EditorGUI.IntField(name, value);
                        }
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = (uint)min.minValue;
                    }

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(int):

                {
                    var value = (int)getValue();

                    var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;
                    var range = getCustomAttribute(typeof(RangeAttribute)) as RangeAttribute;

                    var newValue = value;

                    if (range != null)
                    {
                        newValue = EditorGUI.IntSlider(name, value, (int)range.minValue, (int)range.maxValue);
                    }
                    else
                    {
                        newValue = EditorGUI.IntField(name, value);
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = (int)min.minValue;
                    }

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(bool):

                {
                    var value = (bool)getValue();

                    var newValue = EditorGUI.Toggle(name, value);

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(float):

                {
                    var value = (float)getValue();

                    var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;
                    var range = getCustomAttribute(typeof(RangeAttribute)) as RangeAttribute;

                    var newValue = value;

                    if (range != null)
                    {
                        newValue = EditorGUI.FloatSlider(name, value, range.minValue, range.maxValue);
                    }
                    else
                    {
                        newValue = EditorGUI.FloatField(name, value);
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = min.minValue;
                    }

                    if (newValue != value)
                    {
                        setValue(newValue);
                    }
                }

                break;

            case Type t when t == typeof(double):

                {
                    var value = (double)getValue();

                    if (ImGui.InputDouble(name, ref value))
                    {
                        var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = min.minValue;
                        }

                        setValue(value);
                    }
                }

                break;

            case Type t when t == typeof(byte):

                {
                    var current = (byte)getValue();
                    var value = (int)current;

                    if (ImGui.InputInt(name, ref value))
                    {
                        if (value < byte.MinValue)
                        {
                            value = byte.MinValue;
                        }

                        if (value > byte.MaxValue)
                        {
                            value = byte.MaxValue;
                        }

                        var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (byte)min.minValue;
                        }

                        setValue((byte)value);
                    }
                }

                break;

            case Type t when t == typeof(short):

                {
                    var current = (short)getValue();
                    var value = (int)current;

                    if (ImGui.InputInt(name, ref value))
                    {
                        if (value < short.MinValue)
                        {
                            value = short.MinValue;
                        }

                        if (value > short.MaxValue)
                        {
                            value = short.MaxValue;
                        }

                        var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (short)min.minValue;
                        }

                        setValue((short)value);
                    }
                }

                break;

            case Type t when t == typeof(ushort):

                {
                    var current = (ushort)getValue();
                    var value = (int)current;

                    if (ImGui.InputInt(name, ref value))
                    {
                        if (value < ushort.MinValue)
                        {
                            value = ushort.MinValue;
                        }

                        if (value > ushort.MaxValue)
                        {
                            value = ushort.MaxValue;
                        }

                        var min = getCustomAttribute(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (ushort)min.minValue;
                        }

                        setValue((ushort)value);
                    }
                }

                break;

            case Type t when t == typeof(Color) || t == typeof(Color32):

                {
                    Color c;

                    if (type == typeof(Color))
                    {
                        c = (Color)getValue();
                    }
                    else
                    {
                        c = (Color)((Color32)getValue());
                    }

                    var newValue = EditorGUI.ColorField(name, c);

                    if (newValue != c)
                    {
                        if (type == typeof(Color))
                        {
                            setValue(newValue);
                        }
                        else
                        {
                            var c2 = (Color32)newValue;

                            setValue(c2);
                        }
                    }
                }

                break;

            case Type t when typeof(IGuidAsset).IsAssignableFrom(t):

                {
                    var value = (IGuidAsset)getValue();

                    var newValue = EditorGUI.ObjectPicker(t, name, value);

                    setValue(newValue);
                }

                break;

            case Type t when t == typeof(LayerMask):

                {
                    var value = (LayerMask)getValue();
                    List<string> layers;

                    if (getCustomAttribute(typeof(SortingLayerAttribute)) != null)
                    {
                        layers = LayerMask.AllSortingLayers;
                    }
                    else
                    {
                        layers = LayerMask.AllLayers
                            .Where(x => x != StapleEditor.RenderTargetLayerName)
                            .ToList();
                    }

                    var previewValue = "";

                    var all = true;

                    for (var i = 0; i < layers.Count; i++)
                    {
                        if (value.HasLayer((uint)i) == false)
                        {
                            all = false;
                        }
                        else
                        {
                            previewValue += (previewValue.Length > 0 ? ", " : "") + layers[i];
                        }
                    }

                    if (all)
                    {
                        previewValue = "Everything";
                    }
                    else if (previewValue.Length == 0)
                    {
                        previewValue = "Nothing";
                    }

                    if (ImGui.BeginCombo(name, previewValue))
                    {
                        for (var i = 0; i < layers.Count; i++)
                        {
                            var selected = value.HasLayer((uint)i);

                            var selectedString = selected ? "* " : "  ";

                            if (ImGui.Selectable($"{selectedString}{layers[i]}##{value.GetHashCode()}", selected))
                            {
                                if (selected)
                                {
                                    value.RemoveLayer((uint)i);
                                }
                                else
                                {
                                    value.SetLayer((uint)i);
                                }
                            }
                        }

                        ImGui.EndCombo();

                        setValue(value);
                    }
                }

                break;
        }
    }

    internal static void UpdateEditorTypes()
    {
        editorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsSubclassOf(typeof(Editor)))
                .Distinct()
                .ToArray();
    }

    public static Editor CreateEditor(object target, Type editorType = null)
    {
        var types = editorTypes;

        if (editorType == null)
        {
            var targetType = target.GetType();

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CustomEditorAttribute>();

                if (attribute == null || type.IsSubclassOf(typeof(Editor)) == false)
                {
                    continue;
                }

                if (attribute.target == targetType)
                {
                    editorType = type;

                    break;
                }
            }
        }

        if (editorType == null)
        {
            return null;
        }

        try
        {
            var instance = (Editor)Activator.CreateInstance(editorType);

            instance.target = target;
            instance.targets = new object[] { target };

            return instance;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Editor CreateEditor(object[] targets, Type editorType = null)
    {
        if (targets.Length == 0)
        {
            return null;
        }

        if (targets.Any(x => x == null))
        {
            return null;
        }

        var targetType = targets.FirstOrDefault().GetType();

        if (targets.Any(x => x.GetType() != targetType))
        {
            return null;
        }

        var types = editorTypes;

        if (editorType == null)
        {
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CustomEditorAttribute>();

                if (attribute == null || type.IsSubclassOf(typeof(Editor)) == false)
                {
                    continue;
                }

                if (attribute.target == targetType)
                {
                    editorType = type;

                    break;
                }
            }
        }

        if (editorType == null)
        {
            return null;
        }

        try
        {
            var instance = (Editor)Activator.CreateInstance(editorType);

            instance.targets = targets;

            return instance;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
