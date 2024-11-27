using Staple.Internal;

namespace Staple;

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
}
