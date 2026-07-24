using System.Collections.Generic;

namespace Staple.Internal;

public class TextRenderSystem : RenderSystemBase
{
    private struct TextInfo
    {
        public string text;

        public int fontSize;

        public Transform transform;

        public FontAsset fontAsset;

        public Material material;

        public float scale;
    }

    private readonly List<TextInfo> texts = [];

    public TextRenderSystem() : base(false, typeof(Text), typeof(GenericRenderQueue<Text>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<Text>();

    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }

    public override void Prepare()
    {
        texts.Clear();
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        if(renderQueue is not GenericRenderQueue<Text> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var text = entry.component;

            if ((text.materials?.Count ?? 0) == 0)
            {
                continue;
            }

            text.text ??= "";

            if (text.fontSize < 4)
            {
                text.fontSize = 4;
            }

            IterateValidMaterials(text, renderIndex, (index) =>
            {
                texts.Add(new TextInfo()
                {
                    text = text.text,
                    fontSize = text.fontSize,
                    transform = entry.transform,
                    fontAsset = text.font,
                    scale = activeCamera.cameraType == CameraType.Orthographic ? 1 / (Screen.Height / (float)(activeCamera.orthographicSize * 2)) : 1,
                    material = text.materials[index],
                });
            });
        }
    }

    public override void Submit()
    {
        foreach (var text in texts)
        {
            TextRenderer.instance.DrawText(text.text, text.transform.Matrix, new TextParameters().Font(text.fontAsset).FontSize(text.fontSize),
                text.material, text.scale, false);
        }
    }
}
