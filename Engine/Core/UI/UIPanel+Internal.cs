using Staple.Internal;
using System.Collections.Generic;

namespace Staple.UI;

public partial class UIPanel
{
    internal UIPanel parent;

    internal HashSet<UIPanel> children = [];

    /// <summary>
    /// Timer for checking when to consider a click
    /// </summary>
    internal float clickTimer;

    /// <summary>
    /// Whether the mouse is currently pressed on this element
    /// </summary>
    internal bool clickPressed;

    /// <summary>
    /// The ID of this element
    /// </summary>
    internal readonly string ID;

    /// <summary>
    /// Cached ninepatch vertices
    /// </summary>
    private SpriteRenderSystem.SpriteVertex[] ninePatchVertices = [];

    /// <summary>
    /// Cached ninepatch indices
    /// </summary>
    private uint[] ninePatchIndices = [];

    /// <summary>
    /// Global material used for rendering
    /// </summary>
    private static Material material;

    /// <summary>
    /// Cached normal sprite vertices
    /// </summary>
    private static readonly SpriteRenderSystem.SpriteVertex[] vertices = new SpriteRenderSystem.SpriteVertex[4];

    /// <summary>
    /// Cached normal sprite indices
    /// </summary>
    private static readonly ushort[] indices = [0, 1, 2, 2, 3, 0];

    /// <summary>
    /// Cached text vertices when rendering text
    /// </summary>
    private TextRenderer.PosTexVertex[] textVertices = [];

    /// <summary>
    /// Cached text indices when rendering text
    /// </summary>
    private ushort[] textIndices = [];

    /// <summary>
    /// Adds a child to this panel
    /// </summary>
    /// <param name="child">The child panel</param>
    private void AddChild(UIPanel child)
    {
        if (child == null)
        {
            return;
        }

        child.parent?.RemoveChild(child);

        children.Add(child);

        if (child.parent != this)
        {
            child.parent = this;
        }
    }

    /// <summary>
    /// Removes a child from this panel
    /// </summary>
    /// <param name="child">The child to remove</param>
    private void RemoveChild(UIPanel child)
    {
        if (child != null)
        {
            child.parent = null;
        }

        children.Remove(child);
    }

    internal void OnMouseJustPressedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        if (button == MouseButton.Left)
        {
            clickTimer = Time.time;
            clickPressed = true;
        }

        OnMouseJustPressed(button);

        OnMouseJustPressedHandler?.Invoke(this, button);
    }

    internal void OnMousePressedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMousePressed(button);

        OnMousePressedHandler?.Invoke(this, button);
    }

    internal void OnMouseReleasedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        if (button == MouseButton.Left)
        {
            clickPressed = false;

            if (Time.time - clickTimer < 0.5f)
            {
                OnClick();

                OnClickHandler?.Invoke(this);
            }
        }

        OnMouseReleased(button);

        OnMouseReleasedHandler?.Invoke(this, button);
    }

    internal void OnMouseMoveInternal(Vector2Int position)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMouseMove(position);

        OnMouseMoveHandler?.Invoke(this, position);
    }

    internal void OnKeyJustPressedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyJustPressed(key);

        OnKeyJustPressedHandler?.Invoke(this, key);
    }

    internal void OnKeyPressedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyPressed(key);

        OnKeyPressedHandler?.Invoke(this, key);
    }

    internal void OnKeyReleasedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyReleased(key);

        OnKeyReleasedHandler?.Invoke(this, key);
    }

    internal void OnCharacterEnteredInternal(char character)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnCharacterEntered(character);

        OnCharacterEnteredHandler?.Invoke(this, character);
    }

    internal void OnLoseFocusInternal()
    {
        OnLoseFocus();

        OnLoseFocusHandler?.Invoke(this);
    }

    internal void OnGainFocusInternal()
    {
        OnGainFocus();

        OnGainFocusHandler?.Invoke(this);
    }
}
