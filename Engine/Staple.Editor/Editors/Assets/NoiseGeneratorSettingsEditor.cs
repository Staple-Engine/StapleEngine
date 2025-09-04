using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(NoiseGeneratorSettings))]
public class NoiseGeneratorSettingsEditor : StapleAssetEditor
{
    private const int TextureSize = 512;

    private Texture previewTexture;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(target is not NoiseGeneratorSettings settings)
        {
            return;
        }

        if(EditorGUI.Changed || previewTexture == null)
        {
            previewTexture?.Destroy();

            var generator = settings.MakeGenerator();

            var pixels = new Color[TextureSize * TextureSize];

            for(int y = -TextureSize / 2, yIndex = 0; y < TextureSize / 2; y++, yIndex += TextureSize)
            {
                for(var x = -TextureSize / 2; x < TextureSize / 2; x++)
                {
                    pixels[x + TextureSize / 2 + yIndex] = Color.White * (generator.GetNoise(x, y) * 0.5f + 0.5f);
                }
            }

            var buffer = new byte[pixels.Length * 4];

            for(var i = 0; i < pixels.Length; i++)
            {
                var c = (Color32)pixels[i];

                buffer[i * 4] = c.r;
                buffer[i * 4 + 1] = c.g;
                buffer[i * 4 + 2] = c.b;
                buffer[i * 4 + 3] = c.a;
            }

            previewTexture = Texture.CreatePixels(null, buffer, TextureSize, TextureSize, new(), TextureFormat.RGBA8);
        }

        if(previewTexture != null)
        {
            var width = EditorGUI.RemainingHorizontalSpace();

            EditorGUI.Texture(previewTexture, new Vector2(width, width));
        }
    }
}