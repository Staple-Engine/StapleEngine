using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// Flags for whether to hide an entity
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EntityHierarchyVisibility>))]
public enum EntityHierarchyVisibility
{
    /// <summary>
    /// not hidden
    /// </summary>
    None,
    /// <summary>
    /// Hide this entity from the inspector (but save it)
    /// </summary>
    Hide,
    /// <summary>
    /// Hide this entity from the inspector, and don't save it
    /// </summary>
    HideAndDontSave,
}
