using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(Light))]
internal class LightGizmoEditor : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
        var start = transform.Position;
        var end = transform.Position + transform.Forward;

        var t = Matrix4x4.TRS(default, Vector3.One, transform.Rotation);

        for (var i = 0.0f; i < 360; i += 45)
        {
            var offset = Vector3.Transform(new Vector3(Math.Cos(Math.Deg2Rad * i) * 0.25f, Math.Sin(Math.Deg2Rad * i) * 0.25f, 0), t);

            Gizmo.Line(start + offset, end + offset, new Color(0, 1, 0, 0.25f));
        }
    }
}
