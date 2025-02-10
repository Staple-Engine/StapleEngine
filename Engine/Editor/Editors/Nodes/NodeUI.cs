using Hexa.NET.ImNodes;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System;
using Hexa.NET.ImGui;

namespace Staple.Editor;

public partial class NodeUI
{
    public enum PinShape
    {
        Circle = ImNodesPinShape.Circle,
        CircleFilled = ImNodesPinShape.CircleFilled,
        Triangle = ImNodesPinShape.Triangle,
        TriangleFilled = ImNodesPinShape.TriangleFilled,
        Quad = ImNodesPinShape.Quad,
        QuadFilled = ImNodesPinShape.QuadFilled,
    }

    public enum MinimapCorner
    {
        TopLeft = ImNodesMiniMapLocation.TopLeft,
        TopRight = ImNodesMiniMapLocation.TopRight,
        BottomLeft = ImNodesMiniMapLocation.BottomLeft,
        BottomRight = ImNodesMiniMapLocation.BottomRight,
    }

    public enum SocketType
    {
        Input,
        Output,
    }

    public class NodeSocket
    {
        internal Node node;
        internal List<Node> targets = [];
        internal NodeConnector connector;

        public Node Node => node;

        public int TargetCount => targets.Count;

        public Node GetTarget(int index) => index >= 0 && index < targets.Count ? targets[index] : null;

        public string Name => connector.name;

        public PinShape Shape => connector.shape;

        public SocketType SocketType => connector.socketType;

        public void Connect(NodeSocket target)
        {
            connector.connections.Add(target.connector.ID);

            targets.Add(target.Node);
        }
    }

    public class Node
    {
        internal InternalNode node;
        internal List<NodeSocket> inputs = [];
        internal List<NodeSocket> outputs = [];

        public int ID => node.ID;

        public string Title
        {
            get => node.title;

            set => node.title = value;
        }

        public int InputCount => inputs.Count;

        public int OutputCount => outputs.Count;

        public NodeSocket GetInput(int index) => index >= 0 && index < inputs.Count ? inputs[index] : null;

        public NodeSocket GetOutput(int index) => index >= 0 && index < outputs.Count ? outputs[index] : null;

        public NodeSocket GetInput(string name) => inputs.FirstOrDefault(x => x.Name == name);

        public NodeSocket GetOutput(string name) => outputs.FirstOrDefault(x => x.Name == name);

        public NodeSocket GetInputById(int id) => inputs.FirstOrDefault(x => x.connector.ID == id);

        public NodeSocket GetOutputById(int id) => outputs.FirstOrDefault(x => x.connector.ID == id);
    }

    private INodeUIObserver observer;

    public record class Connector(string name, PinShape shape);

    public IEnumerable<Node> Nodes => userNodes;

    public int NodeCount => userNodes.Count;

    public bool showMinimap = false;

    public MinimapCorner minimapCorner = MinimapCorner.BottomLeft;

    public float minimapFraction = 0.1f;

    public float usedSpace;

    public NodeUI(INodeUIObserver observer)
    {
        this.observer = observer;
    }

    public void DoLayout()
    {
        RenderNodes(() =>
        {
            foreach (var node in nodes)
            {
                MakeNode(node.Key, () => EditorGUI.Label(node.Value.title),
                    () =>
                    {
                        foreach (var input in node.Value.inputs)
                        {
                            NodeInput(input.ID, input.shape, () => EditorGUI.Label(input.name));
                        }

                        foreach (var output in node.Value.outputs)
                        {
                            NodeOutput(output.ID, output.shape, () => EditorGUI.Label(output.name));
                        }

                        try
                        {
                            node.Value.content?.Invoke(node.Value.node);
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"[{GetType().Name}] {e}");
                        }
                    });
            }

            for (var i = 0; i < links.Count; i++)
            {
                Link(i, links[i].Item1, links[i].Item2);
            }

            if (showMinimap)
            {
                Minimap(minimapFraction, minimapCorner);
            }
        });

        int startNode = 0;
        int startAttribute = 0;
        int endNode = 0;
        int endAttribute = 0;
        bool fromSnap = false;

        if (ImNodes.IsLinkCreatedIntPtr(ref startNode, ref startAttribute, ref endNode, ref endAttribute, ref fromSnap))
        {
            if (nodes.TryGetValue(startNode, out var start) &&
                nodes.TryGetValue(endNode, out var end))
            {
                var startConnectorUser = start.node.GetOutputById(startAttribute);
                var endConnectorUser = end.node.GetInputById(endAttribute);

                if (startConnectorUser != null && endConnectorUser != null &&
                    (observer?.ValidateConnection(this, startConnectorUser, endConnectorUser) ?? true))
                {
                    CreateLink(startConnectorUser, endConnectorUser);
                }
            }
        }

