using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace Staple.UI;

public sealed class UIManager
{
    private static readonly string DefaultSkinPath = "Hidden/UI/DefaultSkin.json";

    internal class Element
    {
        public UIPanel panel;
        public int drawOrder;
    }

    internal int drawOrderCounter;

    internal readonly Dictionary<string, Element> elements = [];

    internal readonly Dictionary<string, UILayout> layouts = [];

    private readonly List<Element> drawOrderCache = [];

    private bool drawOrderCacheDirty = true;

    private UISkin skin = new();

    public readonly ushort ViewID = UICanvasSystem.UIViewID;

    public Color DefaultFontColor { get; private set; }

    public Color DefaultSecondaryFontColor { get; private set; }

    public int DefaultFontSize { get; private set; }

    public UIPanel FocusedElement { get; private set; }

    public UIPanel MouseOverElement
    {
        get
        {
            UpdateDrawOrderCache();

            UIPanel foundElement = null;
            UIPanel inputBlocker = GetInputBlocker();

            if(inputBlocker != null)
            {
                RecursiveFindFocusedElement(Vector2Int.Zero, inputBlocker, ref foundElement);
            }
            else
            {
                for(var i = drawOrderCounter; i >= 0; i--)
                {
                    for(var j = 0; j < drawOrderCache.Count; j++)
                    {
                        var p = drawOrderCache[j];

                        if(p.panel.AllowMouseInput == false || p.drawOrder != i)
                        {
                            continue;
                        }

                        RecursiveFindFocusedElement(Vector2Int.Zero, p.panel, ref foundElement);

                        if(foundElement != null)
                        {
                            break;
                        }
                    }

                    if(foundElement != null)
                    {
                        break;
                    }
                }
            }

            return foundElement;
        }
    }

    public Vector2Int CanvasSize { get; internal set; }

    public UIManager()
    {
        var skinGuid = AssetDatabase.GetAssetGuid(DefaultSkinPath);

        if(skinGuid != null)
        {
            LoadSkin(skinGuid);
        }
    }

    internal void RecursiveFindFocusedElement(Vector2Int parentPosition, UIPanel parent, ref UIPanel foundElement)
    {
        if(parent.Visible == false || parent.Enabled == false || parent.AllowMouseInput == false)
        {
            foundElement = null;

            return;
        }

        var min = parentPosition + parent.Position;
        var max = min + parent.Size;

        var aabb = AABB.CreateFromMinMax(new Vector3(min, 0), new Vector3(max, 0));

        if (aabb.Contains(new(Input.PointerPosition, 0)) && parent.AllowMouseInput)
        {
            foundElement = parent;

            foreach (var child in parent.Children)
            {
                RecursiveFindFocusedElement(parentPosition + parent.Position, child, ref foundElement);
            }
        }
    }

    private void UpdateDrawOrderCache()
    {
        if (drawOrderCacheDirty)
        {
            drawOrderCacheDirty = false;

            drawOrderCache.Clear();

            foreach (var pair in elements)
            {
                if (pair.Value.panel.parent == null)
                {
                    drawOrderCache.Add(pair.Value);
                }
            }
        }
    }

    private UIPanel GetInputBlocker()
    {
        foreach (var pair in elements)
        {
            if (pair.Value.panel.BlockingInput && pair.Value.panel.Visible)
            {
                return pair.Value.panel;
            }
        }

        return null;
    }

    public void LoadSkin(string guid)
    {
        var skinData = ResourceManager.instance.LoadTextAsset(guid);

        if(string.IsNullOrEmpty(skinData?.text))
        {
            return;
        }

        try
        {
            skin = JsonSerializer.Deserialize(skinData.text, UISkinSerializationContext.Default.UISkin);
        }
        catch(Exception e)
        {
            Log.Error($"Failed to load UI skin {guid}: {e}");
        }
    }

    public void Update()
    {
        UpdateDrawOrderCache();

        for (var i = 0; i < drawOrderCache.Count; i++)
        {
            var p = drawOrderCache[i];

            if(p.panel.Visible)
            {
                p.panel.Update(Vector2Int.Zero);
            }
        }
    }

