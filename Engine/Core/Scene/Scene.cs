using Newtonsoft.Json.Linq;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

#if _DEBUG
[assembly: InternalsVisibleTo("CoreTests")]
#endif

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple
{
    public class Scene
    {
        internal World world = new World();

        public static Scene current { get; internal set; }

        internal static List<string> sceneList = new List<string>();

        public void Load(string path)
        {
            var scene = ResourceManager.instance.LoadScene(path);

            if(scene == null)
            {
                return;
            }

            current = scene;
        }

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

        public void DestroyEntity(Entity entity)
        {
            world.DestroyEntity(entity);
        }

        public IComponent AddComponent(Entity entity, Type t)
        {
            return world.AddComponent(entity, t);
        }

        public IComponent AddComponent<T>(Entity entity) where T: IComponent
        {
            return world.AddComponent<T>(entity);
        }

        public void UpdateComponent(Entity entity, IComponent component)
        {
            world.UpdateComponent(entity, component);
        }

        public void RemoveComponent(Entity entity, Type t)
        {
            world.RemoveComponent(entity, t);
        }

        public void RemoveComponent<T>(Entity entity) where T: IComponent
        {
            world.RemoveComponent<T>(entity);
        }

        public IComponent GetComponent(Entity entity, Type t)
        {
            return world.GetComponent(entity, t);
        }

        public T GetComponent<T>(Entity entity) where T: IComponent
        {
            return world.GetComponent<T>(entity);
        }

        internal Entity Instantiate(SceneObject sceneObject)
        {
            var entity = CreateEntity();

            var transform = GetComponent<Transform>(entity);

            var rotation = sceneObject.transform.rotation.ToVector3();

            transform.LocalPosition = sceneObject.transform.position.ToVector3();
            transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
            transform.LocalScale = sceneObject.transform.scale.ToVector3();

            foreach (var component in sceneObject.components)
            {
                var type = Type.GetType(component.type) ?? AppPlayer.active?.playerAssembly?.GetType(component.type);

                if (type == null)
                {
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

                        if (field != null && pair.Value != null)
                        {
                            if (field.FieldType == typeof(bool) && pair.Value.GetType() == typeof(bool))
                            {
                                field.SetValue(componentInstance, (bool)pair.Value);
                            }
                            else if (field.FieldType == typeof(float))
                            {
                                if (pair.Value.GetType() == typeof(float))
                                {
                                    field.SetValue(componentInstance, (float)pair.Value);
                                }
                                else if (pair.Value.GetType() == typeof(int))
                                {
                                    field.SetValue(componentInstance, (int)pair.Value);
                                }
                            }
                            else if (field.FieldType == typeof(int))
                            {
                                if (pair.Value.GetType() == typeof(float))
                                {
                                    field.SetValue(componentInstance, (int)((float)pair.Value));
                                }
                                else if (pair.Value.GetType() == typeof(int))
                                {
                                    field.SetValue(componentInstance, (int)pair.Value);
                                }
                            }
                            else if (field.FieldType == typeof(string) && pair.Value.GetType() == typeof(string))
                            {
                                field.SetValue(componentInstance, (string)pair.Value);
                            }
                            else if (field.FieldType.IsEnum && pair.Value.GetType() == typeof(string))
                            {
                                try
                                {
                                    var value = Enum.Parse(field.FieldType, (string)pair.Value);

                                    if (value != null)
                                    {
                                        field.SetValue(componentInstance, value);
                                    }
                                }
                                catch (Exception e)
                                {
                                }
                            }
                            else if (field.FieldType == typeof(Material) && pair.Value.GetType() == typeof(string))
                            {
                                var value = ResourceManager.instance.LoadMaterial((string)pair.Value);

                                if (value != null)
                                {
                                    field.SetValue(componentInstance, value);
                                }
                            }
                            else if (field.FieldType == typeof(Texture) && pair.Value.GetType() == typeof(string))
                            {
                                var value = ResourceManager.instance.LoadTexture((string)pair.Value);

                                if (value != null)
                                {
                                    field.SetValue(componentInstance, value);
                                }
                            }
                            else if ((field.FieldType == typeof(Color32) || field.FieldType == typeof(Color)))
                            {
                                var v = pair.Value;
                                var color = Color32.White;

                                if (v.GetType() == typeof(string))
                                {
                                    var value = (string)pair.Value;
                                    color = new Color32(value);
                                }
                                else if (v.GetType() == typeof(JObject))
                                {
                                    var o = (JObject)v;

                                    var r = o.GetValue("r").Value<int?>();
                                    var g = o.GetValue("g").Value<int?>();
                                    var b = o.GetValue("b").Value<int?>();
                                    var a = o.GetValue("a").Value<int?>();

                                    if (r == null ||
                                        g == null ||
                                        b == null ||
                                        a == null)
                                    {
                                        continue;
                                    }

                                    color = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
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
                            return Entity.Empty;
                        }
                    }
                }

                UpdateComponent(entity, componentInstance);
            }

            return entity;
        }

        internal Entity FindEntity(int ID)
        {
            return world.FindEntity(ID);
        }
    }
}
