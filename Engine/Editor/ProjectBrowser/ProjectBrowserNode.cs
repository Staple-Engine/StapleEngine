using System.Collections.Generic;

namespace Staple.Editor
{
    internal class ProjectBrowserNode
    {
        public string name;
        public string path;
        public ProjectBrowserNodeType type;
        public string extension;
        public string typeName;
        public List<ProjectBrowserNode> subnodes = new();
        public ProjectBrowserNodeAction action = ProjectBrowserNodeAction.None;
    }
}
