using Staple.Internal;

namespace Staple;

/// <summary>
/// Mouse Cursor management class
/// </summary>
public static class Cursor
{
    internal static IRenderWindow window;

    private static CursorLockMode lockState = CursorLockMode.None;

    /// <summary>
    /// The current lock state of the cursor
    /// </summary>
    public static CursorLockMode LockState
    {
        get => lockState;

        set
        {
            lockState = value;

            switch(value)
            {
                case CursorLockMode.None:

                    UnlockCursor();

                    break;

                case CursorLockMode.Locked:

                    LockCursor();

                    break;
            }
        }
    }

    internal static bool visible = true;

    /// <summary>
    /// Whether the cursor is visible
    /// </summary>
    public static bool Visible
    {
        get => visible;

        set
        {
            visible = value;

            if(visible)
            {
                ShowCursor();
            }
            else
            {
                HideCursor();
            }
        }
    }

    /// <summary>
    /// Locks the cursor to the window
    /// </summary>
    internal static void LockCursor()
    {
        window.LockCursor();
    }

    /// <summary>
    /// Unlocks the cursor
    /// </summary>
    internal static void UnlockCursor()
    {
        window.UnlockCursor();
    }

    /// <summary>
    /// Hides the cursor
    /// </summary>
    internal static void HideCursor()
    {
        window.HideCursor();
    }

    /// <summary>
    /// Shows the cursor
    /// </summary>
    internal static void ShowCursor()
    {
        window.ShowCursor();
    }

    /// <summary>
    /// Sets the mouse cursor to a specific image
    /// </summary>
    /// <param name="pixels">The image pixels</param>
    /// <param name="width">The width of the image</param>
    /// <param name="height">The height of the image</param>
    /// <param name="hotX">The x position for the cursor hotspot</param>
    /// <param name="hotY">The y position for the cursor hotspot</param>
    public static bool TryCreateCursorImage(Color32[] pixels, int width, int height, int hotX, int hotY, out CursorImage image)
    {
        if(pixels.Length != width * height)
        {
            image = default;

            return false;
        }

        return window.TryCreateCursorImage(pixels, width, height, hotX, hotY, out image);
    }

    /// <summary>
    /// Changes the current cursor
    /// </summary>
    /// <param name="image">The cursor image. If it's null, resets to the arrow cursor</param>
    public static void SetCursor(CursorImage image)
    {
        window.SetCursor(image);
    }
}
