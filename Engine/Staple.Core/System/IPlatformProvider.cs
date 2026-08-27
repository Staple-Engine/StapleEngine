using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Internal;

/// <summary>
/// Provider for platform-specific functionality
/// </summary>
internal interface IPlatformProvider
{
    /// <summary>
    /// The path where our storage's starting path is located
    /// </summary>
    string StorageBasePath { get; } 

    /// <summary>
    /// Creates a <see cref="IRenderWindow">
    /// </summary>
    /// <returns>The render window instance</returns>
    IRenderWindow CreateWindow();

    /// <summary>
    /// Attempts to open a file for reading
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>A stream, or null</returns>
    Stream OpenFile(string path);

    /// <summary>
    /// Writes a message to the console
    /// </summary>
    /// <param name="message">The message to write</param>
    void ConsoleLog(object message);

    /// <summary>
    /// Shows a message box
    /// </summary>
    /// <param name="type">The type of message box</param>
    /// <param name="title">The title of the message box</param>
    /// <param name="message">The message of the message box</param>
    /// <param name="okTitle">The title of the OK button</param>
    /// <param name="cancelTitle">The title of the cancel button (optional)</param>
    /// <param name="onOK">A callback when clicking OK (optional)</param>
    /// <param name="onCancel">A callback when clicking cancel (optional)</param>
    void ShowMessageBox(MessageBoxType type, string title, string message, string okTitle, string cancelTitle = null,
        Action onOK = null, Action onCancel = null);

    /// <summary>
    /// Shows a dialog for opening a file
    /// </summary>
    /// <param name="title">The title of the dialog. May be null.</param>
    /// <param name="okTitle">The title of the OK button. May be null.</param>
    /// <param name="cancelTitle">The title of the cancel button. May be null.</param>
    /// <param name="startPath">The path to start browsing from. May be null.</param>
    /// <param name="filters">
    /// A list of filters (name -> extension).
    /// The extension should not contain an asterisk or a dot at the start, unless you want to allow all files, in which case the entire extension should be "*".
    /// </param>
    /// <param name="allowMultiple">Whether multiple files might be selected</param>
    /// <param name="success">Called when we successfully choose a file</param>
    /// <param name="failure">Called when no file was selected</param>
    /// <remarks>
    /// This does not block the main thread, so consider it async.
    /// The callbacks will be called in the main thread.
    /// </remarks>
    void ShowOpenFileDialog(string title, string okTitle, string cancelTitle, string startPath, Dictionary<string, string> filters,
        bool allowMultiple, Action<Span<string>> success, Action failure);

    /// <summary>
    /// Shows a dialog for saving a file
    /// </summary>
    /// <param name="title">The title of the dialog. May be null.</param>
    /// <param name="okTitle">The title of the OK button. May be null.</param>
    /// <param name="cancelTitle">The title of the cancel button. May be null.</param>
    /// <param name="startPath">The path to start browsing from. May be null.</param>
    /// <param name="filters">
    /// A list of filters (name -> extension).
    /// The extension should not contain an asterisk or a dot at the start, unless you want to allow all files, in which case the entire extension should be "*".
    /// </param>
    /// <param name="success">Called when we successfully choose a file</param>
    /// <param name="failure">Called when no file was selected</param>
    /// <remarks>
    /// This does not block the main thread, so consider it async.
    /// The callbacks will be called in the main thread.
    /// </remarks>
    void ShowSaveFileDialog(string title, string okTitle, string cancelTitle, string startPath, Dictionary<string, string> filters,
        Action<Span<string>> success, Action failure);

    /// <summary>
    /// Shows a dialog for opening a folder
    /// </summary>
    /// <param name="title">The title of the dialog. May be null.</param>
    /// <param name="okTitle">The title of the OK button. May be null.</param>
    /// <param name="cancelTitle">The title of the cancel button. May be null.</param>
    /// <param name="startPath">The path to start browsing from. May be null.</param>
    /// <param name="allowMultiple">Whether multiple folders might be selected</param>
    /// <param name="success">Called when we successfully choose a folder</param>
    /// <param name="failure">Called when no folder was selected</param>
    /// <remarks>
    /// This does not block the main thread, so consider it async.
    /// The callbacks will be called in the main thread.
    /// </remarks>
    void ShowOpenFolderDialog(string title, string okTitle, string cancelTitle, string startPath, bool allowMultiple,
        Action<Span<string>> success, Action failure);
}
