using System;

namespace Staple.Editor;

public interface INodeUIObserver
{
    bool ValidateConnection(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to);

    (bool, Action) OnNodeRightClick(NodeUI nodeUI, NodeUI.Node node);

    (bool, Action) OnLinkRightClick(NodeUI nodeUI, (NodeUI.NodeSocket, NodeUI.NodeSocket) link);

    (bool, Action) OnWorkspaceRightClick(NodeUI nodeUI);
}
