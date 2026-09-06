using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(MeshRenderer))]
internal class MeshRendererGizmo : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, Component component)
    {
        if (component is not MeshRenderer renderer)
        {
            return;
        }

        Gizmo.WireframeBox(renderer.bounds.center, Quaternion.Identity, renderer.bounds.size, Color.Green);
    }
}
