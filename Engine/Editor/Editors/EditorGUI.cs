using ImGuiNET;

namespace Staple.Editor
{
    public static class EditorGUI
    {
        internal static ImGuiIOPtr io;

        public static int Dropbox(string label, string[] options, int current)
        {
            if(ImGui.Combo(label, ref current, string.Join("\0", options)))
            {
                return current;
            }

            return current;
        }

        public static string TextField(string label, string value, int maxLength = 1000)
        {
            if(ImGui.InputText(label, ref value, (uint)maxLength))
            {
                return value;
            }

            return value;
        }
    }
}
