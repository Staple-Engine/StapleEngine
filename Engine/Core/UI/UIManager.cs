using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;

namespace Staple.UI;

public sealed class UIManager
{
    private static readonly string DefaultSkinPath = "Hidden/UI/DefaultSkin.json";

    private class InputObserverProxy(UIManager owner) : IInputObserver
    {
        public UIManager owner = owner;

        public void OnCharacterEntered(char character)
        {
            owner.OnCharacterEntered(character);
        }

        public void OnKeyJustPressed(KeyCode key)
        {
            owner.OnKeyJustPressed(key);
        }

        public void OnKeyPressed(KeyCode key)
        {
            owner.OnKeyPressed(key);
        }

        public void OnKeyReleased(KeyCode key)
        {
            owner.OnKeyReleased(key);
        }

        public void OnMouseButtonJustPressed(MouseButton button)
        {
            owner.OnMouseJustPressed(button);
        }

        public void OnMouseButtonPressed(MouseButton button)
        {
            owner.OnMousePressed(button);
        }

        public void OnMouseButtonReleased(MouseButton button)
        {
            owner.OnMouseReleased(button);
        }

        public void OnMouseMove(Vector2Int position)
        {
            owner.OnMouseMove(position);
        }

        public void OnMouseWheelScrolled(Vector2 delta)
        {
        }
    }

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

    private readonly InputObserverProxy inputProxy;

    public readonly ushort ViewID = UICanvasSystem.UIViewID;

    public Color DefaultFontColor { get; private set; }

    public Color DefaultSecondaryFontColor { get; private set; }

    public int DefaultFontSize { get; private set; }

    public UIPanel FocusedElement { get; private set; }

    public UIMenu CurrentMenu { get; private set; }

    public UIMenuBar CurrentMenuBar { get; private set; }

    public UIPanel MouseOverElement { get; private set; }

    public Vector2Int CanvasSize { get; internal set; }

    public UIManager()
    {
        var skinGuid = AssetDatabase.GetAssetGuid(DefaultSkinPath);

        if(skinGuid != null)
        {
            LoadSkin(skinGuid);
        }

        inputProxy = new(this);

        Input.RegisterInputObserver(inputProxy);
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
                RecursiveFindFocusedElement(parentPosition + parent.Position - parent.Translation + parent.ChildOffset, child, ref foundElement);
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

            return;
        }

