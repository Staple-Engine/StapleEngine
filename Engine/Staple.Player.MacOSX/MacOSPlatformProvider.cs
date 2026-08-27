using Staple.Internal;
using System;
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
}
