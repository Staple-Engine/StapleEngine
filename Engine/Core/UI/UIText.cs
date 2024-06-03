using Staple.Internal;
using System.Numerics;

namespace Staple;

public class UIText : IUIElement
{
    [Multiline]
    public string text = "New Text";
    public int fontSize = 14;
    public Color textColor = Color.White;
    public Color secondaryTextColor = Color.White;

    [Min(0)]
    public int borderSize = 0;

    public Color borderColor = Color.Clear;

    public FontAsset font;
    public Material material;

    public void Render(Vector2Int position, ushort viewID)
    {
        var parameters = new TextParameters()
            .Position(new Vector2(position.X, position.Y))
            .Font(font)
            .FontSize(fontSize)
            .TextColor(textColor)
            .SecondaryTextColor(secondaryTextColor)
            .BorderSize(borderSize)
            .BorderColor(borderColor);

        material ??= ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");

        TextRenderer.instance.DrawText(text, Matrix4x4.Identity, parameters, material, 1, viewID);
    }
}