        if(skin != null)
        {
            DefaultFontColor = skin.GetColor("General", "DefaultFontColor");
            DefaultSecondaryFontColor = skin.GetColor("General", "DefaultSecondaryFontColor");
            DefaultFontSize = skin.GetInt("General", "DefaultFontSize");
        }
    }

    public void Update()
    {
        UpdateDrawOrderCache();
        UpdateDrawOrderCache();

        UIPanel foundElement = null;
        UIPanel inputBlocker = GetInputBlocker();

        if (inputBlocker != null)
        {
            RecursiveFindFocusedElement(Vector2Int.Zero, inputBlocker, ref foundElement);
        }
        else
        {
            for (var i = drawOrderCounter; i >= 0; i--)
            {
                for (var j = 0; j < drawOrderCache.Count; j++)
                {
                    var p = drawOrderCache[j];

                    if (p.panel.AllowMouseInput == false || p.drawOrder != i)
                    {
                        continue;
                    }

                    RecursiveFindFocusedElement(Vector2Int.Zero, p.panel, ref foundElement);

                    if (foundElement != null)
                    {
                        break;
                    }
                }

                if (foundElement != null)
                {
                    break;
                }
            }
        }

        MouseOverElement = foundElement;

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

    public UIPanel CreateElement(string typeName, string ID)
    {
        try
        {
            var type = TypeCache.GetType(typeName) ?? TypeCache.GetType($"Staple.UI.{typeName}");

            if(type == null)
            {
                Log.Debug($"Failed to create UI Panel: Type {typeName}/Staple.UI.{typeName} not found");

                return null;
            }

            if(type.IsAssignableTo(typeof(UIPanel)) == false)
            {
                Log.Debug($"Failed to create UI Panel: Type {typeName}/Staple.UI.{typeName} is not a UIPanel");

                return null;
            }

            var instance = (UIPanel)Activator.CreateInstance(type, [this, ID]);

            if (instance != null && AddElement(instance))
            {
                return instance;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public T CreateElement<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        T>(string ID) where T: UIPanel
    {
        try
        {
            var instance = (T)Activator.CreateInstance(typeof(T), [this, ID]);

            if(instance != null && AddElement(instance))
            {
                return instance;
            }

            return null;
        }
        catch(Exception)
        {
            return null;
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

    internal void OnMouseMove(Vector2Int position)
    {
        if(FocusedElement != null)
        {
            FocusedElement.OnMouseMoveInternal(position);

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

    public bool AddElement(UIPanel element)
    {
        if(element == null)
        {
            return false;
        }

        drawOrderCacheDirty = true;

        if(elements.TryGetValue(element.ID, out var e))
        {
            Log.Debug($"Failed to add element {element.ID}: Duplicate ID!");

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

        elements.Add(element.ID, e);

        return true;
    }

    public void RemoveElement(string ID)
    {
        drawOrderCacheDirty = true;

        elements.Remove(ID);
    }

    public bool LoadLayouts(string data, UIPanel parent = null)
    {
        try
        {
            var layout = JsonSerializer.Deserialize(data, UILayoutSerializationContext.Default.UILayout);

            if(layout == null)
            {
                return false;
            }

            void Recursive(string layoutName, Dictionary<string, UILayout.UIPanelData> panels, UIPanel parent)
            {
                foreach (var pair in panels)
                {
                    if(pair.Value == null)
                    {
                        continue;
                    }

                    var elementID = $"{layoutName}.{pair.Key}";

                    if(elements.ContainsKey(elementID))
                    {
                        continue;
                    }

                    var panelData = pair.Value;

                    var element = CreateElement(panelData.control ?? "", elementID);

                    if(element == null)
                    {
                        continue;
                    }

                    element.Parent = parent;
                    element.Visible = panelData.visible;

                    var position = Vector2Int.Zero;
                    var size = element.DefaultSize;

                    {
                        if (panelData.x != null && int.TryParse(panelData.x, out var v))
                        {
                            position.X = v;
                        }
                    }

                    {
                        if (panelData.y != null && int.TryParse(panelData.y, out var v))
                        {
                            position.Y = v;
                        }
                    }

                    {
                        if (panelData.wide != null && int.TryParse(panelData.wide, out var v))
                        {
                            size.X = v;
                        }
                    }

                    {
                        if (panelData.tall != null && int.TryParse(panelData.tall, out var v))
                        {
                            size.Y = v;
                        }
                    }

                    element.Position = position;
                    element.Size = size;

                    if((panelData.properties?.Count ?? 0) > 0)
                    {
                        element.ApplyLayoutProperties(panelData.properties);
                    }

                    Recursive(elementID, pair.Value.children, element);
                }
            }

            foreach (var layoutPair in layout.data)
            {
                if(layoutPair.Value == null)
                {
                    continue;
                }

                Recursive(layoutPair.Key, layoutPair.Value, parent);
            }

            return true;
        }
        catch(Exception e)
        {
            Log.Error($"Failed to load UI Layout: {e}");

            return false;
        }

        //TODO
        return false;
    }

    public void ClearLayouts()
    {
        layouts.Clear();
    }

    public UIPanel GetElement(string ID) => elements.TryGetValue(ID, out var e) ? e.panel : null;

    public T GetElement<T>(string ID) where T : UIPanel => elements.TryGetValue(ID, out var e) ? e.panel as T : null;

    public UIMenu CreateMenu(Vector2Int position)
    {
        if(CurrentMenu != null)
        {
            RemoveElement(CurrentMenu.ID);
        }

        CurrentMenu = CreateElement<UIMenu>("UIManager.CurrentMenu");

        if(CurrentMenu != null)
        {
            CurrentMenu.Position = position;
        }

        return CurrentMenu;
    }

    public UIMenuBar CreateMenuBar()
    {
        if(CurrentMenuBar != null)
        {
            RemoveElement(CurrentMenuBar.ID);
        }

        CurrentMenuBar = CreateElement<UIMenuBar>("UIManager.CurrentMenuBar");

        return CurrentMenuBar;
    }

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
