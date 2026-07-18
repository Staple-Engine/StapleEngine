namespace Staple.Internal;

internal class TextureResource(ITextureCreateMethod createMethod)
{
    internal TextureMetadata metadata;
    internal bool renderTarget = false;
    internal ITexture impl;

    internal RawTextureData readbackData;

    internal ITextureCreateMethod createMethod = createMethod;

    public GuidHasher Guid = new();

    public Sprite[] Sprites { get; internal set; } = [];

    public bool CreateMethodDisposed => createMethod == null;

    internal bool Create()
    {
        var ok = false;

        if (renderTarget || (createMethod != null && createMethod.Create(this)))
        {
            ok = true;
        }

        createMethod = null;

        if (!ok)
        {
            return false;
        }

        if (ok && (metadata?.sprites?.Count ?? 0) > 0)
        {
            var sprites = Sprites;

            if ((Sprites?.Length ?? 0) != metadata.sprites.Count)
            {
                sprites = new Sprite[metadata.sprites.Count];
            }

            for (var i = 0; i < metadata.sprites.Count; i++)
            {
                sprites[i] ??= new();

                sprites[i].spriteIndex = i;
            }

            Sprites = sprites;
        }

        return ok;
    }
}
