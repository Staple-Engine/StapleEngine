using System;

namespace Staple.Editor;

public interface INodeUIObserver
{
    bool ValidateConnection(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to);

    (bool, Action) OnNodeClick(NodeUI nodeUI, NodeUI.Node node, MouseButton button);

    (bool, Action) OnLinkClick(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to, MouseButton button);

    (bool, Action) OnWorkspaceClick(NodeUI nodeUI, MouseButton button);
}
