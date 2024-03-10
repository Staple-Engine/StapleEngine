using Staple.Internal;
using System.Numerics;

namespace Staple;

public partial struct Entity
{
    /// <summary>
    /// Instantiates a new copy of an existing entity
    /// </summary>
    /// <param name="source">The entity to instantiate</param>
    /// <param name="parent">The parent transform, if any</param>
    /// <param name="keepWorldPosition">Whether to keep the world position</param>
    /// <returns>The new entity</returns>
    public static Entity Instantiate(Entity source, Transform parent, bool keepWorldPosition = true)
    {
        if (source.IsValid == false ||
            source.TryGetComponent<Transform>(out var sourceTransform) == false)
        {
            return default;
        }

        var newEntity = Create($"{source.Name} (Clone)", typeof(Transform));

        var transform = newEntity.GetComponent<Transform>();

        transform.SetParent(parent);

        if (keepWorldPosition)
        {
            transform.Position = sourceTransform.Position;
            transform.Rotation = sourceTransform.Rotation;
            transform.Scale = sourceTransform.Scale;
        }

        SceneSerialization.InstantiateEntityComponents(source, newEntity);

        void Recursive(Transform sourceTransform, Transform targetTransform)
        {
            foreach(var child in sourceTransform)
            {
                var childEntity = Instantiate(child.entity, targetTransform);

                if(childEntity.IsValid == false ||
                    childEntity.TryGetComponent<Transform>(out var childTransform))
                {
                    continue;
                }

                Recursive(child, childTransform);
            }
        }

        Recursive(sourceTransform, transform);

        return newEntity;
    }

    /// <summary>
    /// Instantiates a new copy of an existing entity
    /// </summary>
    /// <param name="source">The entity to instantiate</param>
    /// <param name="position">The entity's position</param>
    /// <param name="rotation">The entity's rotation</param>
    /// <param name="parent">The parent transform, if any</param>
    /// <returns>The new entity</returns>
    public static Entity Instantiate(Entity source, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (source.IsValid == false ||
            source.TryGetComponent<Transform>(out var sourceTransform) == false)
        {
            return default;
        }

        var newEntity = Create($"{source.Name} (Clone)", typeof(Transform));

        var transform = newEntity.GetComponent<Transform>();

        if (parent != null)
        {
            transform.SetParent(parent);
        }

        transform.Position = position;
        transform.Rotation = rotation;

        SceneSerialization.InstantiateEntityComponents(source, newEntity);

        void Recursive(Transform sourceTransform, Transform targetTransform)
        {
            foreach (var child in sourceTransform)
            {
                var childEntity = Instantiate(child.entity, targetTransform);

                if (childEntity.IsValid == false ||
                    childEntity.TryGetComponent<Transform>(out var childTransform))
                {
                    continue;
                }

                Recursive(child, childTransform);
            }
        }

        Recursive(sourceTransform, transform);

        return newEntity;
    }
    /// <summary>
    /// Instantiates a new copy of an existing entity
    /// </summary>
    /// <param name="prefab">The prefab to instantiate</param>
    /// <param name="parent">The parent transform, if any</param>
    /// <returns>The new entity</returns>
    public static Entity Instantiate(Prefab prefab, Transform parent)
    {
        if (prefab?.data == null)
        {
            return default;
        }

        return SceneSerialization.InstantiatePrefab(parent?.entity ?? default, prefab.data);
    }

    /// <summary>
    /// Instantiates a new copy of an existing entity
    /// </summary>
    /// <param name="prefab">The entity to instantiate</param>
    /// <param name="position">The entity's position</param>
    /// <param name="rotation">The entity's rotation</param>
    /// <param name="parent">The parent transform, if any</param>
    /// <returns>The new entity</returns>
    public static Entity Instantiate(Prefab prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab?.data == null)
        {
            return default;
        }

        var newEntity = Instantiate(prefab, parent);

        var transform = newEntity.GetComponent<Transform>();

        if(transform != null)
        {
            transform.Position = position;
            transform.Rotation = rotation;
        }

        return newEntity;
    }
}
