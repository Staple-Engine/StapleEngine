using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

internal class TextRenderSystem : IRenderSystem
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

    private TextRenderer textRenderer;

    private Material material;

    private readonly List<TextInfo> texts = new();

    public Type RelatedComponent() => typeof(TextComponent);

    public void Prepare()
    {
        texts.Clear();

        if(textRenderer == null)
        {
            textRenderer = new();

            textRenderer.LoadDefaultFont();
        }

        if(material == null)
        {
            var resource = ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");

            if(resource != null)
            {
                material = new Material(resource);
            }
        }
    }

    public void RenderText(string text, Matrix4x4 transform, TextParameters parameters, Material material, float scale, ushort viewID)
    {
        textRenderer.DrawText(text, transform, parameters, material, scale, viewID);
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        if(relatedComponent is not TextComponent text)
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
            RenderText(text.text, text.transform, new TextParameters().Font(text.fontAsset?.font).FontSize(text.fontSize), material,
                text.scale, text.viewID);
        }
    }

    public void Destroy()
    {
        material?.Destroy();
    }
}