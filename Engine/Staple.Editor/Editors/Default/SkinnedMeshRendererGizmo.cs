using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshRenderer))]
internal class SkinnedMeshRendererGizmo : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
        if(component is not SkinnedMeshRenderer renderer)
        {
            return;
        }

        Gizmo.WireframeBox(renderer.bounds.center, Quaternion.Identity, renderer.bounds.size, Color.Green);
    }
}
