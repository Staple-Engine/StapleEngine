using Staple.Internal;
using System.Numerics;

namespace Staple;

/// <summary>
/// UI element that renders text. This element has an intrinsic content size you can use.
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

    public UIText()
    {
        adjustToIntrinsicSize = true;
    }

    public override Vector2Int IntrinsicSize()
    {
        UpdateFontSize();

        var rect = TextRenderer.instance.MeasureTextSimple(text, Parameters(Vector2Int.Zero));

        return new Vector2Int(rect.left + rect.Width, rect.top + rect.Height);
    }

    private TextParameters Parameters(Vector2Int position)
    {
        return new TextParameters()
            .Position(new Vector2(position.X, position.Y))
            .Font(font)
            .FontSize(currentFontSize)
            .TextColor(textColor)
            .SecondaryTextColor(secondaryTextColor)
            .BorderSize(borderSize)
            .BorderColor(borderColor);
    }

    private void UpdateFontSize()
    {
        var parameters = Parameters(Vector2Int.Zero)
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
        UpdateFontSize();

        var parameters = Parameters(position);

        material ??= ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");

        TextRenderer.instance.DrawText(text, Matrix4x4.Identity, parameters, material, 1, viewID);
    }
}
