using Hexa.NET.ImGui;
using Staple.Internal;
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
    private static Type[] propertyDrawerTypes;

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
    /// Keeps track of the cached property drawers for this editor
    /// </summary>
    private readonly Dictionary<string, PropertyDrawer> cachedPropertyDrawers = [];

    /// <summary>
    /// Attempts to draw a property.
    /// Override this to change how a property appears.
    /// Return true if you rendered the property or false if you want to use default rendering
    /// </summary>
    /// <param name="type">The type of the property</param>
    /// <param name="name">The property name</param>
    /// <param name="getter">Call this to get the current value</param>
    /// <param name="setter">Call this to set a new value</param>
    /// <param name="attributes">Call this to find specific attributes in the property</param>
    /// <returns>Whether the property was rendered, or to use default rendering</returns>
    public virtual bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        return false;
    }

    /// <summary>
    /// Called to render the inspector UI.
    /// If not overriden, will inspect each public field and render it.
    /// </summary>
    public virtual void OnInspectorGUI()
    {
        FieldInspector(target, "", "", false);
    }

    public virtual void Destroy()
    {
    }

    private void FieldInspector(object target, string targetName, string IDSuffix, bool indent)
    {
        if(target == null)
        {
            return;
        }

        void Content()
        {
            var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<HideInInspectorAttribute>() != null || field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                if (DrawProperty(field.FieldType, field.Name,
                    () => field.GetValue(target),
                    (value) => field.SetValue(target, value),
                    field.GetCustomAttribute))
                {
                    continue;
                }

                var type = field.FieldType;
                var name = field.Name.ExpandCamelCaseName();

                PropertyInspector(type, name, $"{targetName}{IDSuffix}",
                    () => field.GetValue(target),
                    (value) => field.SetValue(target, value),
                    (attribute) =>
                    {
                        if (attribute.IsSubclassOf(typeof(Attribute)))
                        {
                            return field.GetCustomAttribute(attribute);
                        }

                        return null;
                    });

                var tooltip = field.GetCustomAttribute<TooltipAttribute>();

                if (tooltip != null && ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(tooltip.caption);
                }
            }

            /*
            var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanWrite == false ||
                    property.GetCustomAttribute<HideInInspectorAttribute>() != null ||
                    property.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                if (DrawProperty(property.PropertyType, property.Name,
                    () => property.GetValue(target),
                    (value) => property.SetValue(target, value),
                    property.GetCustomAttribute))
                {
                    continue;
                }

                var type = property.PropertyType;
                var name = property.Name.ExpandCamelCaseName();

                PropertyInspector(type, name,
                    () => property.GetValue(target),
                    (value) => property.SetValue(target, value),
                    property.GetCustomAttribute);

                var tooltip = property.GetCustomAttribute<TooltipAttribute>();

                if (tooltip != null && ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(tooltip.caption);
                }
            }
            */
        }

        if (indent)
        {
            EditorGUI.Indent(Content);
        }
        else
        {
            Content();
        }
    }

    private void PropertyInspector(Type type, string name, string IDSuffix, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        foreach(var t in propertyDrawerTypes)
        {
            var a = t.GetCustomAttribute<CustomPropertyDrawerAttribute>();

            if(a == null)
            {
                continue;
            }

            if (a.targetType == type ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == a.targetType) ||
                attributes(a.targetType) != null)
            {
                if(cachedPropertyDrawers.TryGetValue(type.FullName, out var drawer) == false)
                {
                    try
                    {
                        drawer = (PropertyDrawer)Activator.CreateInstance(t);

                        cachedPropertyDrawers.Add(type.FullName, drawer);
                    }
                    catch(Exception e)
                    {
                        continue;
                    }
                }

                if(drawer != null)
                {
                    try
                    {
                        drawer.OnGUI(name, IDSuffix, getter, setter, attributes);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[{t.FullName}] Exception: {e}");
                    }

                    return;
                }
            }
        }

        switch (type)
        {
            case Type t when t.IsGenericType:

                if (t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = t.GetGenericArguments()[0];

                    if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        if (getter() is IList list)
                        {
                            var changed = false;

                            EditorGUI.TreeNode(name, $"{name}Node{IDSuffix}", false, () =>
                            {
                                ImGui.BeginGroup();

                                for (var i = 0; i < list.Count; i++)
                                {
                                    var entry = list[i];

                                    EditorGUI.Label($"{name} {i + 1}");

                                    var result = EditorGUI.ObjectPicker(listType, "", entry, $"{name} {i} {IDSuffix}");

                                    if (result != entry)
                                    {
                                        changed = true;

                                        list[i] = result;
                                    }

                                    EditorGUI.SameLine();

                                    EditorGUI.Button("-", $"{name}Remove{i}{IDSuffix}", () =>
                                    {
                                        changed = true;

                                        list.RemoveAt(i);
                                    });
                                }

                                ImGui.EndGroup();

                            }, null, () =>
                            {
                                EditorGUI.SameLine();

                                EditorGUI.Button("+", $"{name}Add{IDSuffix}", () =>
                                {
                                    changed = true;

                                    list.Add(listType.IsValueType ? Activator.CreateInstance(listType) : null);
                                });
                            });

                            if (changed)
                            {
                                setter(list);
                            }
                        }
                    }
                    else if (listType.IsPrimitive || listType == typeof(string))
                    {
                        if (getter() is IList list)
                        {
                            var changed = false;

                            EditorGUI.TreeNode(name, $"{name}Node{IDSuffix}", false, () =>
                            {
                                ImGui.BeginGroup();

                                for (var i = 0; i < list.Count; i++)
                                {
                                    var entry = list[i];

                                    PropertyInspector(listType, "", $"{name}{i}{IDSuffix}", () => entry,
                                        (value) =>
                                        {
                                            if (value != entry)
                                            {
                                                changed = true;

                                                list[i] = value;
                                            }
                                        },
                                        (attribute) =>
                                        {
                                            if (attribute.IsSubclassOf(typeof(Attribute)))
                                            {
                                                return listType.GetCustomAttribute(attribute);
                                            }

                                            return null;
                                        });

                                    EditorGUI.SameLine();

                                    EditorGUI.Button("-", $"{name}Remove{i}{IDSuffix}", () =>
                                    {
                                        changed = true;

                                        list.RemoveAt(i);
                                    });
                                }

                                ImGui.EndGroup();
                            }, null,
                            () =>
                            {
                                EditorGUI.SameLine();

                                EditorGUI.Button("+", $"{name}Add{IDSuffix}", () =>
                                {
                                    changed = true;

                                    list.Add(listType.IsValueType ? Activator.CreateInstance(listType) : null);
                                });
                            });

                            if (changed)
                            {
                                setter(list);
                            }
                        }
                    }
                    else if(listType.GetCustomAttribute<SerializableAttribute>() != null)
                    {
                        if (getter() is IList list)
                        {
                            var changed = false;

                            EditorGUI.TreeNode(name, $"{name}Node{IDSuffix}", false,
                                () =>
                                {
                                    ImGui.BeginGroup();

                                    for (var i = 0; i < list.Count; i++)
                                    {
                                        var entry = list[i];

                                        PropertyInspector(listType, "", $"{name}{i}{IDSuffix}", () => entry,
                                            (value) =>
                                            {
                                                if (value != entry)
                                                {
                                                    changed = true;

                                                    list[i] = value;
                                                }
                                            },
                                            (attribute) =>
                                            {
                                                if (attribute.IsSubclassOf(typeof(Attribute)))
                                                {
                                                    return listType.GetCustomAttribute(attribute);
                                                }

                                                return null;
                                            });

                                        EditorGUI.SameLine();

                                        EditorGUI.Button("-", $"{name}Remove{i}{IDSuffix}", () =>
                                        {
                                            changed = true;

                                            list.RemoveAt(i);
                                        });
                                    }

                                    ImGui.EndGroup();
                                },
                                null,
                                () =>
                                {
                                    EditorGUI.SameLine();

                                    EditorGUI.Button("+", $"{name}Add{IDSuffix}", () =>
                                    {
                                        changed = true;

                                        list.Add(Activator.CreateInstance(listType));
                                    });
                                });

                            if (changed)
                            {
                                setter(list);
                            }
                        }
                    }
                }

                break;

            case Type t when t.IsEnum:

                {
                    var value = (Enum)getter();

                    var newValue = EditorGUI.EnumDropdown(name, $"{name}Dropdown{IDSuffix}", value, type);

                    setter(newValue);
                }

                break;

            case Type t when t == typeof(string):

                {
                    var value = (string)getter();

                    string newValue;

                    if(attributes(typeof(MultilineAttribute)) != null)
                    {
                        newValue = EditorGUI.TextFieldMultiline(name, $"{name}TextMultiline{IDSuffix}", value, new Vector2(200, value.Split("\n").Length * 30));
                    }
                    else
                    {
                        newValue = EditorGUI.TextField(name, $"{name}Text{IDSuffix}", value);
                    }

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector2):

                {
                    var value = (Vector2)getter();

                    var newValue = EditorGUI.Vector2Field(name, $"{name}Vector2{IDSuffix}", value);

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    var value = (Vector2Int)getter();

                    var newValue = EditorGUI.Vector2IntField(name, $"{name}Vector2Int{IDSuffix}", value);

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector3):

                {
                    var value = (Vector3)getter();

                    var newValue = EditorGUI.Vector3Field(name, $"{name}Vector3{IDSuffix}", value);

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Vector4):

                {
                    var value = (Vector4)getter();

                    var newValue = EditorGUI.Vector4Field(name, $"{name}Vector4{IDSuffix}", value);

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(Quaternion):

                {
                    var quaternion = (Quaternion)getter();

                    var value = quaternion.ToEulerAngles();

                    var newValue = EditorGUI.Vector3Field(name, $"{name}Quaternion{IDSuffix}", value);

                    if (newValue != value)
                    {
                        quaternion = Math.FromEulerAngles(newValue);

                        setter(quaternion);
                    }
                }

                break;

            case Type t when t == typeof(uint):

                {
                    var value = (int)(uint)getter();

                    var newValue = (uint)value;

                    var min = attributes(typeof(MinAttribute)) as MinAttribute;
                    var range = attributes(typeof(RangeAttribute)) as RangeAttribute;

                    if (attributes(typeof(SortingLayerAttribute)) != null)
                    {
                        newValue = (uint)EditorGUI.Dropdown(name, $"{name}SortingLayer{IDSuffix}", LayerMask.AllSortingLayers.ToArray(), value);
                    }
                    else
                    {
                        if (range != null)
                        {
                            newValue = (uint)EditorGUI.IntSlider(name, $"{name}IntSlider{IDSuffix}", value, (int)range.minValue, (int)range.maxValue);
                        }
                        else
                        {
                            newValue = (uint)EditorGUI.IntField(name, $"{name}IntField{IDSuffix}", value);
                        }
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = (uint)min.minValue;
                    }

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(int):

                {
                    var value = (int)getter();

                    var min = attributes(typeof(MinAttribute)) as MinAttribute;
                    var range = attributes(typeof(RangeAttribute)) as RangeAttribute;

                    var newValue = value;

                    if (range != null)
                    {
                        newValue = EditorGUI.IntSlider(name, $"{name}IntSlider{IDSuffix}", value, (int)range.minValue, (int)range.maxValue);
                    }
                    else
                    {
                        newValue = EditorGUI.IntField(name, $"{name}IntField{IDSuffix}", value);
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = (int)min.minValue;
                    }

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(bool):

                {
                    var value = (bool)getter();

                    var newValue = EditorGUI.Toggle(name, $"{name}Toggle{IDSuffix}", value);

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(float):

                {
                    var value = (float)getter();

                    var min = attributes(typeof(MinAttribute)) as MinAttribute;
                    var range = attributes(typeof(RangeAttribute)) as RangeAttribute;

                    var newValue = value;

                    if (range != null)
                    {
                        newValue = EditorGUI.FloatSlider(name, $"{name}FloatSlider{IDSuffix}", value, range.minValue, range.maxValue);
                    }
                    else
                    {
                        newValue = EditorGUI.FloatField(name, $"{name}FloatField{IDSuffix}", value);
                    }

                    if (min != null && newValue < min.minValue)
                    {
                        newValue = min.minValue;
                    }

                    if (newValue != value)
                    {
                        setter(newValue);
                    }
                }

                break;

            case Type t when t == typeof(double):

                {
                    var value = (double)getter();

                    if (ImGui.InputDouble($"{name}##{name}Double{IDSuffix}", ref value))
                    {
                        var min = attributes(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = min.minValue;
                        }

                        setter(value);
                    }
                }

                break;

            case Type t when t == typeof(byte):

                {
                    var current = (byte)getter();
                    var value = (int)current;

                    if (ImGui.InputInt($"{name}##{name}Byte{IDSuffix}", ref value))
                    {
                        if (value < byte.MinValue)
                        {
                            value = byte.MinValue;
                        }

                        if (value > byte.MaxValue)
                        {
                            value = byte.MaxValue;
                        }

                        var min = attributes(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (byte)min.minValue;
                        }

                        setter((byte)value);
                    }
                }

                break;

            case Type t when t == typeof(short):

                {
                    var current = (short)getter();
                    var value = (int)current;

                    if (ImGui.InputInt($"{name}##{name}Short{IDSuffix}", ref value))
                    {
                        if (value < short.MinValue)
                        {
                            value = short.MinValue;
                        }

                        if (value > short.MaxValue)
                        {
                            value = short.MaxValue;
                        }

                        var min = attributes(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (short)min.minValue;
                        }

                        setter((short)value);
                    }
                }

                break;

            case Type t when t == typeof(ushort):

                {
                    var current = (ushort)getter();
                    var value = (int)current;

                    if (ImGui.InputInt($"{name}##{name}UShort{IDSuffix}", ref value))
                    {
                        if (value < ushort.MinValue)
                        {
                            value = ushort.MinValue;
                        }

                        if (value > ushort.MaxValue)
                        {
                            value = ushort.MaxValue;
                        }

                        var min = attributes(typeof(MinAttribute)) as MinAttribute;

                        if (min != null && value < min.minValue)
                        {
                            value = (ushort)min.minValue;
                        }

                        setter((ushort)value);
                    }
                }

                break;

            case Type t when t == typeof(Color) || t == typeof(Color32):

                {
                    Color c;

                    if (type == typeof(Color))
                    {
                        c = (Color)getter();
                    }
                    else
                    {
                        c = (Color)((Color32)getter());
                    }

                    var newValue = EditorGUI.ColorField(name, $"{name}Color{IDSuffix}", c);

                    if (newValue != c)
                    {
                        if (type == typeof(Color))
                        {
                            setter(newValue);
                        }
                        else
                        {
                            var c2 = (Color32)newValue;

                            setter(c2);
                        }
                    }
                }

                break;

            case Type t when typeof(IGuidAsset).IsAssignableFrom(t):

                {
                    var value = (IGuidAsset)getter();

                    var newValue = EditorGUI.ObjectPicker(t, name, value, IDSuffix);

                    setter(newValue);
                }

                break;

            case Type t when t == typeof(LayerMask):

                {
                    var value = (LayerMask)getter();
                    List<string> layers;

                    if (attributes(typeof(SortingLayerAttribute)) != null)
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

                            if (ImGui.Selectable($"{selectedString}{layers[i]}##{value.GetHashCode()}{IDSuffix}", selected))
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

                        setter(value);
                    }
                }

                break;

            case Type t when t == typeof(Sprite):

                {
                    var value = (Sprite)getter();

                    setter(EditorGUI.SpritePicker(name, value, IDSuffix));
                }

                break;

            case Type t when t == typeof(Entity):

                {
                    var value = (Entity)getter();

                    setter(EditorGUI.EntityField(t.Name, value, IDSuffix));
                }

                break;

            case Type t when t.IsAssignableTo(typeof(IComponent)):

                {
                    var value = (IComponent)getter();

                    setter(EditorGUI.ComponentField(name, value, IDSuffix));
                }

                break;

            case Type t when t.GetCustomAttribute<SerializableAttribute>() != null:

                {
                    if (getter() is object o)
                    {
                        EditorGUI.Label(name);

                        FieldInspector(o, name, IDSuffix, true);

                        if (EditorGUI.Changed)
                        {
                            setter(o);
                        }
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

        propertyDrawerTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsSubclassOf(typeof(PropertyDrawer)))
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
            instance.targets = [ target ];

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
