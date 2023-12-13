using ImGuiNET;
using System;
using System.Collections.Generic;
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
        private static Type[] editorTypes;

        /// <summary>
        /// The original object that is being edited, if any
        /// </summary>
        public object original;

        /// <summary>
        /// The path to the asset
        /// </summary>
        public string path;

        /// <summary>
        /// The cache path to the asset
        /// </summary>
        public string cachePath;

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
                if(field.GetCustomAttribute<HideInInspectorAttribute>() != null || field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                if(RenderField(field))
                {
                    continue;
                }

                var type = field.FieldType;
                var fieldName = field.Name.ExpandCamelCaseName();

                switch (type)
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

                            var newValue = values[EditorGUI.Dropdown(fieldName, valueStrings, current)];

                            field.SetValue(target, newValue);
                        }

                        break;

                    case Type t when t == typeof(string):

                        {
                            var value = (string)field.GetValue(target);

                            var newValue = EditorGUI.TextField(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector2):

                        {
                            var value = (Vector2)field.GetValue(target);

                            var newValue = EditorGUI.Vector2Field(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector2Int):

                        {
                            var value = (Vector2Int)field.GetValue(target);

                            var newValue = EditorGUI.Vector2IntField(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector3):

                        {
                            var value = (Vector3)field.GetValue(target);

                            var newValue = EditorGUI.Vector3Field(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(Vector4):

                        {
                            var value = (Vector4)field.GetValue(target);

                            var newValue = EditorGUI.Vector4Field(fieldName, value);

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

                            var newValue = EditorGUI.Vector3Field(fieldName, value);

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

                            var newValue = (uint)value;

                            if(field.GetCustomAttribute<SortingLayerAttribute>() != null)
                            {
                                newValue = (uint)EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), LayerMask.AllSortingLayers.ToArray(), value);
                            }
                            else
                            {
                                newValue = (uint)EditorGUI.IntField(fieldName, value);
                            }

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(int):

                        {
                            var value = (int)field.GetValue(target);

                            var newValue = EditorGUI.IntField(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(bool):

                        {
                            var value = (bool)field.GetValue(target);

                            var newValue = EditorGUI.Toggle(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(float):

                        {
                            var value = (float)field.GetValue(target);

                            var newValue = EditorGUI.FloatField(fieldName, value);

                            if (newValue != value)
                            {
                                field.SetValue(target, newValue);
                            }
                        }

                        break;

                    case Type t when t == typeof(double):

                        {
                            var value = (double)field.GetValue(target);

                            if (ImGui.InputDouble(fieldName, ref value))
                            {
                                field.SetValue(target, value);
                            }
                        }

                        break;

                    case Type t when t == typeof(byte):

                        {
                            var current = (byte)field.GetValue(target);
                            var value = (int)current;

                            if (ImGui.InputInt(fieldName, ref value))
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

                            if (ImGui.InputInt(fieldName, ref value))
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

                            if (ImGui.InputInt(fieldName, ref value))
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

                            var newValue = EditorGUI.ColorField(fieldName, c);

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

                    case Type t when typeof(IGuidAsset).IsAssignableFrom(t):

                        {
                            var value = (IGuidAsset)field.GetValue(target);

                            var newValue = EditorGUI.ObjectPicker(t, fieldName, value);

                            field.SetValue(target, newValue);
                        }

                        break;

                    case Type t when t == typeof(LayerMask):

                        {
                            var value = (LayerMask)field.GetValue(target);
                            List<string> layers;

                            if (field.FieldType.GetCustomAttribute<SortingLayerAttribute>() != null)
                            {
                                layers = LayerMask.AllSortingLayers;
                            }
                            else
                            {
                                layers = LayerMask.AllLayers;
                            }

                            var previewValue = "";

                            var all = true;

                            for (var i = 0; i < layers.Count; i++)
                            {
                                if (value.HasLayer((uint)i) == false)
                                {
                                    all = false;
                                }
                                else
                                {
                                    previewValue += (previewValue.Length > 0 ? ", " : "") + layers[i];
                                }
                            }

                            if(all)
                            {
                                previewValue = "Everything";
                            }
                            else if(previewValue.Length == 0)
                            {
                                previewValue = "Nothing";
                            }

                            if (ImGui.BeginCombo(fieldName, previewValue))
                            {
                                for(var i = 0; i < layers.Count; i++)
                                {
                                    var selected = value.HasLayer((uint)i);

                                    var selectedString = selected ? "* " : "  ";

                                    if (ImGui.Selectable($"{selectedString}{layers[i]}##{value.GetHashCode()}", selected))
                                    {
                                        if(selected)
                                        {
                                            value.RemoveLayer((uint)i);
                                        }
                                        else
                                        {
                                            value.SetLayer((uint)i);
                                        }
                                    }
                                }

                                ImGui.EndCombo();

                                field.SetValue(target, value);
                            }
                        }

                        break;
                }
            }
        }

        public virtual void Destroy()
        {
        }

        internal static void UpdateEditorTypes()
        {
            editorTypes = Assembly.GetCallingAssembly().GetTypes()
                    .Concat(Assembly.GetExecutingAssembly().GetTypes())
                    .Concat(TypeCache.types.Select(x => x.Value))
                    .Where(x => x.IsSubclassOf(typeof(Editor)))
                    .Distinct()
                    .ToArray();
        }

        public static Editor CreateEditor(object target, Type editorType = null)
        {
            var types = editorTypes;

            if (editorType == null)
            {
                var targetType = target.GetType();

                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<CustomEditorAttribute>();

                    if (attribute == null || type.IsSubclassOf(typeof(Editor)) == false)
                    {
                        continue;
                    }

                    if (attribute.target == targetType)
                    {
                        editorType = type;

                        break;
                    }
                }
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

            var targetType = targets.FirstOrDefault().GetType();

            if (targets.Any(x => x.GetType() != targetType))
            {
                return null;
            }

            var types = editorTypes;

            if (editorType == null)
            {
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<CustomEditorAttribute>();

                    if (attribute == null || type.IsSubclassOf(typeof(Editor)) == false)
                    {
                        continue;
                    }

                    if (attribute.target == targetType)
                    {
                        editorType = type;

                        break;
                    }
                }
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
