using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Editor;

[CustomPropertyDrawer(typeof(EntityCallback))]
internal class EntityCallbackPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(string name, string ID, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute)
    {
        if (getter() is not EntityCallback value)
        {
            return;
        }

        EditorGUI.Label(name);

        EditorGUI.SameLine();

        EditorGUI.Button("+", $"{ID}.Add", () =>
        {
            value.AddPersistentCallback(new());
        });

        var skip = false;

        var counter = 0;

        var callbacks = value.PersistentCallbacks().ToArray();

        for (var i = 0; i < callbacks.Length; i++)
        {
            callbacks[i] ??= new();

            var callback = callbacks[i];

            EditorGUI.Indent(() =>
            {
                EditorGUI.Label($"Item {counter + 1}");

                var entity = Scene.FindEntity(callback.entityID);

                entity = EditorGUI.EntityField("Entity", entity, $"{ID}.{GetType().FullName}{i}");

                callback.entityID = entity.Identifier.ID;

                if (!entity.IsValid)
                {
                    EditorGUI.SameLine();

                    EditorGUI.Button("-", $"{ID}.CallbackRemove{counter}", () =>
                    {
                        counter++;

                        value.RemovePersistentCallback(callback);

                        skip = true;
                    });

                    return;
                }

                var componentTypes = new List<Type>();

                entity.IterateComponents((ref IComponent component) =>
                {
                    componentTypes.Add(component.GetType());
                });

                var index = componentTypes.FindIndex(x => x.FullName == callback.className);

                var newIndex = EditorGUI.Dropdown("Component", $"{ID}.Component{counter}", componentTypes.Select(x => x.Name).ToArray(), index);

                if (newIndex != index && newIndex < componentTypes.Count)
                {
                    callback.className = componentTypes[newIndex].FullName;
                }

                var type = componentTypes.FirstOrDefault(x => x.FullName == callback.className);

                if (type == null)
                {
                    EditorGUI.SameLine();

                    EditorGUI.Button("-", $"{ID}.CallbackRemove2{counter}", () =>
                    {
                        counter++;

                        value.RemovePersistentCallback(callback);

                        skip = true;
                    });

                    return;
                }

                var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                    .Where(x => x.GetParameters().Length == 0 &&
                        x.DeclaringType != typeof(object) &&
                        !x.IsAbstract &&
                        !x.Name.StartsWith("get_") &&
                        !x.Name.StartsWith("set_"))
                    .ToArray();

                var methodNames = methods.Select(x => x.Name).ToList();

                index = methodNames.IndexOf(callback.methodName);

                newIndex = EditorGUI.Dropdown("Method", $"{ID}.CallbackRemove3{counter}", methodNames.ToArray(), index);

                if (newIndex != index && newIndex < methods.Length)
                {
                    callback.methodName = methodNames[newIndex];
                }

                EditorGUI.SameLine();

                EditorGUI.Button("-", $"{ID}.CallbackRemove4{counter}", () =>
                {
                    counter++;

                    value.RemovePersistentCallback(callback);

                    skip = true;
                });

                if (skip)
                {
                    return;
                }

                counter++;
            });
        }

        if(EditorGUI.Changed)
        {
            setter(value);
        }
    }
}

[CustomPropertyDrawer(typeof(EntityCallback<>))]
internal class EntityCallbackGenericPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(string name, string ID, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute)
    {
        var value = getter();

        if (value == null ||
            !value.GetType().IsGenericType)
        {
            return;
        }

        var addPersistentCallbackMethod = value.GetType().GetMethod(nameof(EntityCallback.AddPersistentCallback),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var removePersistentCallbackMethod = value.GetType().GetMethod(nameof(EntityCallback.RemovePersistentCallback),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var persistentCallbacksMethod = value.GetType().GetMethod(nameof(EntityCallback.PersistentCallbacks),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (addPersistentCallbackMethod == null ||
            removePersistentCallbackMethod == null ||
            persistentCallbacksMethod == null)
        {
            return;
        }

        EditorGUI.Label(name);

        EditorGUI.SameLine();

        EditorGUI.Button("+", $"{ID}.Add", () =>
        {
            addPersistentCallbackMethod.Invoke(value, [0, null, null]);
        });

        var skip = false;

        var counter = 0;

        var callbacks = persistentCallbacksMethod.Invoke(value, null);

        if (callbacks is IEnumerable e)
        {
            foreach (var item in e)
            {
                if (item is not EntityCallback.EntityCallbackEntry entry)
                {
                    continue;
                }

                EditorGUI.Indent(() =>
                {
                    EditorGUI.Label($"Item {counter + 1}");

                    var entity = Scene.FindEntity(entry.entityID);

                    entity = EditorGUI.EntityField("Entity", entity, $"{ID}.{GetType().FullName}{counter}");

                    entry.entityID = entity.Identifier.ID;

                    if (!entity.IsValid)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"{ID}.CallbackRemove{counter}", () =>
                        {
                            counter++;

                            removePersistentCallbackMethod.Invoke(value, [entry]);

                            skip = true;
                        });

                        return;
                    }

                    var componentTypes = new List<Type>();

                    entity.IterateComponents((ref IComponent component) =>
                    {
                        componentTypes.Add(component.GetType());
                    });

                    var index = componentTypes.FindIndex(x => x.FullName == entry.className);

                    var newIndex = EditorGUI.Dropdown("Component", $"{ID}.Component{counter}", componentTypes.Select(x => x.Name).ToArray(), index);

                    if (newIndex != index && newIndex < componentTypes.Count)
                    {
                        entry.className = componentTypes[newIndex].FullName;
                    }

                    var type = componentTypes.FirstOrDefault(x => x.FullName == entry.className);

                    if (type == null)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"{ID}.CallbackRemove2{counter}", () =>
                        {
                            counter++;

                            removePersistentCallbackMethod.Invoke(value, [entry]);

                            skip = true;
                        });

                        return;
                    }

                    var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).ToList();

                    for(var i = methods.Count - 1; i >= 0; i--)
                    {
                        var method = methods[i];

                        var parameters = method.GetParameters();

                        var remove = parameters.Length != value.GetType().GenericTypeArguments.Length ||
                            method.DeclaringType == typeof(object) ||
                            method.IsAbstract ||
                            method.Name.StartsWith("get_") ||
                            method.Name.StartsWith("set_");

                        for(var j = 0; j < parameters.Length; j++)
                        {
                            if (!parameters[j].ParameterType.IsAssignableTo(value.GetType().GetGenericArguments()[j]))
                            {
                                remove = true;

                                break;
                            }
                        }

                        if(remove)
                        {
                            methods.RemoveAt(i);
                        }
                    }

                    var methodNames = methods.Select(x => x.Name).ToList();

                    index = methodNames.IndexOf(entry.methodName);

                    newIndex = EditorGUI.Dropdown("Method", $"{ID}.CallbackRemove3{counter}", methodNames.ToArray(), index);

                    if (newIndex != index && newIndex < methods.Count)
                    {
                        entry.methodName = methodNames[newIndex];
                    }

                    EditorGUI.SameLine();

                    EditorGUI.Button("-", $"{ID}.CallbackRemove4{counter}", () =>
                    {
                        counter++;

                        removePersistentCallbackMethod.Invoke(value, [entry]);

                        skip = true;
                    });
                });

                if (skip)
                {
                    break;
                }

                counter++;
            }
        }

        if (EditorGUI.Changed)
        {
            setter(value);
        }
    }
}
