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
    [Flags]
    public enum SelectableFlags
    {
        None = ImGuiSelectableFlags.None,
        SpanAllColumns = ImGuiSelectableFlags.SpanAllColumns,
    }

    internal static ImGuiIOPtr io;
    internal static StapleEditor editor;

    private static bool changed = false;

    internal static readonly Dictionary<string, object> pendingObjectPickers = [];

    private static readonly Dictionary<string, object> cachedEnumValues = [];

    private static readonly Dictionary<string, bool> treeViewStates = [];

    private static readonly HashSet<int> usedTreeViewStates = [];

    private static readonly List<Action> endFrameCallbacks = [];

    private static string MakeIdentifier(string identifier, string key) => $"{identifier}##{key}";

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

    internal static void ExecuteHandler<T>(Action<T> handler, string label, T a)
    {
        try
        {
            handler?.Invoke(a);
        }
        catch (Exception e)
        {
            Log.Debug($"[EditorGUI] Failed to execute handler for {label}: {e}");
        }
    }

    internal static void ExecuteHandler<T, T2>(Action<T, T2> handler, string label, T a, T2 b)
    {
        try
        {
            handler?.Invoke(a, b);
        }
        catch (Exception e)
        {
            Log.Debug($"[EditorGUI] Failed to execute handler for {label}: {e}");
        }
    }

    internal static T ExecuteHandler<T>(Func<T> handler, string label)
    {
        try
        {
            if(handler != null)
            {
                return handler.Invoke();
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[EditorGUI] Failed to execute handler for {label}: {e}");
        }

        return default;
    }

    internal static T2 ExecuteHandler<T, T2>(Func<T, T2> handler, string label, T a)
    {
        try
        {
            if (handler != null)
            {
                return handler.Invoke(a);
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[EditorGUI] Failed to execute handler for {label}: {e}");
        }

        return default;
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

    internal static void OnFrameEnd()
    {
        var actions = endFrameCallbacks.ToArray();

        endFrameCallbacks.Clear();

        foreach(var action in actions)
        {
            action?.Invoke();
        }
    }

    private static void QueueFrameEndAction(Action action)
    {
        if(action == null)
        {
            return;
        }

        endFrameCallbacks.Add(action);
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

    public static Vector2 MousePosition => ImGui.GetMousePos();

    public static Vector2 MousePositionOnPopup => ImGui.GetMousePosOnOpeningCurrentPopup();

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
    /// Gets the current GUI cursor screen position (where the GUI is currently being filled)
    /// </summary>
    /// <returns>The cursor position</returns>
    public static Vector2 CurrentGUICursorScreenPosition()
    {
        return ImGui.GetCursorScreenPos();
    }

    /// <summary>
    /// Gets the current GUI cursor position (where the GUI is currently being filled)
    /// </summary>
    /// <returns>The cursor position</returns>
    public static Vector2 CurrentGUICursorPosition()
    {
        return ImGui.GetCursorPos();
    }

    /// <summary>
    /// Sets the current GUI cursor position (where the GUI is currently being filled)
    /// </summary>
    /// <param name="value">The cursor position</param>
    public static void SetCurrentGUICursorPosition(Vector2 value)
    {
        ImGui.SetCursorPos(value);
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

    /// <summary>
    /// Sets the next item's width
    /// </summary>
    /// <param name="width">The width in pixels</param>
    /// <param name="content">The content of the item</param>
    public static void ItemWidth(float width, Action content)
    {
        if(width > 0)
        {
            ImGui.PushItemWidth(width);
        }

        ExecuteHandler(content, $"ItemWidth {width}");

        if (width > 0)
        {
            ImGui.PopItemWidth();
        }
    }
    
    /// <summary>
    /// Indents content
    /// </summary>
    /// <param name="content">The content to indent</param>
    public static void Indent(Action content)
    {
        ImGui.Indent();

        ExecuteHandler(content, $"Indent");

        ImGui.Unindent();
    }

    /// <summary>
    /// Creates a line of columns of elements.
    /// </summary>
    /// <param name="count">Amount of columns</param>
    /// <param name="columnWidth">Callback for the width of each column. The parameter is the column index.</param>
    /// <param name="content">Content for each column. The parameter is the column index.</param>
    public static void Columns(int count, Func<int, float> columnWidth, Action<int> content)
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
                content(i);
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
    /// Shows a header label
    /// </summary>
    /// <param name="text">The text to show</param>
    public static void HeaderLabel(string text)
    {
        ImGui.PushFont(ImGuiProxy.instance.headerFont);

        Label(text);

        ImGui.PopFont();
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
        Changed |= ImGui.InputInt(MakeIdentifier(label, key), ref value, 0);

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
        Changed |= ImGui.InputFloat(MakeIdentifier(label, key), ref value, 0.0f);

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
        Changed |= ImGui.InputFloat2(MakeIdentifier(label, key), ref value);

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
        Changed |= ImGui.InputInt2(MakeIdentifier(label, key), ref value.X);

        return value;
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
        Changed |= ImGui.InputFloat3(MakeIdentifier(label, key), ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector3Int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector3Int Vector3IntField(string label, string key, Vector3Int value)
    {
        Changed |= ImGui.InputInt3(MakeIdentifier(label, key), ref value.X);

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
        Changed |= ImGui.InputFloat4(MakeIdentifier(label, key), ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector4Int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector4Int Vector4IntField(string label, string key, Vector4Int value)
    {
        Changed |= ImGui.InputInt4(MakeIdentifier(label, key), ref value.X);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Rect
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Rect RectField(string label, string key, Rect value)
    {
        Changed |= ImGui.InputInt4(MakeIdentifier(label, key), ref value.left);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector4
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static RectFloat RectFloatField(string label, string key, RectFloat value)
    {
        unsafe
        {
            Changed |= ImGui.InputFloat4(MakeIdentifier(label, key), &value.left);
        }

        return value;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static T EnumDropdown<T>(string label, string key, T value) where T: struct, Enum
    {
        if (cachedEnumValues.TryGetValue(typeof(T).FullName, out var v) == false || v is not List<T>)
        {
            v = Enum.GetValues<T>()
                .ToList();

            cachedEnumValues.AddOrSetKey(typeof(T).FullName, v);
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

        var newValue = values[Dropdown(label, key, valueStrings, current)];

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
    public static T EnumDropdown<T>(string label, string key, T value, List<T> values) where T : struct, Enum
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

        var newValue = values[Dropdown(label, key, valueStrings, current)];

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
    public static int Dropdown(string label, string key, string[] options, int current)
    {
        Changed |= ImGui.Combo(MakeIdentifier(label, key), ref current, $"{string.Join("\0", options)}\0");

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
    /// <returns>The new value</returns>
    public static string TextField(string label, string key, string value, int maxLength = 1000)
    {
        return TextField(label, key, value, Vector2.Zero, maxLength);
    }

    /// <summary>
    /// Shows a text field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <param name="value">The current value</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <param name="size">How large to make the text field</param>
    /// <returns>The new value</returns>
    public static string TextField(string label, string key, string value, Vector2 size, int maxLength = 1000)
    {
        value ??= "";

        unsafe
        {
            Changed |= ImGui.InputTextEx(MakeIdentifier(label, key), "", ref value, maxLength, size, ImGuiInputTextFlags.None, null, null);
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

        Changed |= ImGui.InputTextMultiline(MakeIdentifier("", key), ref value, (uint)maxLength, size);

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

        Changed |= ImGui.ColorEdit4(MakeIdentifier(label, key), ref v);

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

        Changed |= ImGui.ColorPicker4(MakeIdentifier(label, key), ref v);

        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Shows an object picker field
    /// </summary>
    /// <param name="type">The type to choose</param>
    /// <param name="name">The name to show for the field</param>
    /// <param name="current">The current value</param>
    /// <param name="identifier">Extra identifier if needed</param>
    /// <param name="ignoredGuids">Any ignored GUIDs</param>
    /// <param name="filter">A filter for an asset (GUID)</param>
    /// <returns>The new value</returns>
    public static object ObjectPicker(Type type, string name, object current, string identifier, string[] ignoredGuids = null,
        Func<string, bool> filter = null)
    {
        ImGui.Text(name);

        ImGui.SameLine();

        string selectedName = null;

        if (current is IGuidAsset guidAsset)
        {
            var guid = guidAsset.Guid.Guid;

            if(Path.IsPathRooted(guid))
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

        var key = $"{type.FullName}{name}{current}{identifier}";

        DragDropTarget("ASSET",
            () =>
            {
                if (StapleEditor.instance.dragDropPayloads.TryGetValue("ASSET", out var p))
                {
                    StapleEditor.instance.dropTargetEntity = default;
                    StapleEditor.instance.dropTargetObjectPickerType = type;
                    StapleEditor.instance.dropTargetObjectPickerAction =
                        (value) =>
                        {
                            if (value != null && value.GetType().IsAssignableTo(type))
                            {
                                pendingObjectPickers[key] = value;
                            }
                        };

                    ProjectBrowser.dropType = ProjectBrowserDropType.Asset;

                    p.action(p.index, p.item);

                    StapleEditor.instance.dragDropPayloads.Clear();
                }
            });

        ImGui.SameLine();

        if (ImGui.SmallButton(MakeIdentifier("O", key)))
        {
            editor.ShowAssetPicker(type, key, ignoredGuids ?? [], filter);
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
                    return g.Guid.Guid;
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
        Changed |= ImGui.SliderInt(MakeIdentifier("", key), ref value, min, max);

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
        Changed |= ImGui.SliderFloat(MakeIdentifier("", key), ref value, min, max);

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
    /// <param name="defaultOpen">Whether the tree should be open by default</param>
    /// <remarks>Click Handler will trigger when the left or right mouse button is clicked</remarks>
    public static void TreeNode(string label, string key, bool leaf, Action openHandler, Action clickHandler,
        Action prefixHandler = null, bool defaultOpen = false)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (leaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        if(defaultOpen)
        {
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
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
    /// <param name="defaultOpen">Whether the tree should be open by default</param>
    /// <remarks>Click Handler will trigger when the left or right mouse button is clicked</remarks>
    public static void TreeNodeIcon(Texture icon, Color color, string label, string key, bool leaf,
        Action openHandler, Action clickHandler, Action prefixHandler = null, bool defaultOpen = false)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (leaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        if (defaultOpen)
        {
            flags |= ImGuiTreeNodeFlags.DefaultOpen;
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

        var clicked = ImGui.Selectable(MakeIdentifier(label, key));

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
    /// Creates a menu bar
    /// </summary>
    /// <param name="handler">The content of the menu bar. You probably want to call Menu and MenuItem</param>
    public static void MenuBar(Action handler)
    {
        if(ImGui.BeginMenuBar())
        {
            ExecuteHandler(handler, $"MenuBar");

            ImGui.EndMenuBar();
        }
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
    /// Creates a selectable element
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="key">A unique key for the selectable</param>
    /// <param name="handler">The handler to execute when it's selected</param>
    public static void Selectable(string text, string key, Action handler, SelectableFlags flags = SelectableFlags.None)
    {
        if (ImGui.Selectable(MakeIdentifier(text, key), (ImGuiSelectableFlags)flags))
        {
            ExecuteHandler(handler, $"{text} Selectable");
        }
    }

    /// <summary>
    /// Manages a drop target with a specific payload
    /// </summary>
    /// <param name="payload">The payload</param>
    /// <param name="success">Triggered if successful</param>
    public static void DragDropTarget(string payload, Action success)
    {
        if(ImGui.BeginDragDropTarget())
        {
            var result = ImGui.AcceptDragDropPayload(payload);

            if(result.IsNull == false)
            {
                success();
            }

            ImGui.EndDragDropTarget();
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
                        StapleEditor.instance.draggedEntity.TryGetComponent(type, out var component))
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

    /// <summary>
    /// Creates a window frame as part of a window
    /// </summary>
    /// <param name="key">A unique key for the window</param>
    /// <param name="size">The size of the window. A size of 0,0 will auto resize</param>
    /// <param name="handler">Content for the window frame</param>
    public static void WindowFrame(string key, Vector2 size, Action handler)
    {
        if(ImGui.BeginChild(key, size))
        {
            ExecuteHandler(handler, $"Window {key}");
        }

        ImGui.EndChild();
    }

    /// <summary>
    /// Opens a popup. You should use <see cref="Popup(string, Action)" /> for the popup contents later in the window.
    /// </summary>
    /// <param name="key">A unique key for the popup</param>
    public static void OpenPopup(string key)
    {
        QueueFrameEndAction(() =>
        {
            ImGui.OpenPopup(key);
        });
    }

    /// <summary>
    /// Creates a popup
    /// </summary>
    /// <param name="key">>A unique key for the popup</param>
    /// <param name="handler">Content for the popup</param>
    public static void Popup(string key, Action handler)
    {
        QueueFrameEndAction(() =>
        {
            if (ImGui.BeginPopup(key))
            {
                ExecuteHandler(handler, $"Popup {key}");

                ImGui.EndPopup();
            }
        });
    }

    /// <summary>
    /// Shows a sprite picker
    /// </summary>
    /// <param name="name">The label to use</param>
    /// <param name="value">The current value</param>
    /// <param name="key">A unique key for this UI element</param>
    public static Sprite SpritePicker(string name, Sprite value, string key)
    {
        var texture = value?.texture;

        texture = (Texture)ObjectPicker(typeof(Texture), name.ExpandCamelCaseName(), texture, $"{key}.Texture");

        if (texture != null)
        {
            value ??= new();

            value.texture = texture;

            Label("Selected Sprite");

            SameLine();

            if (value.spriteIndex >= 0 && value.spriteIndex < value.texture.metadata.sprites.Count)
            {
                var sprite = value.texture.metadata.sprites[value.spriteIndex];

                TextureRect(value.texture, sprite.rect, new Vector2(32, 32), sprite.rotation);
            }
            else
            {
                Label("(none)");
            }

            SameLine();

            Button("O", $"{key}.Browse", () =>
            {
                var editor = StapleEditor.instance;
                var assetPath = AssetSerialization.GetAssetPathFromCache(AssetDatabase.GetAssetPath(value.texture.Guid.Guid));

                if (assetPath != value.texture.Guid.Guid && Path.IsPathRooted(assetPath) == false)
                {
                    assetPath = $"Assets{Path.DirectorySeparatorChar}{assetPath}";
                }

                editor.ShowSpritePicker(ThumbnailCache.GetTexture(assetPath) ?? value.texture,
                    value.texture.metadata.sprites,
                    (index) => value.spriteIndex = index);
            });
        }

        return value;
    }

    /// <summary>
    /// Shows a field where an entity can be dragged into
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <param name="value">The current entity</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <returns>The selected entity, if any</returns>
    public static Entity EntityField(string name, Entity value, string key)
    {
        Label($"{name} (Entity)");

        SameLine();

        if (value.IsValid)
        {
            Label(value.Name);
        }
        else
        {
            Label("(None)");
        }

        DragDropTarget(typeof(Entity), (v) =>
        {
            if (v is Entity e)
            {
                value = e;
            }
        });

        if (value.IsValid)
        {
            SameLine();

            Button("X", $"{name}_CLEAR{key}", () =>
            {
                value = default;
            });
        }

        return value;
    }

    /// <summary>
    /// Shows a field where a component can be dragged into
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="name">The name of the field</param>
    /// <param name="value">The current value of the component</param>
    /// <param name="key">A unique key for this UI element</param>
    /// <returns>The new value of the component, if any</returns>
    public static IComponent ComponentField(string name, Type type, IComponent value, string key)
    {
        Label($"{name} ({type.Name})");

        SameLine();

        if (World.Current.TryGetComponentEntity(value, out var target))
        {
            Label($"{target.Name} ({value.GetType().Name})");
        }
        else
        {
            Label("(None)");
        }

        DragDropTarget(type, (v) =>
        {
            if (v.GetType() == type)
            {
                value = (IComponent)v;
            }
        });

        if (value != null)
        {
            SameLine();

            Button("X", $"{name}_CLEAR{key}", () =>
            {
                value = default;
            });
        }

        return value;
    }

    /// <summary>
    /// Creates a table
    /// </summary>
    /// <param name="key">The tablet's unique ID</param>
    /// <param name="rows">The amount of rows</param>
    /// <param name="columns">The amount of columns</param>
    /// <param name="showHeader">Whether to how a header bar</param>
    /// <param name="rowHandler">A callback for when a row is done</param>
    /// <param name="setupColumnHandler">A callback for what header title and width you want for the column</param>
    /// <param name="rowColumnHandler">a callback for a row and column</param>
    /// <param name="rowClickHandler">A callback for when a row is clicked</param>
    public static void Table(string key, int rows, int columns, bool showHeader, Action<int> rowHandler, Func<int, (string, float)> setupColumnHandler,
        Action<int, int> rowColumnHandler, Action<int> rowClickHandler)
    {
        if(ImGui.BeginTable(key, columns))
        {
            for(var i = 0; i < columns; i++)
            {
                var result = ExecuteHandler(setupColumnHandler, $"{key} column  width", i);

                if (result.Item1 != null)
                {
                    if(result.Item2 > 0)
                    {
                        ImGui.TableSetupColumn(result.Item1, ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, result.Item2);
                    }
                    else
                    {
                        ImGui.TableSetupColumn(result.Item1);
                    }
                }
                else
                {
                    ImGui.TableSetupColumn($"{i}");
                }
            }

            if(showHeader)
            {
                ImGui.TableHeadersRow();
            }

            for (var i = 0; i < rows; i++)
            {
                ImGui.TableNextRow();

                ExecuteHandler(rowHandler, $"{key} row {i}", i);

                for (var j = 0; j < columns; j++)
                {
                    ImGui.TableNextColumn();

                    ExecuteHandler(rowColumnHandler, $"{key} row {i} column {j}", i, j);
                }

                SameLine();

                Selectable("", $"{key}.Row{i}", () => rowClickHandler?.Invoke(i), SelectableFlags.SpanAllColumns);
            }

            ImGui.EndTable();
        }
    }
}
