using System;

namespace Staple.Editor
{
    public class EditorWindow
    {
        public string title;
        public bool allowDocking = true;
        public EditorWindowType windowType = EditorWindowType.Normal;
        public bool allowResize = true;
        public Vector2Int size = new(200, 300);
        public bool centerWindow = false;

        internal bool opened = false;

        public virtual void OnGUI()
        {
        }

        public void Close()
        {
            StapleEditor.instance.editorWindows.Remove(this);
        }

        public static T GetWindow<T>() where T : EditorWindow
        {
            foreach(var window in StapleEditor.instance.editorWindows)
            {
                if(window != null && window.GetType() == typeof(T))
                {
                    return (T)window;
                }
            }

            try
            {
                var result = (T)Activator.CreateInstance(typeof(T));

                if (result == null)
                {
                    return null;
                }

                result.title = typeof(T).Name;

                StapleEditor.instance.editorWindows.Add(result);

                return result;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
