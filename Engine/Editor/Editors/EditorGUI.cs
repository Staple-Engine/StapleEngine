using ImGuiNET;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

/// <summary>
/// GUI functions for custom editors and editor windows
/// </summary>
public static class EditorGUI
{
    internal static ImGuiIOPtr io;
    internal static StapleEditor editor;

    internal static Dictionary<string, object> pendingObjectPickers = new();

    private static readonly Dictionary<string, object> cachedEnumValues = new();

    private static bool changed = false;
    private static ulong counter = 0;

    /// <summary>
    /// Whether the GUI was interacted with this frame
    /// </summary>
    public static bool Changed
    {
        get => changed;

        internal set
        {
            changed = value;

            if(changed == false)
            {
                counter = 0;
            }
            else
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
    /// <returns>Whether the button was clicked</returns>
    public static bool Button(string label)
    {
        return ImGui.Button($"{label}##{counter++}");
    }

    /// <summary>
    /// Shows a disabled button
    /// </summary>
    /// <param name="label">The button label</param>
    /// <returns>Whether the button was clicked</returns>
    public static bool ButtonDisabled(string label)
    {
        ImGui.BeginDisabled();

        var result = ImGui.Button($"{label}##{counter++}");

        ImGui.EndDisabled();

        return result;
    }

    /// <summary>
    /// Shows a text field for an int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static int IntField(string label, int value)
    {
        Changed |= ImGui.InputInt(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a float
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static float FloatField(string label, float value)
    {
        Changed |= ImGui.InputFloat(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector2
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector2 Vector2Field(string label, Vector2 value)
    {
        Changed |= ImGui.InputFloat2(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector2Int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector2Int Vector2IntField(string label, Vector2Int value)
    {
        var values = new int[] { value.X, value.Y };

        Changed |= ImGui.InputInt2(label, ref values[0]);

        return new Vector2Int(values[0], values[1]);
    }

    /// <summary>
    /// Shows a text field for a Vector3
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector3 Vector3Field(string label, Vector3 value)
    {
        Changed |= ImGui.InputFloat3(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a text field for a Vector4
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static Vector4 Vector4Field(string label, Vector4 value)
    {
        Changed |= ImGui.InputFloat4(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static T EnumDropdown<T>(string label, T value) where T: struct, Enum
    {
        if(cachedEnumValues.TryGetValue(typeof(T).FullName, out var v) == false)
        {
            v = Enum.GetValues<T>()
                .ToList();

            cachedEnumValues.Add(typeof(T).FullName, v);
        }

        var values = v as List<T>;

        var current = values.IndexOf(value);

        var valueStrings = values
            .Select(x => x.ToString())
            .ToArray();

        var newValue = values[Dropdown(label, valueStrings, current)];

        return newValue;
    }

    /// <summary>
    /// Shows a dropdown field for an enum
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <param name="values">The valid values for the field</param>
    /// <returns>The new value</returns>
    public static T EnumDropdown<T>(string label, T value, List<T> values) where T : struct, Enum
    {
        var current = values.IndexOf(value);

        var valueStrings = values
            .Select(x => x.ToString())
            .ToArray();

        var newValue = values[Dropdown(label, valueStrings, current)];

        return newValue;
    }

    /// <summary>
    /// Shows a dropdown field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="options">The options for the field</param>
    /// <param name="current">The current value index for the field</param>
    /// <returns>The index of the selected value</returns>
    public static int Dropdown(string label, string[] options, int current)
    {
        Changed |= ImGui.Combo(label, ref current, $"{string.Join("\0", options)}\0");

        if(current < 0)
        {
            current = 0;
        }

        return current;
    }

    /// <summary>
    /// Shows a text field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <returns>The new value</returns>
    public static string TextField(string label, string value, int maxLength = 1000)
    {
        value ??= "";

        Changed |= ImGui.InputText(label, ref value, (uint)maxLength);

        return value;
    }

    /// <summary>
    /// Shows a text field with multiline input
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value</param>
    /// <param name="size">The size the text box should use</param>
    /// <param name="maxLength">The maximum amount of characters</param>
    /// <returns>The new value</returns>
    public static string TextFieldMultiline(string label, string value, Vector2 size, int maxLength = 1000)
    {
        value ??= "";

        Changed |= ImGui.InputTextMultiline(label, ref value, (uint)maxLength, size);

        return value;
    }

    /// <summary>
    /// Shows a toggle
    /// </summary>
    /// <param name="label">The toggle label</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static bool Toggle(string label, bool value)
    {
        Changed |= ImGui.Checkbox(label, ref value);

        return value;
    }

    /// <summary>
    /// Shows a color field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static Color ColorField(string label, Color value)
    {
        var v = new Vector4(value.r, value.g, value.b, value.a);

        Changed |= ImGui.ColorEdit4(label, ref v);

        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Shows a color picker field
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value</param>
    /// <returns>The new value</returns>
    public static Color ColorPicker(string label, Color value)
    {
        var v = new Vector4(value.r, value.g, value.b, value.a);

        Changed |= ImGui.ColorPicker4(label, ref v);

        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Shows an object picker field
    /// </summary>
    /// <param name="type">The type to choose</param>
    /// <param name="name">The name to show for the field</param>
    /// <param name="current">The current value</param>
    /// <returns>The new value</returns>
    public static object ObjectPicker(Type type, string name, object current)
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

        var key = $"{type.FullName}{name}{current}";

        if (ImGui.SmallButton($"O##{key}"))
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
        if(texture == null)
        {
            return;
        }

        ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size);
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
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static int IntSlider(string label, int value, int min, int max)
    {
        Changed |= ImGui.SliderInt(label, ref value, min, max);

        return value;
    }

    /// <summary>
    /// Shows a slider for a float value
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static float FloatSlider(string label, float value, float min, float max)
    {
        Changed |= ImGui.SliderFloat(label, ref value, min, max);

        return value;
    }
}
