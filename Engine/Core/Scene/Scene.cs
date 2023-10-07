using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;

namespace Staple
{
    public class Scene
    {
        internal World world = new();

        /// <summary>
        /// The currently active scene
        /// </summary>
        public static Scene current { get; internal set; }

        /// <summary>
        /// A list of all scenes we can load
        /// </summary>
        internal static List<string> sceneList = new();

        /// <summary>
        /// Creates an empty entity
        /// </summary>
        /// <returns>The new entity</returns>
        public Entity CreateEntity()
        {
            var e = world.CreateEntity();

            var transform = world.AddComponent<Transform>(e);

            if(transform != null)
            {
                transform.entity = e;
            }

            return e;
        }

        /// <summary>
        /// Destroys an entity
        /// </summary>
        /// <param name="entity">The entity to destroy</param>
        public void DestroyEntity(Entity entity)
        {
            world.DestroyEntity(entity);
        }

        /// <summary>
        /// Adds a component to an entity
        /// </summary>
        /// <param name="entity">The entity to add the component to</param>
        /// <param name="t">The component's type</param>
        /// <returns>The component's instance</returns>
        public IComponent AddComponent(Entity entity,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type t)
        {
            return world.AddComponent(entity, t);
        }

        /// <summary>
        /// Adds a component to an entity
        /// </summary>
        /// <typeparam name="T">The component's type</typeparam>
        /// <param name="entity">The entity to add the component to</param>
        /// <returns>The component's instance</returns>
        public T AddComponent
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
            (Entity entity) where T: IComponent
        {
            return world.AddComponent<T>(entity);
        }

        /// <summary>
        /// Updates an entity's component
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="component">The component instance</param>
        public void UpdateComponent(Entity entity, IComponent component)
        {
            world.UpdateComponent(entity, component);
        }

        /// <summary>
        /// Removes a component from an entity
        /// </summary>
        /// <param name="entity">The entity to remove the component from</param>
        /// <param name="t">The type of the component to remove</param>
        public void RemoveComponent(Entity entity, Type t)
        {
            world.RemoveComponent(entity, t);
        }

        /// <summary>
        /// Removes a component from an entity
        /// </summary>
        /// <typeparam name="T">The type of the component to remove</typeparam>
        /// <param name="entity">The entity to remove the component from</param>
        public void RemoveComponent<T>(Entity entity) where T: IComponent
        {
            world.RemoveComponent<T>(entity);
        }

        /// <summary>
        /// Gets a component from an entity
        /// </summary>
        /// <param name="entity">The entity to get the component from</param>
        /// <param name="t">The component's type</param>
        /// <returns>The component instance, or default</returns>
        public IComponent GetComponent(Entity entity, Type t)
        {
            return world.GetComponent(entity, t);
        }

        /// <summary>
        /// Gets a component from an entity
        /// </summary>
        /// <typeparam name="T">The component's type</typeparam>
        /// <param name="entity">The entity to get the component from</param>
        /// <returns>The component instance, or default</returns>
        public T GetComponent<T>(Entity entity) where T: IComponent
        {
            return world.GetComponent<T>(entity);
        }

