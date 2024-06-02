using Staple.Internal;
using System.Numerics;

namespace Staple;

public class UIText : IUIElement
{
    public string text = "New Text";
    public int fontSize = 14;
    public FontAsset font;
    public Material material;

    public void Render(Vector2Int position, ushort viewID)
    {
        var parameters = new TextParameters()
            .Position(new Vector2(position.X + (font?.FontSize ?? 0), position.Y - (font?.FontSize ?? 0)))
            .Font(font)
            .FontSize(fontSize);

        material ??= ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");

        TextRenderer.instance.DrawText(text, Matrix4x4.Identity, parameters, material, 1, viewID);
    }
}
