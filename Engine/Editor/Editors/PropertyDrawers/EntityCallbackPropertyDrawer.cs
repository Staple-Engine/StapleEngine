using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Editor;

[CustomPropertyDrawer(typeof(EntityCallback))]
internal class EntityCallbackPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(string name, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute)
    {
        if (getter() is not EntityCallback value)
        {
            return;
        }

        EditorGUI.Label(name);

        EditorGUI.SameLine();

        EditorGUI.Button("+", $"{name}Add", () =>
        {
            value.AddPersistentCallback(new());
        });

        var skip = false;

        var counter = 0;

        foreach(var callback in value.PersistentCallbacks())
        {
            callback.entityID = EditorGUI.IntField("Entity", $"{name}Entity{counter}", callback.entityID);

            var entity = Scene.FindEntity(callback.entityID);

            if(entity.IsValid == false)
            {
                EditorGUI.SameLine();

                EditorGUI.Button("-", $"{name}CallbackRemove{counter}", () =>
                {
                    counter++;

                    value.RemovePersistentCallback(callback);

                    skip = true;
                });

                if(skip)
                {
                    break;
                }

                continue;
            }

            var componentTypes = new List<Type>();

            entity.IterateComponents((ref IComponent component) =>
            {
                componentTypes.Add(component.GetType());
            });

            var index = componentTypes.FindIndex(x => x.FullName == callback.className);

            var newIndex = EditorGUI.Dropdown("Component", $"{name}Component{counter}", componentTypes.Select(x => x.Name).ToArray(), index);

            if(newIndex != index && newIndex < componentTypes.Count)
            {
                callback.className = componentTypes[newIndex].FullName;
            }

            var type = componentTypes.FirstOrDefault(x => x.FullName == callback.className);

            if(type == null)
            {
                EditorGUI.SameLine();

                EditorGUI.Button("-", $"{name}CallbackRemove2{counter}", () =>
                {
                    counter++;

                    value.RemovePersistentCallback(callback);

                    skip = true;
                });

                if (skip)
                {
                    break;
                }

                continue;
            }

            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(x => x.GetParameters().Length == 0 &&
                    x.DeclaringType != typeof(object) &&
                    x.IsAbstract == false &&
                    x.Name.StartsWith("get_") == false &&
                    x.Name.StartsWith("set_") == false)
                .ToArray();

            var methodNames = methods.Select(x => x.Name).ToList();

            index = methodNames.IndexOf(callback.methodName);

            newIndex = EditorGUI.Dropdown("Method", $"{name}CallbackRemove3{counter}", methodNames.ToArray(), index);

            if(newIndex != index && newIndex < methods.Length)
            {
                callback.methodName = methodNames[newIndex];
            }

            EditorGUI.SameLine();

            EditorGUI.Button("-", $"{name}CallbackRemove4{counter}", () =>
            {
                counter++;

                value.RemovePersistentCallback(callback);

                skip = true;
            });

            if(skip)
            {
                break;
            }

            counter++;
        }

        if(EditorGUI.Changed)
        {
            setter(value);
        }
    }
}
