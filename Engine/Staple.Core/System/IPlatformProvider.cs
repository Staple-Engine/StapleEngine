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
}
