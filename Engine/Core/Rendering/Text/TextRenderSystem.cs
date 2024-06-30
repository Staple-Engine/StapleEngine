using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

public class TextRenderSystem : IRenderSystem
{
    private class TextInfo
    {
        public string text;

        public int fontSize;

        public Matrix4x4 transform;

        public ushort viewID;

        public FontAsset fontAsset;

        public float scale;
    }

    private Material material;

    private readonly List<TextInfo> texts = new();

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

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        if(relatedComponent is not Text text)
        {
            return;
        }

        text.text ??= "";
        
        if(text.fontSize < 4)
        {
            text.fontSize = 4;
        }

        texts.Add(new TextInfo()
        {
            text = text.text,
            fontSize = text.fontSize,
            transform = transform.Matrix,
            viewID = viewId,
            fontAsset = text.font,
            scale = activeCamera.cameraType == CameraType.Orthographic ? 1 / (Screen.Height / (float)(activeCamera.orthographicSize * 2)) : 1,
        });
    }

    public void Submit()
    {
        if(material == null)
        {
            return;
        }

        foreach(var text in texts)
        {
            TextRenderer.instance.DrawText(text.text, text.transform, new TextParameters().Font(text.fontAsset).FontSize(text.fontSize),
                material, text.scale, false, text.viewID);
        }
    }

    public void Destroy()
    {
        material?.Destroy();
    }
}