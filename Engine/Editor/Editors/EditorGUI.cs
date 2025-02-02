using Hexa.NET.ImGui;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor;

/// <summary>
/// GUI functions for custom editors and editor windows
/// </summary>
public static class EditorGUI
{
    internal static ImGuiIOPtr io;
    internal static StapleEditor editor;

    internal static readonly Dictionary<string, object> pendingObjectPickers = [];

    private static readonly Dictionary<string, object> cachedEnumValues = [];

    private static bool changed = false;

    private static readonly Dictionary<string, bool> treeViewStates = [];

    private static readonly HashSet<int> usedTreeViewStates = [];

    private static string MakeIdentifier(string identifier, string key) => $"{identifier}##{key}";

    private static bool IdentifierColumns(string identifier, Func<bool> callback)
    {
        var width = RemainingHorizontalSpace();

        ImGui.Columns(2, false);

        var w = width / 3;

        ImGui.SetColumnWidth(0, w);

        ImGui.Text(identifier);

        ImGui.NextColumn();

        var changed = callback();

        ImGui.Columns(1);

        return changed;
    }

    internal static void ExecuteHandler(Action handler, string label)
    {
        try
        {
            handler?.Invoke();
        }
        catch(Exception e)
        {
            Log.Debug($"[EditorGUI] Failed to execute handler for {label}: {e}");
        }
    }

    internal static void OnFrameStart()
    {
        if (treeViewStates.Count > 0)
        {
            var keys = treeViewStates.Keys.ToArray();

            foreach (var key in keys)
            {
                if (usedTreeViewStates.Contains(key.GetHashCode()))
                {
                    continue;
                }

                treeViewStates.Remove(key);
            }
        }

        if (usedTreeViewStates.Count > 0)
        {
            usedTreeViewStates.Clear();
        }
    }

    /// <summary>
    /// Whether the GUI was interacted with this frame
    /// </summary>
    public static bool Changed
    {
        get => changed;

        internal set
        {
            changed = value;

            if(changed)
            {
                StapleEditor.instance.mouseIsHoveringImGui = true;
            }
        }
    }

    /// <summary>
    /// How much horizontal space is left in the current window
    /// </summary>
    /// <returns>The horizontal space</returns>
    public static float RemainingHorizontalSpace()
    {
        return ImGui.GetContentRegionAvail().X;
    }

    /// <summary>
    /// How much vertical space is left in the current window
    /// </summary>
    /// <returns>The vertical space</returns>
    public static float RemainingVerticalSpace()
    {
        return ImGui.GetContentRegionAvail().Y;
    }

    /// <summary>
    /// Gets the current GUI cursor position (where the GUI is currently being filled)
    /// </summary>
    /// <returns>The cursor position</returns>
    public static Vector2 CurrentGUICursorPosition()
    {
        return ImGui.GetCursorScreenPos();
    }

    /// <summary>
    /// Make the next element be horizontal to the last one
    /// </summary>
    public static void SameLine()
    {
        ImGui.SameLine();
    }

    /// <summary>
    /// Adds some space between elements
    /// </summary>
    public static void Space()
    {
        ImGui.Spacing();
    }

    public static void Columns(int count, Func<int, float> columnWidth, Action<int> action)
    {
        ImGui.Columns(count, false);

        for(var i = 0; i < count; i++)
        {
            var width = 1.0f;

            try
            {
                width = columnWidth(i);
            }
            catch (Exception e)
            {
                Log.Debug($"[EditorGUI] Failed to execute width handler for Column {i}: {e}");
            }

            ImGui.SetColumnWidth(i, width);

            try
            {
                action(i);
            }
            catch (Exception e)
            {
                Log.Debug($"[EditorGUI] Failed to execute action handler for Column {i}: {e}");
            }

            if(i + 1 < count)
            {
                ImGui.NextColumn();
            }
        }

        ImGui.Columns(1);
    }

