using System;
using System.Collections.Generic;

namespace Staple.UI;

/// <summary>
/// Base class for UI elements
/// </summary>
public abstract partial class UIPanel
{
    /// <summary>
    /// The UI manager that owns this panel
    /// </summary>
    public UIManager Manager { get; internal set; }

    /// <summary>
    /// Whether this panel is visible
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether this panel is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this panel allows mouse input
    /// </summary>
    public bool AllowMouseInput { get; set; } = true;

    /// <summary>
    /// Whether this panel allows keyboard input
    /// </summary>
    public bool AllowKeyboardInput { get; set; } = true;

    /// <summary>
    /// The position of this panel
    /// </summary>
    public Vector2Int Position { get; set; }

    /// <summary>
    /// The size of this panel
    /// </summary>
    public Vector2Int Size { get; set; }

    /// <summary>
    /// The default size of this panel, used for layouts
    /// </summary>
    public Vector2Int DefaultSize { get; set; }

    /// <summary>
    /// The translation for child panels
    /// </summary>
    public Vector2Int Translation { get; set; }

    /// <summary>
    /// Extra size for this panel
    /// </summary>
    public Vector2Int SelectBoxExtraSize { get; set; }

    /// <summary>
    /// Offset for how children should update or draw
    /// </summary>
    public Vector2Int ChildOffset { get; protected set; }

    /// <summary>
    /// The alpha transparency of this element
    /// </summary>
    public float LocalAlpha { get; set; } = 1;

    /// <summary>
    /// The alpha transparency of this element with the parents combined
    /// </summary>
    public float Alpha
    {
        get
        {
            var alpha = LocalAlpha;

            if(parent != null)
            {
                alpha *= parent.Alpha;
            }

            return alpha;
        }
    }

    /// <summary>
    /// Whether this handles tooltips
    /// </summary>
    public bool ShouldRespondToTooltips {  get; set; }

    /// <summary>
    /// The tooltip value
    /// </summary>
    public string Tooltip { get; set; }

    /// <summary>
    /// The parent panel
    /// </summary>
    public UIPanel Parent
    {
        get => parent;

        set
        {
            parent?.RemoveChild(this);

            parent = value;

            parent?.AddChild(this);
        }
    }

    /// <summary>
    /// A list of all children this panel has
    /// </summary>
    public IEnumerable<UIPanel> Children => children;

    /// <summary>
    /// Whether this blocks input
    /// </summary>
    public bool BlockingInput { get; protected set; }

    /// <summary>
    /// Whether this panel currently responds to tooltips
    /// </summary>
    public bool RespondsToTooltips => ShouldRespondToTooltips && string.IsNullOrEmpty(Tooltip);

    /// <summary>
    /// On Click event
    /// </summary>
    public Action<UIPanel> OnClickHandler;

    /// <summary>
    /// Called when this panel loses focus
    /// </summary>
    public Action<UIPanel> OnLoseFocusHandler;

    /// <summary>
    /// Called when this panel gains focus
    /// </summary>
    public Action<UIPanel> OnGainFocusHandler;

    /// <summary>
    /// Called when a character is typed while this panel is focused
    /// </summary>
    public Action<UIPanel, char> OnCharacterEnteredHandler;

    /// <summary>
    /// Called when the mouse is moved while this panel is focused
    /// </summary>
    public Action<UIPanel, Vector2Int> OnMouseMoveHandler;

    /// <summary>
    /// Called when a mouse button was just pressed while this panel is focused
    /// </summary>
    public Action<UIPanel, MouseButton> OnMouseJustPressedHandler;

    /// <summary>
    /// Called when a mouse button was pressed while this panel is focused
    /// </summary>
    public Action<UIPanel, MouseButton> OnMousePressedHandler;

    /// <summary>
    /// Called when a mouse button was just released while this panel is focused
    /// </summary>
    public Action<UIPanel, MouseButton> OnMouseReleasedHandler;

    /// <summary>
    /// Called when a key was just pressed while this panel is focused
    /// </summary>
    public Action<UIPanel, KeyCode> OnKeyJustPressedHandler;

    /// <summary>
    /// Called when a key was pressed while this panel is focused
    /// </summary>
    public Action<UIPanel, KeyCode> OnKeyPressedHandler;

    /// <summary>
    /// Called when a key was just released while this panel is focused
    /// </summary>
    public Action<UIPanel, KeyCode> OnKeyReleasedHandler;

