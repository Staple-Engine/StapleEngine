using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Player.MacOS;

internal class MacOSPlatformProvider : IPlatformProvider
{
    public string StorageBasePath
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }
    }

    public IRenderWindow CreateWindow() => new SDL3RenderWindow();

    public void ConsoleLog(object message) => Console.WriteLine($"{message}");

    public Stream OpenFile(string path) => File.OpenRead(path);

    public void ShowMessageBox(MessageBoxType type, string title, string message, string okTitle, string cancelTitle, Action onOK,
        Action onCancel) => SDL3PlatformUtils.ShowMessageBox(type, title, message, okTitle, cancelTitle, onOK, onCancel);

    public void ShowOpenFileDialog(string title, string okTitle, string cancelTitle, string startPath, Dictionary<string, string> filters,
        bool allowMultiple, Action<Span<string>> success, Action failure)
    {
        SDL3PlatformUtils.ShowOpenFileDialog(title, okTitle, cancelTitle, startPath, filters, allowMultiple, success, failure);
    }

    public void ShowSaveFileDialog(string title, string okTitle, string cancelTitle, string startPath, Dictionary<string, string> filters,
        Action<Span<string>> success, Action failure)
    {
        SDL3PlatformUtils.ShowSaveFileDialog(title, okTitle, cancelTitle, startPath, filters, success, failure);
    }

    public void ShowOpenFolderDialog(string title, string okTitle, string cancelTitle, string startPath, bool allowMultiple,
        Action<Span<string>> success, Action failure)
    {
        SDL3PlatformUtils.ShowOpenFolderDialog(title, okTitle, cancelTitle, startPath, allowMultiple, success, failure);
    }
}
