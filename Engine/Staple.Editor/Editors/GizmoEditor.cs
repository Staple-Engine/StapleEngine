using Staple.Internal;
using System;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

/// <summary>
/// Editor for gizmos
/// </summary>
public abstract class GizmoEditor
{
    private static Type[] editorTypes;

    /// <summary>
    /// Called when an entity is selected, for each component
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="transform">The entity transform</param>
    /// <param name="component">The component</param>
    public abstract void OnGizmo(Entity entity, Transform transform, IComponent component);

    internal static void UpdateEditorTypes()
    {
        editorTypes = Assembly.GetCallingAssembly().GetTypes()
                .Concat(Assembly.GetExecutingAssembly().GetTypes())
                .Concat(TypeCache.types.Select(x => x.Value))
                .Where(x => x.IsSubclassOf(typeof(GizmoEditor)))
                .Distinct()
                .ToArray();
    }

    /// <summary>
    /// Attempts to create a gizmo editor for a component
    /// </summary>
    /// <param name="component">The component</param>
    /// <returns>The editor, or null</returns>
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