        /// <summary>
        /// Instantiates a scene object
        /// </summary>
        /// <param name="sceneObject">The scene object to instantiate</param>
        /// <param name="localID">The local ID of the entity</param>
        /// <param name="activate">Whether to activate the object and call lifecycle callbacks</param>
        /// <returns>The new entity, or Entity.Empty</returns>
        internal Entity Instantiate(SceneObject sceneObject, out int localID, bool activate)
        {
            localID = sceneObject.ID;

            var entity = CreateEntity();

            world.SetEntityName(entity, sceneObject.name);
            world.SetEntityEnabled(entity, sceneObject.enabled);

            var layer = LayerMask.NameToLayer(sceneObject.layer);

            if(layer >= 0)
            {
                world.SetEntityLayer(entity, (uint)layer);
            }

            var transform = GetComponent<Transform>(entity);

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

                var componentInstance = AddComponent(entity, type);

                if (componentInstance == null)
                {
                    continue;
                }

                if (component.data != null)
                {
                    foreach (var pair in component.data)
                    {
                        var field = type.GetField(pair.Key);

                        if (field != null && pair.Value != null && pair.Value is JsonElement element)
                        {
                            if (field.FieldType == typeof(bool) && (element.ValueKind == JsonValueKind.False || element.ValueKind == JsonValueKind.True))
                            {
                                field.SetValue(componentInstance, element.GetBoolean());
                            }
                            else if (field.FieldType == typeof(float) && element.ValueKind == JsonValueKind.Number)
                            {
                                field.SetValue(componentInstance, element.GetSingle());
                            }
                            else if (field.FieldType == typeof(int) && element.ValueKind == JsonValueKind.Number)
                            {
                                field.SetValue(componentInstance, element.GetInt32());
                            }
                            else if (field.FieldType == typeof(string) && element.ValueKind == JsonValueKind.String)
                            {
                                field.SetValue(componentInstance, element.GetString());
                            }
                            else if (field.FieldType.IsEnum && element.ValueKind == JsonValueKind.String)
                            {
                                if(Enum.TryParse(field.FieldType, element.GetString(), true, out var value))
                                {
                                    field.SetValue(componentInstance, value);
                                }
                            }
                            else if(field.FieldType.GetInterface(typeof(IPathAsset).FullName) != null && element.ValueKind == JsonValueKind.String)
                            {
                                var path = element.GetString();

                                var value = AssetSerialization.GetPathAsset(field.FieldType, path);

                                if(value != null)
                                {
                                    field.SetValue(componentInstance, value);
                                }
                            }
                            else if ((field.FieldType == typeof(Color32) || field.FieldType == typeof(Color)))
                            {
                                var color = Color32.White;

                                if (element.ValueKind == JsonValueKind.String)
                                {
                                    color = new Color32(element.GetString());
                                }
                                else if (element.ValueKind == JsonValueKind.Object)
                                {
                                    try
                                    {
                                        var r = element.GetProperty("r").GetInt32();
                                        var g = element.GetProperty("g").GetInt32();
                                        var b = element.GetProperty("b").GetInt32();
                                        var a = element.GetProperty("a").GetInt32();

                                        color = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
                                    }
                                    catch(Exception e)
                                    {
                                        continue;
                                    }
                                }

                                if (field.FieldType == typeof(Color32))
                                {
                                    field.SetValue(componentInstance, color);
                                }
                                else
                                {
                                    field.SetValue(componentInstance, (Color)color);
                                }
                            }
                            else if(field.FieldType == typeof(Mesh) && element.ValueKind == JsonValueKind.String)
                            {
                                var value = ResourceManager.instance.LoadMesh(element.GetString());

                                if(value != null)
                                {
                                    field.SetValue(componentInstance, value);
                                }
                            }
                        }
                    }
                }

                if (component.parameters != null)
                {
                    foreach (var parameter in component.parameters)
                    {
                        if (parameter.name == null)
                        {
                            continue;
                        }

                        try
                        {
                            var field = type.GetField(parameter.name);

                            if (field != null)
                            {
                                switch (parameter.type)
                                {
                                    case SceneComponentParameterType.Bool:

                                        if (field.FieldType == typeof(bool))
                                        {
                                            field.SetValue(componentInstance, parameter.boolValue);
                                        }

                                        break;

                                    case SceneComponentParameterType.Float:

                                        if (field.FieldType == typeof(float))
                                        {
                                            field.SetValue(componentInstance, parameter.floatValue);
                                        }
                                        else if (field.FieldType == typeof(int))
                                        {
                                            field.SetValue(componentInstance, (int)parameter.floatValue);
                                        }

                                        break;

                                    case SceneComponentParameterType.Int:

                                        try
                                        {
                                            var value = System.Convert.ChangeType(parameter.intValue, field.FieldType);

                                            field.SetValue(componentInstance, value);
                                        }
                                        catch (Exception e)
                                        {
                                            continue;
                                        }

                                        break;

                                    case SceneComponentParameterType.Vector2:

                                        if(field.FieldType == typeof(Vector2))
                                        {
                                            field.SetValue(componentInstance, parameter.vector2Value.ToVector2());
                                        }
                                        else if(field.FieldType == typeof(Vector3))
                                        {
                                            field.SetValue(componentInstance, parameter.vector2Value.ToVector3());
                                        }
                                        else if (field.FieldType == typeof(Vector4))
                                        {
                                            field.SetValue(componentInstance, parameter.vector2Value.ToVector4());
                                        }

                                        break;

                                    case SceneComponentParameterType.Vector3:

                                        if (field.FieldType == typeof(Vector3))
                                        {
                                            field.SetValue(componentInstance, parameter.vector3Value.ToVector3());
                                        }
                                        else if (field.FieldType == typeof(Vector4))
                                        {
                                            field.SetValue(componentInstance, parameter.vector3Value.ToVector4());
                                        }
                                        else if(field.FieldType == typeof(Quaternion))
                                        {
                                            field.SetValue(componentInstance, parameter.vector3Value.ToQuaternion());
                                        }

                                        break;

                                    case SceneComponentParameterType.Vector4:

                                        if (field.FieldType == typeof(Vector4))
                                        {
                                            field.SetValue(componentInstance, parameter.vector4Value.ToVector4());
                                        }

                                        break;

                                    case SceneComponentParameterType.String:

                                        if (field.FieldType == typeof(string))
                                        {
                                            field.SetValue(componentInstance, parameter.stringValue);
                                        }
                                        else if (field.FieldType.IsEnum)
                                        {
                                            try
                                            {
                                                var value = Enum.Parse(field.FieldType, parameter.stringValue);

                                                if (value != null)
                                                {
                                                    field.SetValue(componentInstance, value);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                continue;
                                            };
                                        }
                                        else if ((field.FieldType == typeof(Color32) || field.FieldType == typeof(Color)))
                                        {
                                            var color = new Color32(parameter.stringValue);

                                            if (field.FieldType == typeof(Color32))
                                            {
                                                field.SetValue(componentInstance, color);
                                            }
                                            else
                                            {
                                                field.SetValue(componentInstance, (Color)color);
                                            }
                                        }
                                        else if(field.FieldType.GetInterface(typeof(IPathAsset).FullName) != null)
                                        {
                                            var path = parameter.stringValue;

                                            var value = AssetSerialization.GetPathAsset(field.FieldType, path);

                                            if (value != null)
                                            {
                                                field.SetValue(componentInstance, value);
                                            }
                                        }
                                        else if(field.FieldType == typeof(LayerMask))
                                        {
                                            var mask = new LayerMask();

                                            if(parameter.stringValue.ToUpperInvariant() == "EVERYTHING")
                                            {
                                                mask = LayerMask.Everything;
                                            }
                                            else
                                            {
                                                var layers = LayerMask.GetMask(parameter.stringValue.Split(" ".ToCharArray()));

                                                mask.value = layers;
                                            }

                                            field.SetValue(componentInstance, mask);
                                        }

                                        break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            return Entity.Empty;
                        }
                    }
                }

                UpdateComponent(entity, componentInstance);
            }

            if(activate)
            {
                world.IterateComponents(entity, (ref IComponent c) =>
                {
                    c.Invoke("Awake", entity, transform);
                });
            }

            return entity;
        }

        /// <summary>
        /// Attempts to find an entity by ID
        /// </summary>
        /// <param name="ID">The entity's ID</param>
        /// <returns>an Entity, or Entity.Empty</returns>
        internal Entity FindEntity(int ID)
        {
            return world.FindEntity(ID);
        }
    }
}
