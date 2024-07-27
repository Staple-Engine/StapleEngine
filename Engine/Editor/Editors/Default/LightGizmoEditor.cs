namespace Staple.Editor;

[CustomEditor(typeof(Light))]
internal class LightGizmoEditor : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
        base.OnGizmo(entity, transform, component);

        Gizmo.Line(transform.Position, transform.Position + transform.Forward, new Color(0, 1, 0, 0.25f));
    }
}
