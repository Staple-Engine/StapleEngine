using System;
using System.Linq;
using System.Reflection;

namespace Staple.Editor
{
    public class Editor
    {
        public object target;
        public object[] targets;

        public virtual void OnInspectorGUI()
        {
        }

        public static Editor CreateEditor(object target, Type editorType = null)
        {
            if(editorType == null)
            {
                var type = target.GetType();

                editorType = Assembly.GetCallingAssembly().GetTypes()
                    .Concat(Assembly.GetExecutingAssembly().GetTypes())
                    .FirstOrDefault(x =>
                    {
                        var attribute = x.GetCustomAttribute<CustomEditorAttribute>();

                        if(attribute == null || x.IsSubclassOf(typeof(Editor)) == false)
                        {
                            return false;
                        }

                        if(attribute.target.IsSubclassOf(typeof(Attribute)))
                        {
                            return type == attribute.target || type.GetCustomAttribute(attribute.target) != null;
                        }
                        else
                        {
                            return attribute.target == type;
                        }
                    });
            }

            if(editorType == null)
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
            catch(Exception)
            {
                return null;
            }
        }

        public static Editor CreateEditor(object[] targets, Type editorType = null)
        {
            if(targets.Length == 0)
            {
                return null;
            }

            if(targets.Any(x => x == null))
            {
                return null;
            }

            var type = targets.FirstOrDefault().GetType();

            if(targets.Any(x => x.GetType() != type))
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

                        if (attribute.target.IsSubclassOf(typeof(Attribute)))
                        {
                            return type == attribute.target || type.GetCustomAttribute(attribute.target) != null;
                        }
                        else
                        {
                            return attribute.target == type;
                        }
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
