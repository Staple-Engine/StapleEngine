namespace Staple.Internal;

internal interface ITexture
{
    int Width { get; }

    int Height { get; }

    bool Disposed { get; }

    TextureFormat Format { get; }

    TextureType Type { get; }

    void Destroy();
}
