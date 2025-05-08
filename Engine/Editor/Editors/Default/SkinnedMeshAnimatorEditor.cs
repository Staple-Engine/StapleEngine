using System;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshAnimator))]
internal class SkinnedMeshAnimatorEditor : Editor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if (target is not SkinnedMeshAnimator renderer)
        {
            return false;
        }

        if(name == nameof(SkinnedMeshAnimator.animation))
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

                current = EditorGUI.Dropdown(name.ExpandCamelCaseName(), "SkinnedMeshAnimatorNames", animationNames.ToArray(), current);

                if(current < 0)
                {
                    current = 0;
                }

                if(current >= 0 && current < animationNames.Count)
                {
                    setter(animationNames[current]);
                }
            }

            return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(target is not SkinnedMeshAnimator animator)
        {
            return;
        }

        animator.playInEditMode = EditorGUI.Toggle("Play on edit mode", "SkinnedMeshAnimatorPlay", animator.playInEditMode);

        if(animator.animation != null &&
            animator.mesh != null &&
            animator.mesh.meshAsset != null &&
            animator.mesh.meshAssetIndex >= 0 &&
            animator.mesh.meshAssetIndex < animator.mesh.meshAsset.meshes.Count &&
            animator.mesh.meshAsset.animations.TryGetValue(animator.animation, out var animation))
        {
            var newPlaytime = EditorGUI.FloatField("Play Time (seconds)", "SkinnedMeshAnimatorPlayTime", animator.playTime);

            string TimeString(float time)
            {
                var seconds = (int)time;

                var milliseconds = (int)((time - (int)time) * 10090);

                return $"{seconds}.{milliseconds:0000}";
            }

            EditorGUI.Label($"{TimeString(animator.playTime)} / {TimeString(animation.duration)}");

            if (animator.playTime != newPlaytime)
            {
                animator.playTime = newPlaytime;
            }
        }
    }
}
