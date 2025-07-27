using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.UI;

public partial class UIPanel
{
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
