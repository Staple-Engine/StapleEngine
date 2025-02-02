using System.Collections.Generic;
using System;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace Staple.Editor;

public partial class NodeUI
{
    internal class InternalNode
    {
        public int ID;
        public string title;
        public List<NodeConnector> inputs = [];
        public List<NodeConnector> outputs = [];
        public Node node;

        public Action<Node> content;
    }

    internal class NodeConnector
    {
        public int ID;
        public int nodeID;
        public List<int> connections = [];
        public SocketType socketType;
        public PinShape shape;
        public string name;
        public NodeSocket socket;
    }

    private readonly Dictionary<int, InternalNode> nodes = [];
    private readonly Dictionary<int, NodeConnector> connectors = [];
    private readonly List<(int, int)> links = [];
    private readonly List<Node> userNodes = [];
    private int nodeCounter = 0;
    private int connectorCounter = 0;
    private Action popupContent;

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
        EditorGUI.WindowFrame($"NodeEditor{observer?.GetType().FullName ?? ""}", new Vector2(usedSpace, 0), () =>
        {
            ImNodes.BeginNodeEditor();

            try
            {
                content?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}] {e.Message}");
            }

            ImNodes.EndNodeEditor();
        });
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
