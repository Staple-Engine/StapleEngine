using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal class SkinnedAnimationStateMachineWindow : EditorWindow, INodeUIObserver
{
    private static string NodePopup = "SkinnedAnimationStateMachineWindow.NodePopup";
    private static string LinkPopup = "SkinnedAnimationStateMachineWindow.LinkPopup";
    private static string WorkspacePopup = "SkinnedAnimationStateMachineWindow.WorkspacePopup";

    public SkinnedAnimationStateMachineEditor owner;
    public NodeUI nodeUI;

    private readonly List<NodeUI.Node> nodes = [];

    private SkinnedAnimationStateMachine.AnimationStateConnection selectedConnection;
    private (NodeUI.NodeSocket, NodeUI.NodeSocket) selectedNodes;
    private NodeUI.Node selectedNode;

    public SkinnedAnimationStateMachineWindow()
    {
        title = "Animation State Editor";
        size = new Vector2Int(300, 400);

        windowFlags = EditorWindowFlags.Resizable | EditorWindowFlags.Dockable | EditorWindowFlags.HasMenuBar;

        nodeUI = new(this)
        {
            showMinimap = true,
        };
    }

    private NodeUI.Node GetNode(string stateName)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Title == stateName)
            {
                return nodes[i];
            }
        }

        return null;
    }

    public override void OnGUI()
    {
        if (owner == null || owner.target is not SkinnedAnimationStateMachine asset)
        {
            return;
        }

        EditorGUI.MenuBar(() =>
        {
            EditorGUI.Menu("File", $"{GetType().Name}.File", () =>
            {
                EditorGUI.MenuItem("Save", $"{GetType().Name}.Save", () =>
                {
                    asset.editorData.nodePositions = new SkinnedAnimationStateMachine.NodePosition[nodes.Count];

                    for(var i = 0; i < nodes.Count; i++)
                    {
                        var pos = asset.editorData.nodePositions[i] = new();

                        var nodePos = nodeUI.GetNodePosition(nodes[i]);

                        pos.x = nodePos.X;
                        pos.y = nodePos.Y;
                    }

                    EditorUtils.SaveAsset(asset);
                });
            });
        });

        if(asset.states.Count > 0 && nodeUI.NodeCount == 0)
        {
            for(var i = 0; i < asset.states.Count; i++)
            {
                for(int j = 0, counter = 0; j < i; j++)
                {
                    if(asset.states[j].name == asset.states[i].name)
                    {
                        asset.states[i].name = $"{asset.states[i].name}{++counter}";
                    }
                }
            }

            for(var i = 0; i < asset.states.Count; i++)
            {
                var state = asset.states[i];
                var index = i;

                var node = nodeUI.CreateNode(state.name, [new("In", NodeUI.PinShape.Circle)], [new("Out", NodeUI.PinShape.Circle)], (node) =>
                {
                    state.name = EditorGUI.TextField("Name", $"SkinnedAnimationStateMachineWindow.State{index}.Name", state.name, new Vector2(100, 0));

                    state.repeat = EditorGUI.Toggle("Repeat", $"SkinnedAnimationStateMachineWindow.State{index}.Repeat", state.repeat);

                    var animations = asset.mesh?.meshAsset?.animations;

                    if (animations != null)
                    {
                        var animationNames = animations.Keys.ToArray();

                        var currentIndex = Array.IndexOf(animationNames, state.animation);

                        EditorGUI.ItemWidth(100, () =>
                        {
                            var newIndex = EditorGUI.Dropdown("Animation", "SkinnedAnimationStateMachineWindow.Selected.Animation", animationNames, currentIndex);

                            if (newIndex != currentIndex && newIndex >= 0 && newIndex < animationNames.Length)
                            {
                                state.animation = animationNames[newIndex];
                            }
                        });
                    }
                });

                nodes.Add(node);

                if((asset.editorData.nodePositions?.Length ?? 0) > i)
                {
                    nodeUI.SetNodePosition(node, new(asset.editorData.nodePositions[i].x, asset.editorData.nodePositions[i].y));
                }
            }

            for(var i = 0; i < asset.states.Count; i++)
            {
                var state = asset.states[i];
                var localNode = nodes[i];

                foreach (var connection in state.connections)
                {
                    var targetNode = GetNode(connection.name);

                    if(targetNode == null || targetNode == nodes[i])
                    {
                        continue;
                    }

                    var fromConnection = localNode.GetOutput("Out");
                    var toConnection = targetNode.GetInput("In");

                    nodeUI.CreateLink(fromConnection, toConnection);
                }
            }
        }

        EditorGUI.WindowFrame("SkinnedAnimationStateMachineWindow.Parameters", new Vector2(200, 0), () =>
        {
            EditorGUI.Label("Parameters");

            EditorGUI.SameLine();

            EditorGUI.Button("+", "SkinnedAnimationStateMachineWindow.Parameters.Add", () =>
            {
                asset.parameters.Add(new());
            });

            for(var i = 0; i < asset.parameters.Count; i++)
            {
                var p = asset.parameters[i];

                EditorGUI.Columns(2,
                    (column) =>
                    {
                        return 100;
                    },
                    (column) =>
                    {
                        switch(column)
                        {
                            case 0:

                                p.name = EditorGUI.TextField("", $"SkinnedAnimationStateMachineWindow.ParameterName{i}", p.name, new Vector2(100, 0));

                                break;

                            case 1:

                                p.parameterType = EditorGUI.EnumDropdown("", $"SkinnedAnimationStateMachineWindow.ParameterType{i}", p.parameterType);

                                break;
                        }
                    });
            }
        });

        EditorGUI.SameLine();

        nodeUI.usedSpace = (selectedConnection != null) ? EditorGUI.RemainingHorizontalSpace() - 300 : 0;

        nodeUI.DoLayout();

        if(selectedConnection != null)
        {
            EditorGUI.SameLine();

            EditorGUI.WindowFrame("SkinnedAnimationStateMachineWindow.Selected", new Vector2(300, 0), () =>
            {
                if (selectedConnection != null)
                {
                    EditorGUI.Label($"{selectedNodes.Item1.Node.Title} to {selectedNodes.Item2.Node.Title}");

                    EditorGUI.Label("Parameters");

                    EditorGUI.SameLine();

                    EditorGUI.Button("+", "SkinnedAnimationStateMachineWindow.Selected.Parameters.Add", () =>
                    {
                        selectedConnection.parameters.Add(new());
                    });

                    for (var i = 0; i < selectedConnection.parameters.Count; i++)
                    {
                        var p = selectedConnection.parameters[i];

                        var names = asset.parameters.Select(x => x.name).ToArray();

                        var currentIndex = Array.IndexOf(names, p.name);

                        var newIndex = EditorGUI.Dropdown("", $"SkinnedAnimationStateMachineWindow.Selected.Parameters{i}.Name",
                            names, currentIndex);

                        if (newIndex != currentIndex && newIndex >= 0)
                        {
                            p.name = names[newIndex];
                        }

                        var parameter = asset.parameters.FirstOrDefault(x => x.name == p.name);

                        EditorGUI.SameLine();

                        switch (parameter.parameterType)
                        {
                            case SkinnedAnimationStateMachine.AnimationParameterType.Bool:

                                EditorGUI.ItemWidth(60, () =>
                                {
                                    p.boolValue = EditorGUI.Toggle("", $"SkinnedAnimationStateMachineWindow.Selected.Parameters{i}.Value", p.boolValue);
                                });

                                break;

                            case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                                EditorGUI.ItemWidth(60, () =>
                                {
                                    p.intValue = EditorGUI.IntField("", $"SkinnedAnimationStateMachineWindow.Selected.Parameters{i}.Value", p.intValue);
                                });

                                break;

                            case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                                EditorGUI.ItemWidth(60, () =>
                                {
                                    p.floatValue = EditorGUI.FloatField("", $"SkinnedAnimationStateMachineWindow.Selected.Parameters{i}.Value", p.floatValue);
                                });

                                break;
                        }
                    }

                    selectedConnection.onFinish = EditorGUI.Toggle("On Finish", $"SkinnedAnimationStateMachineWindow.Selected.OnFinish",
                        selectedConnection.onFinish);

                    selectedConnection.any = EditorGUI.Toggle("On Any", $"SkinnedAnimationStateMachineWindow.Selected.Any",
                        selectedConnection.any);
                }
            });
        }

        EditorGUI.Popup(NodePopup, () =>
        {
            EditorGUI.MenuItem("Delete", $"SkinnedAnimationStateMachineWindow.Selected.Delete", () =>
            {
                if(selectedNode != null)
                {
                    nodeUI.DeleteNode(selectedNode);

                    foreach (var state in asset.states)
                    {
                        if (state.name == selectedNode.Title)
                        {
                            foreach(var s in asset.states)
                            {
                                for(var i = s.connections.Count - 1; i >= 0; i--)
                                {
                                    if(s.connections[i].name == selectedNode.Title)
                                    {
                                        s.connections.RemoveAt(i);
                                    }
                                }
                            }

                            asset.states.Remove(state);

                            selectedNode = null;

                            return;
                        }
                    }
                }
            });
        });

        EditorGUI.Popup(LinkPopup, () =>
        {
            EditorGUI.MenuItem("Delete", $"SkinnedAnimationStateMachineWindow.Selected.Delete", () =>
            {
                var fromNode = selectedNodes.Item1.Node.Title;
                var toNode = selectedNodes.Item2.node.Title;

                nodeUI.DeleteLink(selectedNodes.Item1, selectedNodes.Item2);

                selectedConnection = null;

                foreach (var state in asset.states)
                {
                    if(state.name == fromNode)
                    {
                        foreach (var connection in state.connections)
                        {
                            if (connection.name == toNode)
                            {
                                state.connections.Remove(connection);

                                return;
                            }
                        }
                    }
                }
            });
        });

        EditorGUI.Popup(WorkspacePopup, () =>
        {

        });
    }

    public (bool, Action) OnLinkClick(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to, MouseButton button)
    {
        if (owner == null || owner.target is not SkinnedAnimationStateMachine asset)
        {
            return (false, null);
        }

        selectedConnection = null;
        selectedNodes = (from, to);

        foreach (var state in asset.states)
        {
            if (state.name == from.Node.Title)
            {
                foreach (var connection in state.connections)
                {
                    if (connection.name == to.Node.Title)
                    {
                        selectedConnection = connection;

                        break;
                    }
                }
            }

            if(selectedConnection != null)
            {
                break;
            }
        }

        switch (button)
        {
            case MouseButton.Right:

                EditorGUI.OpenPopup(LinkPopup);

                break;
        }

        return (false, null);
    }

    public (bool, Action) OnNodeClick(NodeUI nodeUI, NodeUI.Node node, MouseButton button)
    {
        if (owner == null || owner.target is not SkinnedAnimationStateMachine asset)
        {
            return (false, null);
        }

        selectedNode = node;

        switch(button)
        {
            case MouseButton.Right:

                EditorGUI.OpenPopup(NodePopup);

                break;
        }

        return (false, null);
    }

    public (bool, Action) OnWorkspaceClick(NodeUI nodeUI, MouseButton button)
    {
        switch(button)
        {
            case MouseButton.Left:

                selectedConnection = null;

                break;

            case MouseButton.Right:

                EditorGUI.OpenPopup(WorkspacePopup);

                break;
        }

        return (false, null);
    }

    public bool ValidateConnection(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to)
    {
        if(from.Name != "Out" || to.Name != "In")
        {
            return false;
        }

        //TODO: Add logic

        return false;
    }
}
