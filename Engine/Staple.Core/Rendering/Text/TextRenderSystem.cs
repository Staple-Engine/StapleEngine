using System;
using System.Collections.Generic;

namespace Staple.Internal;

public class TextRenderSystem : IRenderSystem
{
    private struct TextInfo
    {
        public string text;

        public int fontSize;

        public Transform transform;

        public FontAsset fontAsset;

        public float scale;
    }

    private Material material;

    private readonly List<TextInfo> texts = [];

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(Text);

    public void Startup()
    {
    }

    public void Shutdown()
    {
        material?.Destroy();
    }

    public void Prepare()
    {
        texts.Clear();

        if(material == null)
        {
            var resource = SpriteUtils.DefaultMaterial.Value;

            if(resource != null)
            {
                material = new Material(resource);
            }
        }
    }

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        texts.Clear();

        foreach (var entry in renderQueue)
        {
            var text = (Text)entry.component;

            text.text ??= "";

            if (text.fontSize < 4)
            {
                text.fontSize = 4;
            }

            texts.Add(new TextInfo()
            {
                text = text.text,
                fontSize = text.fontSize,
                transform = entry.transform,
                fontAsset = text.font,
                scale = activeCamera.cameraType == CameraType.Orthographic ? 1 / (Screen.Height / (float)(activeCamera.orthographicSize * 2)) : 1,
            });
        }
    }

    public void Submit()
    {
        if (material == null)
        {
            return;
        }

        foreach (var text in texts)
        {
            TextRenderer.instance.DrawText(text.text, text.transform.Matrix, new TextParameters().Font(text.fontAsset).FontSize(text.fontSize),
                material, text.scale, false);
        }
    }
}
