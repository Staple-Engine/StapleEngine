using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.UI;

/// <summary>
/// UI component that renders text. This component has an intrinsic content size.
/// </summary>
public class UIText : UIElement
{
    /// <summary>
    /// The text to display
    /// </summary>
    [Multiline]
    public string text = "New Text";

    /// <summary>
    /// The font size for the text
    /// </summary>
    public int fontSize = 14;

    /// <summary>
    /// Whether to autosize the text
    /// </summary>
    public bool autoSizeText = false;

    /// <summary>
    /// Minimum font size when autosizing
    /// </summary>
    public int minFontSize = 14;
    
    /// <summary>
    /// The maximum font size when autosizing
    /// </summary>
    public int maxFontSize = 30;

    /// <summary>
    /// The text color
    /// </summary>
    public Color textColor = Color.White;

    /// <summary>
    /// The secondary text color. Can be used to do a gradient.
    /// </summary>
    public Color secondaryTextColor = Color.White;

    /// <summary>
    /// The size in pixels of the font border/outline. Set to 0 to use no border.
    /// </summary>
    [Min(0)]
    public int borderSize = 0;

    /// <summary>
    /// The border color
    /// </summary>
    public Color borderColor = Color.Clear;

    /// <summary>
    /// The font to use
    /// </summary>
    public FontAsset font;

    /// <summary>
    /// The material to use
    /// </summary>
    public Material material;

    private int currentFontSize = 0;

    private string previousText;
    private int previousFontSize;
    private bool previousAutoSizeText;
    private int previousMinFontSize;
    private int previousMaxFontSize;
    private Color previousTextColor;
    private Color previousSecondaryTextColor;
    private int previousBorderSize;
    private Color previousBorderColor;
    private VertexBuffer vertexBuffer;
    private IndexBuffer indexBuffer;
    private int vertexCount;
    private int indexCount;

    private bool IsDirty
    {
        get
        {
            return previousText != text ||
                previousFontSize != fontSize ||
                previousAutoSizeText != autoSizeText ||
                previousMinFontSize != minFontSize ||
                previousMaxFontSize != maxFontSize ||
                previousTextColor != textColor ||
                previousSecondaryTextColor != secondaryTextColor ||
                previousBorderSize != borderSize ||
                previousBorderColor != borderColor;
        }

        set
        {
            previousText = text;
            previousFontSize = fontSize;
            previousAutoSizeText = autoSizeText;
            previousMinFontSize = minFontSize;
            previousMaxFontSize = maxFontSize;
            previousTextColor = textColor;
            previousSecondaryTextColor = secondaryTextColor;
            previousBorderSize = borderSize;
            previousBorderColor = borderColor;
        }
    }

    public UIText()
    {
        adjustToIntrinsicSize = true;
    }

    public override Vector2Int IntrinsicSize()
    {
        UpdateFontSize();

        var rect = TextRenderer.instance.MeasureTextSimple(text, Parameters());

        return new Vector2Int(rect.left + rect.Width, rect.top + rect.Height);
    }

    private TextParameters Parameters()
    {
        return new TextParameters()
            .Font(font)
            .FontSize(currentFontSize)
            .TextColor(textColor)
            .SecondaryTextColor(secondaryTextColor)
            .BorderSize(borderSize)
            .BorderColor(borderColor)
            .Position(new Vector2(0, currentFontSize));
    }

    private void UpdateFontSize()
    {
        var parameters = Parameters()
            .FontSize(fontSize);

        if(autoSizeText)
        {
            TextRenderer.instance.FitTextAroundLength(text, parameters, size.X, out var estimatedFontSize);

            if (estimatedFontSize < minFontSize)
            {
                estimatedFontSize = minFontSize;
            }

            if (estimatedFontSize > maxFontSize)
            {
                estimatedFontSize = maxFontSize;
            }

            if (estimatedFontSize != currentFontSize)
            {
                currentFontSize = estimatedFontSize;
            }
        }
        else
        {
            currentFontSize = fontSize;
        }
    }

    public override void Render(Vector2Int position, ushort viewID)
    {
        material ??= ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");

        UpdateFontSize();

        var parameters = Parameters();

        if (IsDirty ||
            (vertexBuffer?.Disposed ?? true) ||
            (indexBuffer?.Disposed ?? true))
        {
            IsDirty = false;

            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();

            if (TextRenderer.instance.MakeTextGeometry(text, parameters, 1, true, out var vertices, out var indices))
            {
                vertexBuffer = VertexBuffer.Create(vertices.AsSpan(), TextRenderer.VertexLayout.Value);

                indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None);

                if(vertexBuffer == null || indexBuffer == null)
                {
                    vertexBuffer?.Destroy();
                    indexBuffer?.Destroy();

                    vertexBuffer = null;
                    indexBuffer = null;
                    vertexCount = 0;
                    indexCount = 0;

                    return;
                }

                vertexCount = vertices.Length;
                indexCount = indices.Length;
            }
        }

        if(vertexBuffer != null &&
            indexBuffer != null &&
            vertexBuffer.Disposed == false &&
            indexBuffer.Disposed == false &&
            material != null)
        {
            material.MainTexture = TextRenderer.instance.FontTexture(parameters);

            Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertexCount, 0, indexCount, material,
                Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MeshLighting.Unlit,
                viewID);
        }
    }
}
