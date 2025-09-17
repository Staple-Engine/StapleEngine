using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace Staple.Internal;

internal static class SceneSerialization
{
    /// <summary>
    /// Instantiates a scene object
    /// </summary>
    /// <param name="sceneObject">The scene object to instantiate</param>
    /// <param name="localID">The local ID of the entity</param>
    /// <param name="activate">Whether to activate the object and call lifecycle callbacks</param>
    /// <returns>The new entity, or Entity.Empty</returns>
    internal static Entity Instantiate(SceneObject sceneObject, out int localID, bool activate)
    {
        localID = sceneObject.ID;

        Scene.InstancingComponent = true;

        var entity = Entity.Create(sceneObject.name);

        if ((sceneObject.prefabGuid?.Length ?? 0) > 0)
        {
            entity.SetPrefab(sceneObject.prefabGuid, sceneObject.prefabLocalID);
        }

        entity.HierarchyVisibility = sceneObject.hierarchyVisibility;

        var transform = entity.AddComponent<Transform>();

        entity.Enabled = sceneObject.enabled;

        var layer = LayerMask.NameToLayer(sceneObject.layer);

        if (layer >= 0)
        {
            entity.Layer = (uint)layer;
        }

        var rotation = sceneObject.transform.rotation.ToVector3();

        transform.LocalPosition = sceneObject.transform.position.ToVector3();
        transform.LocalRotation = Math.FromEulerAngles(rotation);
        transform.LocalScale = sceneObject.transform.scale.ToVector3();

        foreach (var component in sceneObject.components)
        {
            var type = TypeCache.GetType(component.type);

            if (type == null)
            {
                Log.Error($"Failed to create component {component.type} for entity {sceneObject.name}");

                continue;
            }

            var container = new StapleSerializerContainer()
            {
                typeName = component.type,
            };

            if ((component.data?.Count ?? 0) > 0)
            {
                foreach (var pair in component.data)
                {
                    var field = type.GetField(pair.Key);

                    if (field is null)
                    {
                        continue;
                    }

                    container.fields.Add(pair.Key, new()
                    {
                        typeName = field.FieldType.ToString(),
                        value = pair.Value,
                    });
                }
            }
            else if (component.parameters != null)
            {
                foreach (var pair in component.parameters)
                {
                    var field = type.GetField(pair.Key);

                    if (field is null)
                    {
                        continue;
                    }

                    container.fields.Add(pair.Key, new()
                    {
                        typeName = field.FieldType.ToString(),
                        value = pair.Value,
                    });
                }
            }

            var componentInstance = entity.AddComponent(type);

            StapleSerializer.DeserializeContainer(container, StapleSerializationMode.Scene, componentInstance);

            if (componentInstance is null)
            {
                continue;
            }

            entity.SetComponent(componentInstance);
        }

        if (activate)
        {
            entity.IterateComponents((ref IComponent c) =>
            {
                World.Current?.EmitAddComponentEvent(entity, ref c);
            });
        }

        Scene.InstancingComponent = false;

        return entity;
    }

    /// <summary>
    /// Instantiates all components in an entity into another entity
    /// </summary>
    /// <param name="source">The source entity</param>
    /// <param name="target">The target entity</param>
    public static void InstantiateEntityComponents(Entity source, Entity target)
    {
        source.IterateComponents((ref IComponent component) =>
        {
            if(component is Transform)
            {
                return;
            }

            var container = StapleSerializer.SerializeContainer(component, StapleSerializationMode.Binary);

            var clone = (IComponent)StapleSerializer.DeserializeContainer(container, StapleSerializationMode.Binary);

            if(clone is null)
            {
                return;
            }

            target.SetComponent(clone);
        });
    }

