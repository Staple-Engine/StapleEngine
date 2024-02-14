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

        public Vector3 position;

        public ushort viewID;

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
            var resource = ResourceManager.instance.LoadMaterial("Materials/Sprite.mat");

            if(resource != null)
            {
                material = new Material(resource);
            }
        }
    }

    public void RenderText(string text, TextParameters parameters, Material material, float scale, ushort viewID)
    {
        textRenderer.DrawText(text, parameters, material, scale, viewID);
    }

    public void Preprocess(World world, Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(World world, Entity entity, Transform transform, IComponent relatedComponent,
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
            position = transform.Position,
            viewID = viewId,
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
            RenderText(text.text, new TextParameters().FontSize(text.fontSize).Position(new Vector2(text.position.X, text.position.Y)), material,
                text.scale, text.viewID);
        }
    }

    public void Destroy()
    {
        material?.Destroy();
    }
}