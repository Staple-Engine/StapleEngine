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
}
