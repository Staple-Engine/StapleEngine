using System.IO;

namespace Staple.Internal;

internal interface IPlatformProvider
{
    string StorageBasePath { get; } 

    IRenderWindow CreateWindow();

    Stream OpenFile(string path);
}
