using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshRenderer))]
internal class SkinnedMeshRendererEditor : Editor
{
    public override bool RenderField(FieldInfo field)
    {
        if(target is not SkinnedMeshRenderer renderer)
        {
            return base.RenderField(field);
        }

        if(field.Name == nameof(SkinnedMeshRenderer.animation))
        {
            if(renderer.mesh?.meshAsset != null)
            {
                var animations = renderer.mesh.meshAsset.animations;

                var animationNames = animations.Select(x => x.Key).ToList();

                var current = animationNames.IndexOf(renderer.animation);

                if (current < 0)
                {
                    current = 0;
                }

                current = EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), animationNames.ToArray(), current);

                if(current < 0)
                {
                    current = 0;
                }

                if(current >= 0 && current < animationNames.Count)
                {
                    field.SetValue(target, animationNames[current]);
                }
            }

            return true;
        }

        return base.RenderField(field);
    }
}