    public void Draw()
    {
        UpdateDrawOrderCache();

        var inputBlocker = GetInputBlocker();

        for(var i = 0; i <= drawOrderCounter; i++)
        {
            for(var j = 0; j < drawOrderCache.Count; j++)
            {
                var p = drawOrderCache[j];

                if(p.drawOrder != i || p.panel.Visible == false)
                {
                    continue;
                }

                if(p.panel == inputBlocker)
                {
                    var material = SpriteRenderSystem.DefaultMaterial.Value;

                    var color = material.MainColor;

                    material.MainColor = new(0, 0, 0, 0.3f);

                    MeshRenderSystem.RenderMesh(Mesh.Quad, new Vector3(CanvasSize.X / 2, CanvasSize.Y / 2, 0),
                        Quaternion.Identity, new Vector3(CanvasSize, 0), material, MaterialLighting.Unlit, ViewID);

                    material.MainColor = color;
                }

                p.panel.Draw(Vector2Int.Zero);
            }
        }
    }

    internal void OnMouseJustPressed(MouseButton button)
    {
        UpdateDrawOrderCache();

        if(button == MouseButton.Left)
        {
            var previousFocus = FocusedElement;

            FocusedElement = null;

            UIPanel foundElement = null;

            var inputBlocker = GetInputBlocker();

            if(inputBlocker != null)
            {
                RecursiveFindFocusedElement(Vector2Int.Zero, inputBlocker, ref foundElement);
            }
            else
            {
                for(var i = drawOrderCounter; i >= 0; i--)
                {
                    foreach(var p in drawOrderCache)
                    {
                        if(p.panel.AllowMouseInput == false || p.drawOrder != i)
                        {
                            continue;
                        }

                        RecursiveFindFocusedElement(Vector2.Zero, p.panel, ref foundElement);

                        if(foundElement != null)
                        {
                            break;
                        }
                    }

                    if(foundElement != null)
                    {
                        break;
                    }
                }
            }

            if(foundElement != null)
            {
                FocusedElement = foundElement;
            }

            if(previousFocus != null && previousFocus != FocusedElement)
            {
                previousFocus.OnLoseFocusInternal();
            }

            FocusedElement?.OnGainFocusInternal();
        }

        if(FocusedElement != null)
        {
            FocusedElement.OnMouseJustPressedInternal(button);

            //TODO: Consume Input
        }
    }

    internal void OnMousePressed(MouseButton button)
    {
        if(FocusedElement != null)
        {
            FocusedElement.OnMousePressedInternal(button);

            //TODO: Consume Input
        }
    }

    internal void OnMouseReleased(MouseButton button)
    {
        if (FocusedElement != null)
        {
            FocusedElement.OnMouseReleasedInternal(button);

            //TODO: Consume Input
        }
    }

    internal void OnMouseMove()
    {
        if(FocusedElement != null)
        {
            FocusedElement.OnMouseMoveInternal();

            //TODO: Consume Input
        }

        //TODO: Tooltips
    }

    internal void OnKeyJustPressed(KeyCode key)
    {
        if (FocusedElement != null)
        {
            FocusedElement.OnKeyJustPressedInternal(key);

            //TODO: Consume Input
        }
    }

    internal void OnKeyPressed(KeyCode key)
    {
        if (FocusedElement != null)
        {
            FocusedElement.OnKeyPressedInternal(key);

            //TODO: Consume Input
        }
    }

    internal void OnKeyReleased(KeyCode key)
    {
        if (FocusedElement != null)
        {
            FocusedElement.OnKeyReleasedInternal(key);

            //TODO: Consume Input
        }
    }

    internal void OnCharacterEntered(char character)
    {
        if (FocusedElement != null)
        {
            FocusedElement.OnCharacterEnteredInternal(character);

            //TODO: Consume Input
        }
    }

    public bool AddElement(string ID, UIPanel element)
    {
        drawOrderCacheDirty = true;

        if(elements.TryGetValue(ID, out var e))
        {
            Log.Debug($"Failed to add element {ID}: Duplicate ID!");

            return false;
        }

        if(element.Manager != this)
        {
            return false;
        }

        e = new()
        {
            panel = element,
            drawOrder = ++drawOrderCounter,
        };

        element.SetSkin(skin);

        elements.Add(ID, e);

        return true;
    }

    public void RemoveElement(string ID)
    {
        drawOrderCacheDirty = true;

        elements.Remove(ID);
    }

    public bool LoadLayouts(string data, UIPanel parent = null)
    {
        //TODO
        return false;
    }

    public void ClearLayouts()
    {
        layouts.Clear();
    }

    public UIPanel GetElement(string ID) => elements.TryGetValue(ID, out var e) ? e.panel : null;

    public void Clear()
    {
        drawOrderCacheDirty = true;

        elements.Clear();

        FocusedElement = null;
    }

    public void ClearFocus()
    {
        FocusedElement = null;
    }
}
