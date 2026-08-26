using System;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Tracks the version of <see cref="IComponentVersion"/> components
/// </summary>
/// <typeparam name="T">The component</typeparam>
public class ComponentVersionTracker<T> where T: IComponent, IComponentVersion
{
    private readonly ExpandableContainer<ulong> versions = new();
    private readonly ExpandableContainer<int> generations = new();

    /// <summary>
    /// Checks whether we should update a component based on its version changing
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="component">The component</param>
    /// <returns>Whether the version changed</returns>
    /// <exception cref="ArgumentNullException">The component is null</exception>
    /// <remarks>This method should be called only when you're completely sure the <paramref name="entity"/> is valid</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldUpdateComponent(Entity entity, in T component) =>
        ShouldUpdateComponent(entity.Identifier.ID - 1, entity.Identifier.generation, in component);

    /// <summary>
    /// Checks whether we should update a component based on its version changing
    /// </summary>
    /// <param name="index">The entity index</param>
    /// <param name="generation">The generation of the entity</param>
    /// <param name="component">The component</param>
    /// <returns>Whether the version changed</returns>
    /// <exception cref="ArgumentNullException">The component is null</exception>
    /// <remarks>This method should be called only when you're completely sure the <paramref name="index"/> is valid</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldUpdateComponent(int index, int generation, in T component)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component), "Component can't be null");
        }

        var componentVersion = component.Version;

        if (index >= versions.Length)
        {
            versions.Resize(index + 1, true);
            generations.Resize(index + 1, true);
        }

        ref var version = ref versions.Contents[index];
        ref var entityGeneration = ref generations.Contents[index];

        if (version != componentVersion)
        {
            version = componentVersion;

            return true;
        }

        if(entityGeneration != generation)
        {
            entityGeneration = generation;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears the data stored in this tracker
    /// </summary>
    public void Clear()
    {
        versions.ClearValues();
    }
}
