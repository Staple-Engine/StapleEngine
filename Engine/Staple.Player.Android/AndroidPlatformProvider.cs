using Android.Content.Res;
using Android.OS;
using Staple.Internal;
using System.IO;

namespace Staple.Player.Android;

internal class AndroidPlatformProvider : IPlatformProvider
{
    internal AssetManager assetManager;

    public static readonly AndroidPlatformProvider Instance = new();

    public string StorageBasePath => Environment.ExternalStorageDirectory.AbsolutePath;

    public IRenderWindow CreateWindow() => AndroidRenderWindow.Instance;

    public Stream OpenFile(string path)
    {
        try
        {
            var s = assetManager.OpenFd(path);

            return s.CreateInputStream();
        }
        catch (System.Exception)
        {
            var s = assetManager.Open(path);

            var stream = new MemoryStream();

            s.CopyTo(stream);

            stream.Position = 0;

            return stream;
        }
    }
}
