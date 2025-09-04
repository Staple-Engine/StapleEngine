namespace Staple.Editor;

[CustomEditor(typeof(CullingVolume))]
internal class CullingVolumeGizmoEditor : GizmoEditor
{
    public override void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
        if (component is not CullingVolume volume ||
            volume.type != CullingVolume.CullingType.Bounds)
        {
            return;
        }

        Gizmo.WireframeBox(transform.Position, transform.Rotation, volume.bounds * transform.Scale, new Color(0, 1, 1, 0.25f));
    }
}