    /// <summary>
    /// Serializes the components of an entity into a SceneObject
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The new scene object</returns>
    public static SceneObject SerializeEntityComponents(Entity entity, StapleSerializationMode mode)
    {
        var components = new List<SceneComponent>();

        entity.IterateComponents((ref IComponent component) =>
        {
            if (component == null || component.GetType() == typeof(Transform))
            {
                return;
            }

            var container = StapleSerializer.SerializeContainer(component, mode);

            if(container == null)
            {
                return;
            }

            var sceneComponent = new SceneComponent()
            {
                type = container.typeName,
            };

            switch(mode)
            {
                case StapleSerializationMode.Binary:

                    foreach (var pair in container.fields)
                    {
                        sceneComponent.parameters.Add(pair.Key, pair.Value.ToRawValue());
                    }

                    break;

                case StapleSerializationMode.Scene:

                    foreach (var pair in container.fields)
                    {
                        sceneComponent.data.Add(pair.Key, pair.Value.ToRawValue());
                    }

                    break;

                case StapleSerializationMode.Text:

                    foreach (var pair in container.fields)
                    {
                        sceneComponent.data.Add(pair.Key, pair.Value);
                    }

                    break;
            }

            components.Add(sceneComponent);
        });

        string entityLayer;

        var index = entity.Layer;

        if (index < LayerMask.AllLayers.Count)
        {
            entityLayer = LayerMask.AllLayers[(int)index];
        }
        else
        {
            entityLayer = LayerMask.AllLayers.FirstOrDefault();
        }

        return new SceneObject()
        {
            ID = entity.Identifier.ID,
            name = entity.Name,
            enabled = entity.Enabled,
            kind = SceneObjectKind.Entity,
            components = components,
            layer = entityLayer,
            hierarchyVisibility = entity.HierarchyVisibility,
        };
    }

    /// <summary>
    /// Serializes an entity into a SceneObject
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The entity, or null</returns>
    public static SceneObject SerializeEntity(Entity entity, StapleSerializationMode mode)
    {
        if(entity.IsValid == false || entity.HierarchyVisibility == EntityHierarchyVisibility.HideAndDontSave)
        {
            return null;
        }

        SceneObjectTransform transform = null;

        var entityTransform = entity.GetComponent<Transform>();

        var parent = entityTransform.Parent?.Entity ?? default;

        if (entityTransform != null)
        {
            transform = new()
            {
                position = new Vector3Holder(entityTransform.LocalPosition),
                rotation = new Vector3Holder(entityTransform.LocalRotation),
                scale = new Vector3Holder(entityTransform.LocalScale),
            };
        }

        var outEntity = SerializeEntityComponents(entity, mode);

        outEntity.parent = parent.IsValid ? parent.Identifier.ID : 0;
        outEntity.transform = transform;

        if(entity.TryGetPrefab(out var prefabGuid, out var localID))
        {
            outEntity.prefabGuid = prefabGuid;
            outEntity.prefabLocalID = localID;
        }

        return outEntity;
    }

    /// <summary>
    /// Serializes a scene into a SerializableScene
    /// </summary>
    /// <returns>The serialized scene</returns>
    public static SerializableScene Serialize(this Scene _)
    {
        var outValue = new SerializableScene();

        Scene.IterateEntities((Entity entity) =>
        {
            if(entity.HierarchyVisibility == EntityHierarchyVisibility.HideAndDontSave)
            {
                return;
            }

            var outEntity = SerializeEntity(entity, StapleSerializationMode.Scene);

            if(outEntity == null)
            {
                return;
            }

            outValue.objects.Add(outEntity);
        });

        return outValue;
    }

    /// <summary>
    /// Serializes an entity into a prefab
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <returns>The prefab data, or null</returns>
    public static SerializablePrefab SerializeIntoPrefab(Entity entity)
    {
        if(entity.IsValid == false ||
            entity.TryGetComponent<Transform>(out var entityTransform) == false)
        {
            return null;
        }

        var outValue = new SerializablePrefab
        {
            mainObject = SerializeEntity(entity, StapleSerializationMode.Scene)
        };

        outValue.mainObject.ID = 0;

        var localIDs = new Dictionary<int, int>();

        void GatherIDs(Transform transform)
        {
            if(transform.Entity.HierarchyVisibility == EntityHierarchyVisibility.HideAndDontSave)
            {
                return;
            }

            localIDs.Add(transform.Entity.Identifier.ID, localIDs.Count);

            foreach(var child in transform.Children)
            {
                GatherIDs(child);
            }
        }

        GatherIDs(entityTransform);

        void GatherSceneObjects(Transform transform, bool first)
        {
            if(transform.Entity.HierarchyVisibility == EntityHierarchyVisibility.HideAndDontSave)
            {
                return;
            }

            if(first == false)
            {
                var entityObject = SerializeEntity(transform.Entity, StapleSerializationMode.Scene);

                if(entityObject == null ||
                    localIDs.TryGetValue(transform.Entity.Identifier.ID, out var localID) == false ||
                    localIDs.TryGetValue(transform.Parent.Entity.Identifier.ID, out var localParent) == false)
                {
                    return;
                }

                entityObject.ID = localID;
                entityObject.parent = localParent;

                outValue.children.Add(entityObject);
            }

            foreach (var child in transform.Children)
            {
                GatherSceneObjects(child, false);
            }
        }

        GatherSceneObjects(entityTransform, true);

        return outValue;
    }

