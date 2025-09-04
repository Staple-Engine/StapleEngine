namespace Staple.Editor;

[CustomEditor(typeof(Camera))]
internal class CameraGizmoEditor : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
        if (component is not Camera camera)
        {
            return;
        }

        var corners = camera.Corners(transform);

        var color = new Color("#D3D3D366");

        Gizmo.Lines(corners, [0, 1, 1, 3, 3, 2, 2, 0, 4, 5, 5, 7, 7, 6, 6, 4, 0, 4, 1, 5, 2, 6, 3, 7], color);
    }
}
