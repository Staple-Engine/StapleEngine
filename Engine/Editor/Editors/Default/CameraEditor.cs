using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(Camera))]
    internal class CameraEditor : Editor
    {
        public override bool RenderField(FieldInfo field)
        {
            var camera = target as Camera;

            if (field.Name == nameof(Camera.orthographicSize))
            {
                if(camera.cameraType == CameraType.Orthographic)
                {
                    camera.orthographicSize = EditorGUI.FloatField(field.Name.ExpandCamelCaseName(), camera.orthographicSize);

                    if(camera.orthographicSize < 1)
                    {
                        camera.orthographicSize = 1;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