    /// <summary>
    /// Instantiates a prefab
    /// </summary>
    /// <param name="parent">The parent entity, if any</param>
    /// <param name="prefab">The prefab to instantiate</param>
    /// <returns>The new entity, or Invalid</returns>
    public static Entity InstantiatePrefab(Entity parent, SerializablePrefab prefab)
    {
        if(prefab == null ||
            prefab.mainObject == null ||
            prefab.children == null)
        {
            return default;
        }

        var parentTransform = parent.GetComponent<Transform>();

        var newEntity = Instantiate(prefab.mainObject, out _, true);

        newEntity.SetPrefab(prefab.guid, prefab.mainObject.ID);

        var transform = newEntity.GetComponent<Transform>();

        transform.SetParent(parentTransform);

        transform.LocalPosition = prefab.mainObject.transform.position.ToVector3();
        transform.LocalRotation = prefab.mainObject.transform.rotation.ToQuaternion();
        transform.LocalScale = prefab.mainObject.transform.scale.ToVector3();

        var localIDs = new Dictionary<int, int>();

        localIDs.AddOrSetKey(0, newEntity.Identifier.ID);

        var counter = 1;

        var localEntities = new Dictionary<int, Entity>();

        foreach(var sceneObject in prefab.children)
        {
            var childEntity = Instantiate(sceneObject, out _, true);

            if(childEntity.IsValid)
            {
                localEntities.Add(counter, childEntity);
                localIDs.Add(counter++, childEntity.Identifier.ID);

                if(localIDs.TryGetValue(sceneObject.parent, out var localParentID))
                {
                    var childTransform = childEntity.GetComponent<Transform>();
                    var targetTransform = Scene.FindEntity(localParentID).GetComponent<Transform>();

                    childEntity.SetPrefab(prefab.guid, sceneObject.ID);

                    if (targetTransform != null)
                    {
                        childTransform.SetParent(targetTransform);
                    }
                }
            }
        }

        void HandleReferences(Entity entity, SceneObject sceneObject)
        {
            foreach(var component in sceneObject.components)
            {
                var componentType = TypeCache.GetType(component.type);

                if(componentType == null || entity.TryGetComponent(componentType, out var componentInstance) == false)
                {
                    continue;
                }

                foreach(var parameter in component.parameters)
                {
                    try
                    {
                        var field = componentType.GetField(parameter.Key, BindingFlags.Public | BindingFlags.Instance);

                        if (field == null)
                        {
                            continue;
                        }

                        if(field.FieldType == typeof(Entity) && parameter.Value is int intValue)
                        {
                            Entity targetEntity = default;

                            if (localIDs.TryGetValue(intValue, out var localEntityID))
                            {
                                targetEntity = Scene.FindEntity(localEntityID);
                            }

                            if(targetEntity.IsValid)
                            {
                                field.SetValue(componentInstance, targetEntity);
                            }
                        }
                        else if((field.FieldType == typeof(IComponent) ||
                            field.FieldType.GetInterface(typeof(IComponent).FullName) != null) &&
                            parameter.Value is string stringValue)
                        {
                            var pieces = stringValue.Split(":");

                            if(pieces.Length == 2 &&
                                int.TryParse(pieces[0], out var entityID))
                            {
                                var targetComponentType = TypeCache.GetType(pieces[1]);

                                if(targetComponentType == null ||
                                    targetComponentType.IsAssignableTo(field.FieldType) == false)
                                {
                                    continue;
                                }

                                Entity targetEntity = default;

                                if(localIDs.TryGetValue(entityID, out var localEntityID))
                                {
                                    targetEntity = Scene.FindEntity(localEntityID);
                                }

                                if(targetEntity.IsValid == false ||
                                    targetEntity.TryGetComponent(targetComponentType, out var targetComponent) == false)
                                {
                                    continue;
                                }

                                field.SetValue(componentInstance, targetComponent);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Error($"[SceneSerialization] Failed to deserialize field {parameter.Key} for {component.type}: {e}");
                    }
                }
            }
        }

        HandleReferences(newEntity, prefab.mainObject);

        for(var i = 0; i < prefab.children.Count; i++)
        {
            var sceneObject = prefab.children[i];

            if(localEntities.TryGetValue(i + 1, out var entity) && entity.IsValid)
            {
                HandleReferences(entity, sceneObject);
            }
        }

        return newEntity;
    }
}
