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

    private readonly Dictionary<ushort, List<TextInfo>> texts = [];

    public bool NeedsUpdate { get; set; }

    public bool UsesOwnRenderProcess => false;

    public void Startup()
    {
    }

    public void Shutdown()
    {
        material?.Destroy();
    }

    public void ClearRenderData(ushort viewID)
    {
        texts.Remove(viewID);
    }

    public Type RelatedComponent() => typeof(Text);

    public void Prepare()
    {
        texts.Clear();

        if(material == null)
        {
            var resource = SpriteRenderSystem.DefaultMaterial.Value;

            if(resource != null)
            {
                material = new Material(resource);
            }
        }
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(texts.TryGetValue(viewID, out var container) == false)
        {
            container = [];

            texts.Add(viewID, container);
        }
        else
        {
            container.Clear();
        }

        foreach (var (_, transform, relatedComponent) in entities)
        {
            if (relatedComponent is not Text text)
            {
                continue;
            }

            text.text ??= "";

            if (text.fontSize < 4)
            {
                text.fontSize = 4;
            }

            container.Add(new TextInfo()
            {
                text = text.text,
                fontSize = text.fontSize,
                transform = transform,
                fontAsset = text.font,
                scale = activeCamera.cameraType == CameraType.Orthographic ? 1 / (Screen.Height / (float)(activeCamera.orthographicSize * 2)) : 1,
            });
        }
    }

    public void Submit(ushort viewID)
    {
        if (material == null)
        {
            return;
        }

        if (texts.TryGetValue(viewID, out var container) == false)
        {
            return;
        }

        foreach (var text in container)
        {
            TextRenderer.instance.DrawText(text.text, text.transform.Matrix, new TextParameters().Font(text.fontAsset).FontSize(text.fontSize),
                material, text.scale, false, viewID);
        }
    }
}