    /// <summary>
    /// Shows a text label
    /// </summary>
    /// <param name="text">The text to show</param>
    public static void Label(string text)
    {
        ImGui.Text(text);
    }

    /// <summary>
    /// Shows a button
    /// </summary>
    /// <param name="label">The button label</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="handler">A handler to execute if the button is clicked</param>
    /// <returns>Whether the button was clicked</returns>
    public static void Button(string label, string key, Action handler)
    {
        if(ImGui.Button(MakeIdentifier(label, key)))
        {
            ExecuteHandler(handler, $"Button {label}");
        }
    }

    /// <summary>
    /// Shows a disabled button
    /// </summary>
    /// <param name="label">The button label</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="handler">A handler to execute if the button is clicked</param>
    /// <returns>Whether the button was clicked</returns>
    public static void ButtonDisabled(string label, string key, Action handler)
    {
        ImGui.BeginDisabled();

        if(ImGui.Button(MakeIdentifier(label, key)))
        {
            ExecuteHandler(handler, $"ButtonDisabled {label}");
        }

        ImGui.EndDisabled();
    }

    /// <summary>
    /// Shows a text field for an int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static int IntField(string label, string key, int value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputInt(MakeIdentifier("", key), ref value);
        });

        return value;
    }

    /// <summary>
    /// Shows a text field for a float
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static float FloatField(string label, string key, float value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat(MakeIdentifier("", key), ref value);
        });

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector2
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector2 Vector2Field(string label, string key, Vector2 value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat2(MakeIdentifier("", key), ref value);
        });

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector2Int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector2Int Vector2IntField(string label, string key, Vector2Int value)
    {
        var values = new int[] { value.X, value.Y };

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputInt2(MakeIdentifier("", key), ref values[0]);
        });

        return new Vector2Int(values[0], values[1]);
    }

    /// <summary>
    /// Shows a text field for a Vector3
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector3 Vector3Field(string label, string key, Vector3 value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat3(MakeIdentifier("", key), ref value);
        });

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector4
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector4 Vector4Field(string label, string key, Vector4 value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat4(MakeIdentifier("", key), ref value);
        });

        return value;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <param name="simple">Whether to use simple mode</param>
    /// <returns>The new value</returns>
    public static T EnumDropdown<T>(string label, string key, T value, bool simple = false) where T: struct, Enum
    {
        if (cachedEnumValues.TryGetValue(typeof(T).FullName, out var v) == false)
        {
            v = Enum.GetValues<T>()
                .ToList();

            cachedEnumValues.Add(typeof(T).FullName, v);
        }

        var values = v as List<T>;

        var isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;

        var current = isFlags ? 0 : values.IndexOf(value);

        var valueStrings = values
            .Select(x =>
            {
                var prefix = isFlags ? (value.HasFlag(x) ? "*" : " ") : "";

                return $"{prefix}{x}";
            })
            .ToArray();

        var prevChanged = Changed;

        Changed = false;

        var newValue = values[Dropdown(label, key, valueStrings, current, simple)];

        if (isFlags)
        {
            if (Changed)
            {
                if (value.HasFlag(newValue))
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (T)Enum.ToObject(typeof(T), a & ~b);
                }
                else
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (T)Enum.ToObject(typeof(T), a | b);
                }
            }
            else
            {
                newValue = value;
            }
        }

        Changed = prevChanged;

        return newValue;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <param name="values">The valid values for the field</param>
    /// <returns>The new value</returns>
    public static T EnumDropdown<T>(string label, string key, T value, List<T> values, bool simple = false) where T : struct, Enum
    {
        var isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;

        var current = isFlags ? 0 : values.IndexOf(value);

        var valueStrings = values
            .Select(x =>
            {
                var prefix = isFlags ? (value.HasFlag(x) ? "*" : " ") : "";

                return $"{prefix}{x}";
            })
            .ToArray();

        var prevChanged = Changed;

        Changed = false;

        var newValue = values[Dropdown(label, key, valueStrings, current, simple)];

        if (isFlags)
        {
            if (Changed)
            {
                if (value.HasFlag(newValue))
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (T)Enum.ToObject(typeof(T), a & ~b);
                }
                else
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (T)Enum.ToObject(typeof(T), a | b);
                }
            }
            else
            {
                newValue = value;
            }
        }

        Changed = prevChanged;

        return newValue;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Enum EnumDropdown(string label, string key, Enum value, Type enumType)
    {
        var v = Enum.GetValues(enumType);

        var values = new List<object>();

        foreach (var t in v)
        {
            values.Add(t);
        }

        var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

        var current = isFlags ? 0 : values.IndexOf(value);

        var valueStrings = values
            .Select(x =>
            {
                var prefix = isFlags ? (value.HasFlag((Enum)x) ? "* " : "  ") : "";

                return $"{prefix}{x}";
            })
            .ToArray();

        var prevChanged = Changed;

        Changed = false;

        var newValue = (Enum)values[Dropdown(label, key, valueStrings, current)];

        if (isFlags)
        {
            if (Changed)
            {
                if (value.HasFlag(newValue))
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (Enum)Enum.ToObject(enumType, a & ~b);
                }
                else
                {
                    var a = Convert.ToInt64(value);
                    var b = Convert.ToInt64(newValue);

                    newValue = (Enum)Enum.ToObject(enumType, a | b);
                }
            }
            else
            {
                newValue = value;
            }
        }

        Changed = prevChanged;

        return newValue;
    }

    /// <summary>
    /// Shows a dropdown field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="options">The options for the field</param>
    /// <param name="current">The current value index for the field</param>
    /// <returns>The index of the selected value</returns>
    public static int Dropdown(string label, string key, string[] options, int current, bool simple = false)
    {
        if (simple)
        {
            Changed |= ImGui.Combo(MakeIdentifier(label, key), ref current, $"{string.Join("\0", options)}\0");
        }
        else
        {
            Changed |= IdentifierColumns(label, () =>
            {
                return ImGui.Combo(MakeIdentifier("", key), ref current, $"{string.Join("\0", options)}\0");
            });
        }

        if (current < 0)
        {
            current = 0;
        }

        return current;
    }

    /// <summary>
    /// Shows a text field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <param name="simple">Whether to use the simplest rendering mode</param>
    /// <returns>The new value</returns>
    public static string TextField(string label, string key, string value, bool simple = false, int maxLength = 1000)
    {
        return TextField(label, key, value, Vector2.Zero, simple, maxLength);
    }

    /// <summary>
    /// Shows a text field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <param name="size">How large to make the text field</param>
    /// <param name="simple">Whether to use the simplest rendering mode</param>
    /// <returns>The new value</returns>
    public static string TextField(string label, string key, string value, Vector2 size, bool simple = false, int maxLength = 1000)
    {
        value ??= "";

        if(simple)
        {
            unsafe
            {
                Changed |= ImGui.InputTextEx(MakeIdentifier(label, key), "", ref value, maxLength, size, ImGuiInputTextFlags.None, null, null);
            }
        }
        else
        {
            Changed |= IdentifierColumns(label, () =>
            {
                return ImGui.InputText(MakeIdentifier("", key), ref value, (uint)maxLength);
            });
        }

        return value;
    }

    /// <summary>
    /// Shows a text field with multiline input
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <param name="size">The size the text box should use</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <returns>The new value</returns>
    public static string TextFieldMultiline(string label, string key, string value, Vector2 size, int maxLength = 1000)
    {
        value ??= "";

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputTextMultiline(MakeIdentifier("", key), ref value, (uint)maxLength, size);
        });

        return value;
    }

    /// <summary>
    /// Shows a toggle
    /// </summary>
    /// <param name="label">The toggle label</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static bool Toggle(string label, string key, bool value)
    {
        Changed |= ImGui.Checkbox(MakeIdentifier(label, key), ref value);

        return value;
    }

    /// <summary>
    /// Shows a color field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static Color ColorField(string label, string key, Color value)
    {
        var v = new Vector4(value.r, value.g, value.b, value.a);

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.ColorEdit4(MakeIdentifier("", key), ref v);
        });

        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Shows a color picker field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static Color ColorPicker(string label, string key, Color value)
    {
        var v = new Vector4(value.r, value.g, value.b, value.a);

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.ColorPicker4(MakeIdentifier("", key), ref v);
        });

        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Shows an object picker field
    /// </summary>
    /// <param name="type">The type to choose</param>
    /// <param name="name">The name to show for the field</param>
    /// <param name="current">The current value</param>
    /// <param name="identifier">Extra identifier if needed</param>
    /// <returns>The new value</returns>
    public static object ObjectPicker(Type type, string name, object current, string identifier = "")
    {
        ImGui.Text(name);

        ImGui.SameLine();

        string selectedName = null;

        if (current is IGuidAsset guidAsset)
        {
            var guid = guidAsset.Guid;

            if(Path.IsPathRooted(guidAsset.Guid))
            {
                var cacheIndex = guid.IndexOf(StapleEditor.instance.currentPlatform.ToString());

                guid = guid.Substring(cacheIndex + $"{StapleEditor.instance.currentPlatform}0".Length).Replace("\\", "/");

                guid = AssetDatabase.GetAssetGuid(guid) ?? guid;
            }

            selectedName = AssetDatabase.GetAssetName(guid);
        }

        selectedName ??= current?.ToString() ?? "(None)";

        if((type == typeof(Texture) || type.IsSubclassOf(typeof(Texture))) && current is Texture t && t.Disposed == false)
        {
            ImGui.Image(ImGuiProxy.GetImGuiTexture(t), new Vector2(32, 32));
        }
        else
        {
            ImGui.Text(selectedName);
        }

        ImGui.SameLine();

        var key = $"{type.FullName}{name}{current}{identifier}";

        if (ImGui.SmallButton(MakeIdentifier("O", key)))
        {
            editor.ShowAssetPicker(type, key);
        }

        if(pendingObjectPickers.ContainsKey(key) == false)
        {
            pendingObjectPickers.Add(key, current);
        }
        else if (pendingObjectPickers[key] != current)
        {
            var newValue = pendingObjectPickers[key];

            pendingObjectPickers.Remove(key);

            if(current is string)
            {
                if(current is IGuidAsset g)
                {
                    return g.Guid;
                }
                else
                {
                    return null;
                }
            }

            return newValue;
        }

        return current;
    }

    /// <summary>
    /// Shows a texture
    /// </summary>
    /// <param name="texture">The texture to show</param>
    /// <param name="size">The size of the image that will appear</param>
    public static void Texture(Texture texture, Vector2 size)
    {
        Texture(texture, size, Color.White);
    }

    /// <summary>
    /// Shows a texture
    /// </summary>
    /// <param name="texture">The texture to show</param>
    /// <param name="size">The size of the image that will appear</param>
    /// <param name="color">The color of the image</param>
    public static void Texture(Texture texture, Vector2 size, Color color)
    {
        if (texture == null)
        {
            return;
        }

        ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size, new Vector4(color.r, color.g, color.b, color.a));
    }

    /// <summary>
    /// Shows a texture with a specific rectangle and rotation
    /// </summary>
    /// <param name="texture">The texture to show</param>
    /// <param name="rect">The rectangle with the coordinates inside the texture</param>
    /// <param name="size">The size of the image that will appear</param>
    /// <param name="rotation">The rotation to apply to the texture</param>
    public static void TextureRect(Texture texture, Rect rect, Vector2 size, TextureSpriteRotation rotation = TextureSpriteRotation.None)
    {
        if (texture == null)
        {
            return;
        }

        var min = new Vector2(rect.left / (float)texture.Width, rect.top / (float)texture.Height);
        var max = new Vector2(rect.right / (float)texture.Width, rect.bottom / (float)texture.Height);

        switch(rotation)
        {
            case TextureSpriteRotation.FlipX:

                ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size, new Vector2(max.X, min.Y), new Vector2(min.X, max.Y));

                break;

            case TextureSpriteRotation.FlipY:

                ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size, new Vector2(min.X, max.Y), new Vector2(max.X, min.Y));

                break;

            default:

                ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size, min, max);

                break;
        }
    }

    /// <summary>
    /// Shows a slider for an int value
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static int IntSlider(string label, string key, int value, int min, int max)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.SliderInt(MakeIdentifier("", key), ref value, min, max);
        });

        return value;
    }

    /// <summary>
    /// Shows a slider for a float value
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static float FloatSlider(string label, string key, float value, float min, float max)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.SliderFloat(MakeIdentifier("", key), ref value, min, max);
        });

        return value;
    }

    /// <summary>
    /// Creates a group
    /// </summary>
    /// <param name="handler">A handler for the content of the group</param>
    public static void Group(Action handler)
    {
        ImGui.BeginGroup();

        ExecuteHandler(handler, "Group");

        ImGui.EndGroup();
    }

    /// <summary>
    /// Creates a tree node, and runs a handler if it's open
    /// </summary>
    /// <param name="label">The label of the tree node</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="leaf">Whether it's a leaf (doesn't open on click, no arrow)</param>
    /// <param name="clickHandler">A handler for when it is clicked</param>
    /// <param name="openHandler">A handler for when it is open</param>
    /// <param name="prefixHandler">A handler to run regardless of the node being open</param>
    /// <remarks>Click Handler will trigger when the left or right mouse button is clicked</remarks>
    public static void TreeNode(string label, string key, bool leaf, Action openHandler, Action clickHandler,
        Action prefixHandler = null)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (leaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        var open = ImGui.TreeNodeEx($"##{key}", flags);

        var stateKey = $"{label}-{key}";

        if (treeViewStates.TryGetValue(stateKey, out var isOpen) == false)
        {
            isOpen = false;

            treeViewStates.Add(stateKey, isOpen);
        }

        usedTreeViewStates.Add(stateKey.GetHashCode());

        if (isOpen != open)
        {
            treeViewStates.AddOrSetKey(stateKey, open);
        }

        isOpen = open;

        ImGui.SameLine();

        var clicked = ImGui.Selectable(label);

        ExecuteHandler(prefixHandler, $"TreeNode {label} prefix");

        if (ImGui.IsItemClicked() ||
            clicked ||
            (ImGui.IsItemHovered() && Input.GetMouseButtonUp(MouseButton.Right)))
        {
            ExecuteHandler(clickHandler, $"TreeNode {label} click");
        }

        if (open)
        {
            ExecuteHandler(openHandler, $"TreeNode {label} open");

            if (leaf == false)
            {
                ImGui.TreePop();
            }
        }
    }

    /// <summary>
    /// Creates a tree node with an icon, and runs a handler if it's open
    /// </summary>
    /// <param name="icon">The icon texture</param>
    /// <param name="color">The icon's tint color</param>
    /// <param name="label">The label of the tree node</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="leaf">Whether it's a leaf (doesn't open on click, no arrow)</param>
    /// <param name="clickHandler">A handler for when it is clicked</param>
    /// <param name="openHandler">A handler for when it is open</param>
    /// <param name="prefixHandler">A handler to run regardless of the node being open</param>
    /// <remarks>Click Handler will trigger when the left or right mouse button is clicked</remarks>
    public static void TreeNodeIcon(Texture icon, Color color, string label, string key, bool leaf,
        Action openHandler, Action clickHandler, Action prefixHandler = null)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (leaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        var open = ImGui.TreeNodeEx($"##{key}", flags);

        var stateKey = $"{label}-{key}";

        if (treeViewStates.TryGetValue(stateKey, out var isOpen) == false)
        {
            isOpen = false;

            treeViewStates.Add(stateKey, isOpen);
        }

        usedTreeViewStates.Add(stateKey.GetHashCode());

        if(isOpen != open)
        {
            treeViewStates.AddOrSetKey(stateKey, open);
        }

        isOpen = open;

        if (icon != null)
        {
            ImGui.SameLine();

            Texture(icon, new Vector2(20, 20), color);
        }

        ImGui.SameLine();

        var clicked = ImGui.Selectable(label);

        ExecuteHandler(prefixHandler, $"TreeNode {label} prefix");

        if (ImGui.IsItemClicked() ||
            clicked ||
            (ImGui.IsItemHovered() && Input.GetMouseButtonUp(MouseButton.Right)))
        {
            ExecuteHandler(clickHandler, $"TreeNode {label} click");
        }

        if (open)
        {
            ExecuteHandler(openHandler, $"TreeNode {label} open");

            if (leaf == false)
            {
                ImGui.TreePop();
            }
        }
    }

    /// <summary>
    /// Creates a tree node with an icon, and runs a handler if it's open
    /// </summary>
    /// <param name="icon">The icon texture</param>
    /// <param name="label">The label of the tree node</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="leaf">Whether it's a leaf (doesn't open on click, no arrow)</param>
    /// <param name="clickHandler">A handler for when it is clicked</param>
    /// <param name="openHandler">A handler for when it is open</param>
    /// <param name="prefixHandler">A handler to run regardless of the node being open</param>
    /// <remarks>Click Handler will trigger when the left or right mouse button is clicked</remarks>
    public static void TreeNodeIcon(Texture icon, string label, string key, bool leaf, Action openHandler, Action clickHandler,
        Action prefixHandler = null)
    {
        TreeNodeIcon(icon, Color.White, label, key, leaf, openHandler, clickHandler, prefixHandler);
    }

    /// <summary>
    /// Creates a tab bar
    /// </summary>
    /// <param name="titles">The tab titles</param>
    /// <param name="handler">A handler with the tab index to render</param>
    public static void TabBar(string[] titles, string key, Action<int> handler)
    {
        if(ImGui.BeginTabBar(MakeIdentifier("", key)))
        {
            for(var i = 0; i < titles.Length; i++)
            {
                var title = titles[i];

                //Apparently using IDs here doesn't work out, so we don't.
                //The tab bar itself seems to work independently as long as we use IDs for it.
                if(ImGui.BeginTabItem(title))
                {
                    try
                    {
                        handler?.Invoke(i);
                    }
                    catch(Exception e)
                    {
                        Log.Debug($"[EditorGUI] TabBar item {title} exception: {e}");
                    }

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }

    /// <summary>
    /// Creates a disabled group
    /// </summary>
    /// <param name="disabled">Whether to actually disable (to simplify code)</param>
    /// <param name="handler">The handler for the code inside it</param>
    public static void Disabled(bool disabled, Action handler)
    {
        if(disabled)
        {
            ImGui.BeginDisabled();
        }

        ExecuteHandler(handler, $"Disabled ({disabled})");

        if(disabled)
        {
            ImGui.EndDisabled();
        }
    }

    /// <summary>
    /// Adds a rectangle to the GUI
    /// </summary>
    /// <param name="rect">The rectangle coordinates</param>
    /// <param name="color">The color of the coordinate</param>
    public static void AddRectangle(Rect rect, Color32 color)
    {
        ImGui.GetWindowDrawList().AddRect(new Vector2(rect.Min.X, rect.Min.Y), new Vector2(rect.Max.X, rect.Max.Y),
            ImGuiProxy.ImGuiRGBA(color.r, color.g, color.b, color.a));
    }

    /// <summary>
    /// Adds a rectangle to the GUI
    /// </summary>
    /// <param name="rect">The rectangle coordinates</param>
    /// <param name="color">The color of the coordinate</param>
    public static void AddRectangle(RectFloat rect, Color32 color)
    {
        ImGui.GetWindowDrawList().AddRect(rect.Min, rect.Max, ImGuiProxy.ImGuiRGBA(color.r, color.g, color.b, color.a));
    }

    /// <summary>
    /// Gets the size of a text string
    /// </summary>
    /// <param name="text">The text to measure</param>
    public static Vector2 GetTextSize(string text)
    {
        return ImGui.CalcTextSize(text);
    }

    /// <summary>
    /// Adds text to the GUI. You probably want to use the `Label` method instead!
    /// </summary>
    /// <param name="text">The text to add</param>
    /// <param name="position">The position of the text</param>
    /// <param name="color">The color of the text</param>
    public static void AddText(string text, Vector2 position, Color32 color)
    {
        ImGui.GetWindowDrawList().AddText(position, ImGuiProxy.ImGuiRGBA(color.r, color.g, color.b, color.a),
            text);
    }

    /// <summary>
    /// Creates a menu
    /// </summary>
    /// <param name="name">The name of the menu</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="handler">A handler called if the menu is used</param>
    public static void Menu(string name, string key, Action handler)
    {
        if(ImGui.BeginMenu(MakeIdentifier(name, key)))
        {
            ExecuteHandler(handler, $"Menu {name}");

            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Creates a menu item, normally used with menus
    /// </summary>
    /// <param name="name">The name of the menu item</param>
    /// <param name="handler">A handler called if the item is clicked</param>
    public static void MenuItem(string name, string key, Action handler)
    {
        if(ImGui.MenuItem(MakeIdentifier(name, key)))
        {
            ExecuteHandler(handler, $"MenuItem {name}");
        }
    }

    /// <summary>
    /// Creates a separator
    /// </summary>
    public static void Separator()
    {
        ImGui.Separator();
    }

    /// <summary>
    /// Sets a tooltip for the next element
    /// </summary>
    /// <param name="text">The text for the tooltip</param>
    public static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }

    /// <summary>
    /// Manages a drop target with a specific type and callback
    /// </summary>
    /// <param name="type">The type we want to handle</param>
    /// <param name="callback">The callback to call with the target</param>
    public static void DragDropTarget(Type type, Action<object> callback)
    {
        if (ImGui.BeginDragDropTarget())
        {
            if(type == typeof(Entity))
            {
                var payload = ImGui.AcceptDragDropPayload("ENTITY");

                unsafe
                {
                    if (payload.Handle != null && StapleEditor.instance.draggedEntity.IsValid)
                    {
                        var e = StapleEditor.instance.draggedEntity;

                        StapleEditor.instance.draggedEntity = default;

                        Changed = true;

                        callback(e);
                    }
                }
            }
            else if(type == typeof(IComponent) ||
                type.IsAssignableTo(typeof(IComponent)))
            {
                var payload = ImGui.AcceptDragDropPayload("ENTITY");

                unsafe
                {
                    if (payload.Handle != null && StapleEditor.instance.draggedEntity.IsValid &&
                        StapleEditor.instance.draggedEntity.TryGetComponent(out var component, type))
                    {
                        StapleEditor.instance.draggedEntity = default;

                        Changed = true;

                        callback(component);
                    }
                }
            }

            ImGui.EndDragDropTarget();
        }
    }

    public static void WindowFrame(string key, Vector2 size, Action handler)
    {
        if(ImGui.BeginChild(key, size))
        {
            ExecuteHandler(handler, $"Window {key}");
        }

        ImGui.EndChild();
    }
}
