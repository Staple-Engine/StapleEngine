using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor
{
    /// <summary>
    /// Editor class that renders the inspector for one or more targets
    /// </summary>
    public class Editor
    {
        /// <summary>
        /// The target to edit
        /// </summary>
        public object target;

        /// <summary>
        /// All targets to edit (if supports multiple objects)
        /// </summary>
        public object[] targets;

        /// <summary>
        /// Attempts to render a field.
        /// Override this to change how a field appears.
        /// Return true if you rendered the field or false if you want to use default rendering
        /// </summary>
        /// <param name="field">The field to render</param>
        /// <returns>Whether the field was rendered, or to use default rendering</returns>
        public virtual bool RenderField(FieldInfo field)
        {
            return false;
        }

        /// <summary>
        /// Called to render the inspector UI.
        /// If not overriden, will inspect each public field and render it.
        /// </summary>
        public virtual void OnInspectorGUI()
        {
            var fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if(RenderField(field))
                {
                    continue;
                }

                var type = field.FieldType;

                switch(type)
                {
                    case Type t when t.IsEnum:

                        {
                            var values = Enum.GetValues(type)
                                .OfType<Enum>()
                                .ToList();

                            var value = (Enum)field.GetValue(target);

                            var current = values.IndexOf(value);

                            var valueStrings = values
                                .Select(x => x.ToString())
                                .ToArray();

                            var newValue = values[EditorGUI.Dropdown(field.Name, valueStrings, current)];

                            field.SetValue(target, newValue);
                        }

                        break;

                    case Type t when t == typeof(string):

                        {
                            var value = (string)field.GetValue(target);

                            var newValue = EditorGUI.TextField(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector2):

                        {
                            var value = (Vector2)field.GetValue(target);

                            var newValue = EditorGUI.Vector2Field(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector3):

                        {
                            var value = (Vector3)field.GetValue(target);

                            var newValue = EditorGUI.Vector3Field(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector4):

                        {
                            var value = (Vector4)field.GetValue(target);

                            var newValue = EditorGUI.Vector4Field(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Quaternion):

                        {
                            var quaternion = (Quaternion)field.GetValue(target);

                            var value = Math.ToEulerAngles(quaternion);

                            var newValue = EditorGUI.Vector3Field(field.Name, value);

                            if (newValue != value)
                            {
                                quaternion = Math.FromEulerAngles(newValue);

                                field.SetValue(target, quaternion);
                            }
                        }

                        break;

                    case Type t when t == typeof(uint):

                        {
                            var value = (int)(uint)field.GetValue(target);

                            var newValue = EditorGUI.IntField(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, (uint)newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(int):

                        {
                            var value = (int)field.GetValue(target);

                            var newValue = EditorGUI.IntField(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(bool):

                        {
                            var value = (bool)field.GetValue(target);

                            var newValue = EditorGUI.Toggle(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(float):

                        {
                            var value = (float)field.GetValue(target);

                            var newValue = EditorGUI.FloatField(field.Name, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(double):

                        {
                            var value = (double)field.GetValue(target);

                            if (ImGui.InputDouble(field.Name, ref value))
                            {
                                field.SetValue(target, value);
                            }
                        }

                        break;

                    case Type t when t == typeof(byte):

                        {
                            var current = (byte)field.GetValue(target);
                            var value = (int)current;

                            if (ImGui.InputInt(field.Name, ref value))
                            {
                                if (value < 0)
                                {
                                    value = 0;
                                }

                                if (value > 255)
                                {
                                    value = 255;
                                }

                                field.SetValue(target, (byte)value);
                            }
                        }

                        break;

                    case Type t when t == typeof(short):

                        {
                            var current = (short)field.GetValue(target);
                            var value = (int)current;

                            if (ImGui.InputInt(field.Name, ref value))
                            {
                                if (value < short.MinValue)
                                {
                                    value = short.MinValue;
                                }

                                if (value > short.MaxValue)
                                {
                                    value = short.MaxValue;
                                }

                                field.SetValue(target, (short)value);
                            }
                        }

                        break;

                    case Type t when t == typeof(ushort):

                        {
                            var current = (ushort)field.GetValue(target);
                            var value = (int)current;

                            if (ImGui.InputInt(field.Name, ref value))
                            {
                                if (value < ushort.MinValue)
                                {
                                    value = ushort.MinValue;
                                }

                                if (value > ushort.MaxValue)
                                {
                                    value = ushort.MaxValue;
                                }

                                field.SetValue(target, (ushort)value);
                            }
                        }

                        break;

                    case Type t when t == typeof(Color) || t == typeof(Color32):

                        {
                            Color c;

                            if (type == typeof(Color))
                            {
                                c = (Color)field.GetValue(target);
                            }
                            else
                            {
                                c = (Color)((Color32)field.GetValue(target));
                            }

                            var newValue = EditorGUI.ColorField(field.Name, c);

                            if (newValue != c)
                            {
                                if (type == typeof(Color))
                                {
                                    field.SetValue(target, newValue);
                                }
                                else
                                {
                                    var c2 = (Color32)newValue;

                                    field.SetValue(target, c2);
                                }
                            }
                        }

                        break;
                }
            }
        }

        public static Editor CreateEditor(object target, Type editorType = null)
        {
            if (editorType == null)
            {
                var type = target.GetType();

                editorType = Assembly.GetCallingAssembly().GetTypes()
                    .Concat(Assembly.GetExecutingAssembly().GetTypes())
                    .FirstOrDefault(x =>
                    {
                        var attribute = x.GetCustomAttribute<CustomEditorAttribute>();

                        if (attribute == null || x.IsSubclassOf(typeof(Editor)) == false)
                        {
                            return false;
                        }

                        return attribute.target == type;
                    });
            }

            if (editorType == null)
            {
                return null;
            }

            try
            {
                var instance = (Editor)Activator.CreateInstance(editorType);

                instance.target = target;
                instance.targets = new object[] { target };

                return instance;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Editor CreateEditor(object[] targets, Type editorType = null)
        {
            if (targets.Length == 0)
            {
                return null;
            }

            if (targets.Any(x => x == null))
            {
                return null;
            }

            var type = targets.FirstOrDefault().GetType();

            if (targets.Any(x => x.GetType() != type))
            {
                return null;
            }

            if (editorType == null)
            {
                editorType = Assembly.GetCallingAssembly().GetTypes()
                    .Concat(Assembly.GetExecutingAssembly().GetTypes())
                    .FirstOrDefault(x =>
                    {
                        var attribute = x.GetCustomAttribute<CustomEditorAttribute>();

                        if (attribute == null || x.IsSubclassOf(typeof(Editor)) == false)
                        {
                            return false;
                        }

                        return attribute.target == type;
                    });
            }

            if (editorType == null)
            {
                return null;
            }

            try
            {
                var instance = (Editor)Activator.CreateInstance(editorType);

                instance.targets = targets;

                return instance;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
