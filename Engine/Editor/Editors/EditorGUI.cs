using Hexa.NET.ImGui;
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

    private static string MakeIdentifier(string identifier)
    {
        return $"{identifier}##{counter++}";
    }

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

    private static void ExecuteHandler(Action handler, string label)
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
        counter = 0;
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
    /// <param name="handler">A handler to execute if the button is clicked</param>
    /// <returns>Whether the button was clicked</returns>
    public static void Button(string label, Action handler)
    {
        if(ImGui.Button(MakeIdentifier(label)))
        {
            ExecuteHandler(handler, $"Button {label}");
        }
    }

    /// <summary>
    /// Shows a disabled button
    /// </summary>
    /// <param name="label">The button label</param>
    /// <param name="handler">A handler to execute if the button is clicked</param>
    /// <returns>Whether the button was clicked</returns>
    public static void ButtonDisabled(string label, Action handler)
    {
        ImGui.BeginDisabled();

        if(ImGui.Button(MakeIdentifier(label)))
        {
            ExecuteHandler(handler, $"ButtonDisabled {label}");
        }

        ImGui.EndDisabled();
    }

    /// <summary>
    /// Shows a text field for an int
    /// </summary>
    /// <param name="label">The label for the field</param>
    /// <param name="value">The current value of the field</param>
    /// <returns>The new value</returns>
    public static int IntField(string label, int value)
    {
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputInt(MakeIdentifier(""), ref value);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat(MakeIdentifier(""), ref value);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat2(MakeIdentifier(""), ref value);
        });

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

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputInt2(MakeIdentifier(""), ref values[0]);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat3(MakeIdentifier(""), ref value);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputFloat4(MakeIdentifier(""), ref value);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.Combo(MakeIdentifier(""), ref current, $"{string.Join("\0", options)}\0");
        });

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

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputText(MakeIdentifier(""), ref value, (uint)maxLength);
        });

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

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.InputTextMultiline(MakeIdentifier(""), ref value, (uint)maxLength, size);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.Checkbox(MakeIdentifier(""), ref value);
        });

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

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.ColorEdit4(MakeIdentifier(""), ref v);
        });

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

        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.ColorPicker4(MakeIdentifier(""), ref v);
        });

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

        if (ImGui.SmallButton(MakeIdentifier("O")))
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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.SliderInt(MakeIdentifier(""), ref value, min, max);
        });

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
        Changed |= IdentifierColumns(label, () =>
        {
            return ImGui.SliderFloat(MakeIdentifier(""), ref value, min, max);
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
    /// <param name="leaf">Whether it's a leaf (doesn't open on click, no arrow)</param>
    /// <param name="spanFullWidth">Whether to use the full width</param>
    /// <param name="handler">A handler for when it is clicked or is open</param>
    /// <param name="prefixHandler">A handler to run regardless of the node being open</param>
    public static void TreeNode(string label, bool leaf, bool spanFullWidth, Action handler, Action prefixHandler = null)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if(spanFullWidth)
        {
            flags |= ImGuiTreeNodeFlags.SpanFullWidth;
        }

        if (leaf)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }

        var isOpen = ImGui.TreeNodeEx(MakeIdentifier(label), flags);

        ExecuteHandler(prefixHandler, $"TreeNode {label} prefix");

        if(isOpen)
        {
            ExecuteHandler(handler, $"TreeNode {label}");

            if(leaf == false)
            {
                ImGui.TreePop();
            }
        }
    }

    /// <summary>
    /// Creates a tab bar
    /// </summary>
    /// <param name="titles">The tab titles</param>
    /// <param name="handler">A handler with the tab index to render</param>
    public static void TabBar(string[] titles, Action<int> handler)
    {
        if(ImGui.BeginTabBar(MakeIdentifier("")))
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
    /// <param name="handler">A handler called if the menu is used</param>
    public static void Menu(string name, Action handler)
    {
        if(ImGui.BeginMenu(MakeIdentifier(name)))
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
    public static void MenuItem(string name, Action handler)
    {
        if(ImGui.MenuItem(MakeIdentifier(name)))
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
}
