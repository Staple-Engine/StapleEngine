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
    /// Attempts to create a window. If an existing window of the same type exists, it'll show that one instead.
    /// </summary>
    /// <typeparam name="T">The editor window type</typeparam>
    /// <returns>The window, or null</returns>
    public static T GetWindow<T>() where T : EditorWindow
    {
        foreach(var window in StapleEditor.instance.editorWindows)
        {
            if(window != null && window.GetType() == typeof(T))
            {
                return (T)window;
            }
        }

        try
        {
            var result = (T)Activator.CreateInstance(typeof(T));

            if (result == null)
            {
                return null;
            }

            result.title = result.title ?? typeof(T).Name;

            StapleEditor.instance.editorWindows.Add(result);

            return result;
        }
        catch(Exception)
        {
            return null;
        }
    }
}