    /// <summary>
    /// Finds the global position of this element
    /// </summary>
    public Vector2Int GlobalPosition
    {
        get
        {
            if(parent == null)
            {
                return Vector2Int.Zero;
            }

            var position = Vector2Int.Zero;

            var p = parent;

            while(p != null)
            {
                position += p.Position;

                p = p.parent;
            }

            return position;
        }
    }

    /// <summary>
    /// Gets the size of all children in this panel
    /// </summary>
    public Vector2Int ChildrenSize
    {
        get
        {
            var outValue = Vector2Int.Zero;

            foreach(var child in children)
            {
                if(!child.Visible)
                {
                    continue;
                }

                child.PerformLayout();

                var max = child.Position + child.Size;

                if(outValue.X < max.X)
                {
                    outValue.X = max.X;
                }

                if (outValue.Y < max.Y)
                {
                    outValue.Y = max.Y;
                }
            }

            return outValue;
        }
    }

    public UIPanel(UIManager manager, string ID)
    {
        this.ID = ID;

        Manager = manager;

        OnConstructed();
    }

    /// <summary>
    /// Called when this element has just been constructed
    /// </summary>
    protected virtual void OnConstructed()
    {
    }

    /// <summary>
    /// Checks if this element is not visible in the screen
    /// </summary>
    /// <param name="position">The element's position</param>
    /// <returns>Whether this element should not be rendered</returns>
    protected bool IsCulled(Vector2Int position)
    {
        return !Visible ||
            Alpha == 0 ||
            position.X + Size.X < 0 ||
            position.Y + Size.Y < 0 ||
            position.X > Manager.CanvasSize.X ||
            position.Y > Manager.CanvasSize.Y;
    }

    /// <summary>
    /// Calculates the layout of this panel and its children
    /// </summary>
    protected virtual void PerformLayout()
    {
        foreach (var child in children)
        {
            child.PerformLayout();
        }
    }

    /// <summary>
    /// Applies a UI Skin to this panel
    /// </summary>
    /// <param name="skin">The UI Skin</param>
    public abstract void SetSkin(UISkin skin);

    /// <summary>
    /// Applies properties from a layout, if any. These properties likely have <see cref="System.Text.Json"/> types!
    /// </summary>
    /// <param name="properties">The layout properties</param>
    public abstract void ApplyLayoutProperties(Dictionary<string, object> properties);

    /// <summary>
    /// Handles update logic to this panel
    /// </summary>
    /// <param name="parentPosition">The position of the parent element</param>
    public abstract void Update(Vector2Int parentPosition);

    /// <summary>
    /// Draws this panel
    /// </summary>
    /// <param name="parentPosition">The position of the parent element</param>
    public abstract void Draw(Vector2Int parentPosition);

    /// <summary>
    /// Called when this panel is clicked
    /// </summary>
    protected virtual void OnClick()
    {
    }

    /// <summary>
    /// Called when a mouse button was just pressed on this panel
    /// </summary>
    /// <param name="button">The button</param>
    protected virtual void OnMouseJustPressed(MouseButton button)
    {
    }

    /// <summary>
    /// Called when a mouse button was pressed on this panel
    /// </summary>
    /// <param name="button">The button</param>
    protected virtual void OnMousePressed(MouseButton button)
    {
    }

    /// <summary>
    /// Called when a mouse button was just released on this panel
    /// </summary>
    /// <param name="button">The button</param>
    protected virtual void OnMouseReleased(MouseButton button)
    {
    }

    /// <summary>
    /// Called when the mouse was moved while this panel is focused
    /// </summary>
    /// <param name="position">The mouse's current position</param>
    protected virtual void OnMouseMove(Vector2Int position)
    {
    }

    /// <summary>
    /// Called when a key was just pressed on this panel
    /// </summary>
    /// <param name="key">The key</param>
    protected virtual void OnKeyJustPressed(KeyCode key)
    {
    }

    /// <summary>
    /// Called when a key was pressed on this panel
    /// </summary>
    /// <param name="key">The key</param>
    protected virtual void OnKeyPressed(KeyCode key)
    {
    }

    /// <summary>
    /// Called when a key was just released on this panel
    /// </summary>
    /// <param name="key">The key</param>
    protected virtual void OnKeyReleased(KeyCode key)
    {
    }

    /// <summary>
    /// Called when a character was typed onto this panel
    /// </summary>
    /// <param name="character">The character</param>
    protected virtual void OnCharacterEntered(char character)
    {
    }

    /// <summary>
    /// Called when this element stops being focused
    /// </summary>
    protected virtual void OnLoseFocus()
    {
    }

    /// <summary>
    /// Called when this element gains focus
    /// </summary>
    protected virtual void OnGainFocus()
    {
    }
}
