using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshAnimator))]
internal class SkinnedMeshAnimatorEditor : Editor
{
    public override bool RenderField(FieldInfo field)
    {
        if(target is not SkinnedMeshAnimator renderer)
        {
            return base.RenderField(field);
        }

        if(field.Name == nameof(SkinnedMeshAnimator.animation))
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

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(target is not SkinnedMeshAnimator animator)
        {
            return;
        }

        animator.playInEditMode = EditorGUI.Toggle("Play on edit mode", animator.playInEditMode);
    }
}
