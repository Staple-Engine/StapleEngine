using Hexa.NET.ImNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Editor;

public class NodeEditor : Editor
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

    internal class InternalNode
    {
        public int ID;
        public string title;
        public List<NodeConnector> inputs = [];
        public List<NodeConnector> outputs = [];
        public Node node;

        public Action content;
    }

    internal class NodeConnector
    {
        public int ID;
        public int nodeID;
        public List<int> connections = [];
        public SocketType socketType;
        public PinShape shape;
        public string name;
    }

    public class NodeSocket
    {
        internal Node node;
        internal List<Node> targets = [];
        internal NodeConnector connector;

        public Node Node => node;

        public IEnumerable<Node> Targets => targets;

        public string Name => connector.name;

        public PinShape Shape => connector.shape;

        public SocketType SocketType => connector.socketType;
    }

    public class Node
    {
        internal InternalNode node;
        internal List<NodeSocket> inputs = [];
        internal List<NodeSocket> outputs = [];

        public IEnumerable<NodeSocket> Inputs => inputs;

        public IEnumerable<NodeSocket> Outputs => outputs;
    }

    public record class Connector(string name, PinShape shape);

    private readonly Dictionary<int, InternalNode> nodes = [];
    private readonly Dictionary<int, NodeConnector> connectors = [];
    private readonly List<(int, int)> links = [];
    private readonly List<Node> userNodes = [];
    private int nodeCounter = 0;
    private int connectorCounter = 0;

    public IEnumerable<Node> Nodes => userNodes;

    public Node CreateNode(string title, Connector[] inputs, Connector[] outputs, Action content = null)
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

        var outNode = new Node()
        {
            node = node,
        };

        outNode.inputs = node.inputs
            .Select(x => new NodeSocket()
            {
                connector = x,
                node = outNode,
            })
            .ToList();

        outNode.outputs = node.outputs
            .Select(x => new NodeSocket()
            {
                connector = x,
                node = outNode,
            })
            .ToList();

        node.node = outNode;

        nodes.Add(nodeCounter, node);

        userNodes.Add(outNode);

        return outNode;
    }

    public void DestroyNode(Node node)
    {
        if(node == null ||
            node.node == null)
        {
            return;
        }

        nodes.Remove(node.node.ID);

        foreach(var input in node.inputs)
        {
            for(var i = links.Count - 1; i >= 0; i--)
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

    public virtual bool ConnectionIsValid(NodeSocket from, NodeSocket to)
    {
        return true;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

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
                            node.Value.content?.Invoke();
                        }
                        catch(Exception e)
                        {
                            Log.Debug($"[{GetType().Name}] {e}");
                        }
                    });
            }

            for(var i = 0; i < links.Count; i++)
            {
                Link(i + 1, links[i].Item1, links[i].Item2);
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
                var startConnector = start.outputs.FirstOrDefault(x => x.ID == startAttribute);
                var endConnector = end.inputs.FirstOrDefault(x => x.ID == endAttribute);
                var startConnectorUser = start.node.Outputs.FirstOrDefault(x => x.connector.ID == startAttribute);
                var endConnectorUser = end.node.Inputs.FirstOrDefault(x => x.connector.ID == endAttribute);

                if (startConnector != null && endConnector != null &&
                    startConnectorUser != null &&
                    endConnectorUser != null &&
                    ConnectionIsValid(startConnectorUser, endConnectorUser))
                {
                    ConnectNodes(startConnector, startConnectorUser, endConnector, endConnectorUser);
                }
            }
        }
    }

    public Vector2 GetNodePosition(int ID)
    {
        return ImNodes.GetNodeEditorSpacePos(ID);
    }

    public void SetNodePosition(int ID, Vector2 pos)
    {
        ImNodes.SetNodeEditorSpacePos(ID, pos);
    }

    private void ConnectNodes(NodeConnector start, NodeSocket startUser, NodeConnector end, NodeSocket endUser)
    {
        start.connections.Add(end.ID);
        end.connections.Add(start.ID);

        startUser.targets.Add(endUser.Node);
        endUser.targets.Add(startUser.Node);

        links.Add((start.ID, end.ID));
    }

    private ImNodesPinShape GetPinShape(PinShape pinShape)
    {
        return pinShape switch
        {
            PinShape.Circle => ImNodesPinShape.Circle,
            PinShape.CircleFilled => ImNodesPinShape.CircleFilled,
            PinShape.Triangle => ImNodesPinShape.Triangle,
            PinShape.TriangleFilled => ImNodesPinShape.TriangleFilled,
            PinShape.Quad => ImNodesPinShape.Quad,
            PinShape.QuadFilled => ImNodesPinShape.QuadFilled,
            _ => ImNodesPinShape.Circle,
        };
    }

    private void RenderNodes(Action content)
    {
        ImNodes.BeginNodeEditor();

        try
        {
            content?.Invoke();
        }
        catch(Exception e)
        {
            Log.Error($"[{GetType().Name}] {e.Message}");
        }

        ImNodes.EndNodeEditor();
    }

    private void MakeNode(int ID, Action title, Action body)
    {
        ImNodes.BeginNode(ID);

        ImNodes.BeginNodeTitleBar();

        try
        {
            title?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"[{GetType().Name}] {e.Message}");
        }

        ImNodes.EndNodeTitleBar();

        try
        {
            body?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"[{GetType().Name}] {e.Message}");
        }

        ImNodes.EndNode();
    }

    private void NodeInput(int ID, PinShape shape, Action content)
    {
        ImNodes.BeginInputAttribute(ID, GetPinShape(shape));

        try
        {
            content?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"[{GetType().Name}] {e.Message}");
        }

        ImNodes.EndInputAttribute();
    }

    private void NodeOutput(int ID, PinShape shape, Action content)
    {
        ImNodes.BeginOutputAttribute(ID, GetPinShape(shape));

        try
        {
            content?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"[{GetType().Name}] {e.Message}");
        }

        ImNodes.EndOutputAttribute();
    }

    private static void Link(int ID, int from, int to)
    {
        ImNodes.Link(ID, from, to);
    }
}
