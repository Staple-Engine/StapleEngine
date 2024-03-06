namespace Staple.Editor;

/// <summary>
/// Allows creating specific entity templates
/// </summary>
public interface IEntityTemplate
{
    /// <summary>
    /// The template name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Creates the entity
    /// </summary>
    /// <returns>The entity</returns>
    Entity Create();
}
