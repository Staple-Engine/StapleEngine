using System;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

public class GizmoEditor
{
    private static Type[] editorTypes;

    public virtual void OnGizmo(Entity entity, Transform transform, IComponent component)
    {
    }

    internal static void UpdateEditorTypes()
    {
        editorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsSubclassOf(typeof(GizmoEditor)))
                .Distinct()
                .ToArray();
    }

    public static GizmoEditor CreateGizmoEditor(IComponent component)
    {
        Type editorType = null;

        var targetType = component.GetType();

        foreach (var type in editorTypes)
        {
            var attribute = type.GetCustomAttribute<CustomEditorAttribute>();

            if (attribute == null || type.IsSubclassOf(typeof(GizmoEditor)) == false)
            {
                continue;
            }

            if (attribute.target == targetType)
            {
                editorType = type;

                break;
            }
        }

        if (editorType == null)
        {
            return null;
        }

        try
        {
            var instance = (GizmoEditor)Activator.CreateInstance(editorType);

            return instance;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
