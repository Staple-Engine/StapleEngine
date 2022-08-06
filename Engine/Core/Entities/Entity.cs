using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if _DEBUG
[assembly: InternalsVisibleTo("CoreTests")]
#endif

namespace Staple
{
    public class Entity
    {
        internal List<Component> components = new List<Component>();

        public readonly string Name;

        public readonly Transform Transform;

        public string ID { get; internal set; }

        public uint layer;

        public Entity(string name)
        {
            Name = name;
            Transform = new Transform(this);
            ID = Guid.NewGuid().ToString();

            Scene.current?.AddEntity(this);
        }

        ~Entity()
        {
            Scene.current?.RemoveEntity(this);
        }

        internal static Entity Instantiate(SceneObject sceneObject)
        {
            var entity = new Entity(sceneObject.name)
            {
                ID = sceneObject.ID,
            };

            var rotation = sceneObject.transform.rotation.ToVector3();

            entity.Transform.LocalPosition = sceneObject.transform.position.ToVector3();
            entity.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
            entity.Transform.LocalScale = sceneObject.transform.scale.ToVector3();

            foreach (var component in sceneObject.components)
            {
                var type = Type.GetType(component.type) ?? AppPlayer.active?.playerAssembly?.GetType(component.type);

                if (type == null)
                {
                    continue;
                }

                var componentInstance = entity.AddComponent(type);

                if (componentInstance == null)
                {
                    continue;
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

                                        if (field.FieldType == typeof(int))
                                        {
                                            field.SetValue(componentInstance, parameter.intValue);
                                        }
                                        else if (field.FieldType == typeof(float))
                                        {
                                            field.SetValue(componentInstance, parameter.intValue);
                                        }

                                        break;

                                    case SceneComponentParameterType.String:

                                        if (field.FieldType == typeof(string))
                                        {
                                            field.SetValue(componentInstance, parameter.stringValue);

                                            continue;
                                        }

                                        if (field.FieldType.IsEnum)
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
                                            }

                                            continue;
                                        }

                                        if (field.FieldType == typeof(Color))
                                        {
                                            //TODO

                                            continue;
                                        }

                                        if (field.FieldType == typeof(Material))
                                        {
                                            var value = ResourceManager.instance.LoadMaterial(parameter.stringValue);

                                            if (value != null)
                                            {
                                                field.SetValue(componentInstance, value);
                                            }

                                            continue;
                                        }

                                        if (field.FieldType == typeof(Texture))
                                        {
                                            var value = ResourceManager.instance.LoadTexture(parameter.stringValue);

                                            if (value != null)
                                            {
                                                field.SetValue(componentInstance, value);
                                            }

                                            continue;
                                        }

                                        break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            return null;
                        }
                    }
                }
            }

            return entity;
        }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = null;

            foreach(var item in components)
            {
                if(item is T outValue)
                {
                    component = outValue;

                    return true;
                }
            }

            return false;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            foreach (var item in components)
            {
                if (item != null && item.GetType() == typeof(T))
                {
                    yield return (T)item;
                }
            }
        }

        public T GetComponent<T>() where T : Component
        {
            foreach(var item in components)
            {
                if(item != null && item.GetType() == typeof(T))
                {
                    return (T)item;
                }
            }

            return null;
        }

        internal bool HasComponents(params Type[] types)
        {
            for(var i = 0; i < types.Length; i++)
            {
                if (types[i].IsSubclassOf(typeof(Component)) == false)
                {
                    return false;
                }
            }

            for(var i = 0; i < types.Length; i++)
            {
                bool found = false;

                for(var j = 0; j < components.Count; j++)
                {
                    if (components[j] == null)
                    {
                        continue;
                    }

                    var type = components[j].GetType();

                    if (type == types[i] ||
                        type.IsSubclassOf(types[j]) ||
                        types[j].IsAssignableFrom(type))
                    {
                        found = true;

                        break;
                    }
                }

                if (found == false)
                {
                    return false;
                }
            }

            return true;
        }

        public Component AddComponent(Type t)
        {
            if(t.IsSubclassOf(typeof(Component)) == false)
            {
                return null;
            }

            try
            {
                var attributes = Attribute.GetCustomAttributes(t);

                foreach (var attribute in attributes)
                {
                    if (attribute is DisallowMultipleComponentAttribute && components.Any(x => x.GetType() == t))
                    {
                        //TODO: Log

                        return null;
                    }
                }

                var component = (Component)Activator.CreateInstance(t);

                if (component == null)
                {
                    return null;
                }

                component.Entity = new WeakReference<Entity>(this);

                components.Add(component);

                return component;
            }
            catch (Exception e)
            {
                //TODO: Log

                return null;
            }

        }

        public T AddComponent<T>() where T: Component
        {
            return (T)AddComponent(typeof(T));
        }
    }
}
