using System;

namespace Staple.Editor;

/// <summary>
/// Represents an editor window.
/// Use this to create custom windows for the editor
/// </summary>
public abstract class EditorWindow
{
    /// <summary>
    /// The window title
    /// </summary>
    public string title;

    /// <summary>
    /// Flags for the window's presentation
    /// </summary>
    public EditorWindowFlags windowFlags = EditorWindowFlags.Resizable | EditorWindowFlags.Dockable;

    /// <summary>
    /// The window type
    /// </summary>
    public EditorWindowType windowType = EditorWindowType.Normal;

    /// <summary>
    /// The window's size
    /// </summary>
    public Vector2Int size = new(600, 400);

    internal bool opened = false;

    /// <summary>
    /// Override this to add content using EditorGUI
    /// </summary>
    public abstract void OnGUI();

    /// <summary>
    /// Closes this window
    /// </summary>
    public void Close()
    {
        StapleEditor.instance.editorWindows.Remove(this);
    }

    /// <summary>
    /// Attempts to get an existing window if it's open
    /// </summary>
    /// <typeparam name="T">The window type</typeparam>
    /// <param name="window">The result if true</param>
    /// <returns>Whether the window was found</returns>
    public static bool TryGetWindow<T>(out T window) where T : EditorWindow
    {
        foreach (var w in StapleEditor.instance.editorWindows)
        {
            if (w != null && w.GetType() == typeof(T))
            {
                window = (T)w;

                return true;
            }
        }

        window = default;

        return false;
    }

    /// <summary>
    /// Attempts to create a window. If an existing window of the same type exists, it'll show that one instead.
    /// </summary>
    /// <typeparam name="T">The editor window type</typeparam>
    /// <returns>The window, or null</returns>
    public static T GetWindow<T>() where T : EditorWindow, new()
    {
        if(TryGetWindow<T>(out var w))
        {
            return w;
        }

        try
        {
            var result = new T();

            if (result == null)
            {
                return null;
            }

            result.title ??= typeof(T).Name;

            StapleEditor.instance.editorWindows.Add(result);

            return result;
        }
        catch(Exception)
        {
            return null;
        }
    }
}