        if (Input.GetMouseButtonUp(MouseButton.Left) ||
            Input.GetMouseButtonUp(MouseButton.Right))
        {
            var button = Input.GetMouseButtonUp(MouseButton.Left) ? MouseButton.Left : MouseButton.Right;

            void ShowPopup((bool, Action) handler)
            {
                if (handler.Item1)
                {
                    popupContent = handler.Item2;

                    ImGui.OpenPopup("NODEUIPOPUP");
                }
            }

            if(observer != null)
            {
                (bool, Action) r = (false, null);

                foreach (var pair in nodes)
                {
                    if (ImNodes.IsNodeSelected(pair.Key))
                    {
                        if (observer != null &&
                            nodes.TryGetValue(pair.Key, out var i))
                        {
                            r = observer.OnNodeClick(this, i.node, button);

                            ShowPopup(r);

                            return;
                        }
                    }
                }

                for (var i = 0; i < links.Count; i++)
                {
                    if (ImNodes.IsLinkSelected(i) &&
                        connectors.TryGetValue(links[i].Item1, out var from) &&
                        connectors.TryGetValue(links[i].Item2, out var to))
                    {
                        r = observer.OnLinkClick(this, from.socket, to.socket, button);

                        ShowPopup(r);

                        return;
                    }
                }

                r = observer.OnWorkspaceClick(this, button);

                ShowPopup(r);
            }
        }

        if (ImGui.BeginPopup("NODEUIPOPUP"))
        {
            try
            {
                popupContent?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}]: {e}");
            }

            ImGui.EndPopup();
        }
    }

    #region User API

    public Node CreateNode(string title, Connector[] inputs, Connector[] outputs, Action<Node> content = null)
    {
        nodeCounter++;

        var node = new InternalNode()
        {
            ID = nodeCounter,
            title = title,
            content = content,

            inputs = inputs
                .Select(x => new NodeConnector()
                {
                    ID = ++connectorCounter,
                    name = x.name,
                    shape = x.shape,
                    nodeID = nodeCounter,
                    socketType = SocketType.Input,
                })
                .ToList(),

            outputs = outputs
                .Select(x => new NodeConnector()
                {
                    ID = ++connectorCounter,
                    name = x.name,
                    shape = x.shape,
                    nodeID = nodeCounter,
                    socketType = SocketType.Output,
                })
                .ToList(),
        };

        foreach(var t in node.inputs)
        {
            connectors.Add(t.ID, t);
        }

        foreach (var t in node.outputs)
        {
            connectors.Add(t.ID, t);
        }

        var outNode = new Node()
        {
            node = node,
        };

        for(var i = 0; i < node.inputs.Count; i++)
        {
            outNode.inputs.Add(new()
            {
                connector = node.inputs[i],
                node = outNode,
            });

            node.inputs[i].socket = outNode.inputs[i];
        }

        for (var i = 0; i < node.outputs.Count; i++)
        {
            outNode.outputs.Add(new()
            {
                connector = node.outputs[i],
                node = outNode,
            });

            node.outputs[i].socket = outNode.outputs[i];
        }

        node.node = outNode;

        nodes.Add(nodeCounter, node);

        userNodes.Add(outNode);

        return outNode;
    }

    public void CreateLink(NodeSocket start, NodeSocket end)
    {
        start.connector.connections.Add(end.connector.ID);
        end.connector.connections.Add(start.connector.ID);

        start.targets.Add(end.Node);
        end.targets.Add(start.Node);

        links.Add((start.connector.ID, end.connector.ID));
    }

    public void DeleteNode(Node node)
    {
        if (node == null ||
            node.node == null)
        {
            return;
        }

        nodes.Remove(node.node.ID);

        foreach (var input in node.inputs)
        {
            for (var i = links.Count - 1; i >= 0; i--)
            {
                if (links[i].Item1 == input.connector.ID || links[i].Item2 == input.connector.ID)
                {
                    links.RemoveAt(i);
                }
            }

            for (var i = input.connector.connections.Count - 1; i >= 0; i--)
            {
                if (input.connector.connections[i] == node.node.ID)
                {
                    input.connector.connections.RemoveAt(i);
                }
            }
        }

        foreach (var output in node.outputs)
        {
            for (var i = links.Count - 1; i >= 0; i--)
            {
                if (links[i].Item1 == output.connector.ID || links[i].Item2 == output.connector.ID)
                {
                    links.RemoveAt(i);
                }
            }

            for (var i = output.connector.connections.Count - 1; i >= 0; i--)
            {
                if (output.connector.connections[i] == node.node.ID)
                {
                    output.connector.connections.RemoveAt(i);
                }
            }
        }
    }

    public void DeleteLink(NodeSocket from, NodeSocket to)
    {
        for (var i = 0; i < links.Count; i++)
        {
            if (links[i].Item1 == from.connector.ID &&
                links[i].Item2 == to.connector.ID)
            {
                links.RemoveAt(i);

                return;
            }
        }
    }

    public void Minimap(float sizeFraction, MinimapCorner corner)
    {
        var c = corner switch
        {
            MinimapCorner.BottomLeft => ImNodesMiniMapLocation.BottomLeft,
            MinimapCorner.BottomRight => ImNodesMiniMapLocation.BottomRight,
            MinimapCorner.TopLeft => ImNodesMiniMapLocation.TopLeft,
            MinimapCorner.TopRight => ImNodesMiniMapLocation.TopRight,
            _ => ImNodesMiniMapLocation.BottomLeft,
        };

        ImNodes.MiniMap(sizeFraction, c, default, default);
    }

    public Vector2 GetNodePosition(Node node)
    {
        return ImNodes.GetNodeEditorSpacePos(node.node.ID);
    }

    public void SetNodePosition(Node node, Vector2 pos)
    {
        ImNodes.SetNodeEditorSpacePos(node.node.ID, pos);
    }

    public Vector2 GetNodeSize(Node node)
    {
        return ImNodes.GetNodeDimensions(node.node.ID);
    }

    #endregion
}
