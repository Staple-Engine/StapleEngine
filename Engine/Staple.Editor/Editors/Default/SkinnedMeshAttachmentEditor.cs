using System;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshAttachment))]
internal class SkinnedMeshAttachmentEditor : Editor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if (target is not SkinnedMeshAttachment attachment)
        {
            return false;
        }

        if(name == nameof(attachment.boneName))
        {
            if(attachment.mesh == null ||
                attachment.mesh.meshAsset == null)
            {
                return true;
            }

            var boneNames = attachment.mesh.meshAsset.nodes.Select(x => x.name).ToArray();

            var index = Array.IndexOf(boneNames, attachment.boneName);

            var newIndex = EditorGUI.Dropdown("Bone:", "SkinnedMeshAttachmentEditor.boneName", boneNames, index);

            if(newIndex != index && newIndex >= 0)
            {
                attachment.boneName = boneNames[newIndex];
            }

            return true;
        }

        return false;
    }
}
