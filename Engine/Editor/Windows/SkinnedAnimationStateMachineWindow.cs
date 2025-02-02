using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal class SkinnedAnimationStateMachineWindow : EditorWindow, INodeUIObserver
{
    public SkinnedAnimationStateMachineEditor owner;
    public NodeUI nodeUI;

    private readonly List<NodeUI.Node> nodes = [];

    private SkinnedAnimationStateMachine.AnimationStateConnection selectedConnection;

    public SkinnedAnimationStateMachineWindow()
    {
        title = "Animation State Editor";
        size = new Vector2Int(300, 400);
        allowDocking = true;
        allowResize = true;

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
        base.OnGUI();

        if (owner == null || owner.target is not SkinnedAnimationStateMachine asset)
        {
            return;
        }

        if(asset.states.Count > 0 && nodeUI.NodeCount == 0)
        {
            for(var i = 0; i < asset.states.Count; i++)
            {
                var state = asset.states[i];

                nodes.Add(nodeUI.CreateNode(state.name, [new("In", NodeUI.PinShape.Circle)], [new("Out", NodeUI.PinShape.Circle)], (node) =>
                {
                    //state.name = EditorGUI.TextField("Name", $"SkinnedAnimationStateMachineWindow.State{i}.Name", state.name);

                    //state.repeat = EditorGUI.Toggle("Repeat", $"SkinnedAnimationStateMachineWindow.State{i}.Repeat", state.repeat);
                }));
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

        nodeUI.usedSpace = selectedConnection != null ? EditorGUI.RemainingHorizontalSpace() - 300 : 0;

        nodeUI.DoLayout();

        if(selectedConnection != null)
        {
            EditorGUI.SameLine();

            EditorGUI.WindowFrame("SkinnedAnimationStateMachineWindow.Selected", new Vector2(300, 0), () =>
            {
                if (selectedConnection != null)
                {
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
    }

    public (bool, Action) OnLinkClick(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to, MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left:

                if (owner == null || owner.target is not SkinnedAnimationStateMachine asset)
                {
                    return (false, null);
                }

                foreach (var state in asset.states)
                {
                    if(state.name == from.Node.Title)
                    {
                        foreach(var connection in state.connections)
                        {
                            if(connection.name == to.Node.Title)
                            {
                                selectedConnection = connection;

                                return (false, null);
                            }
                        }
                    }
                }

                break;
        }

        return (false, null);
    }

    public (bool, Action) OnNodeClick(NodeUI nodeUI, NodeUI.Node node, MouseButton button)
    {
        return (false, null);
    }

    public (bool, Action) OnWorkspaceClick(NodeUI nodeUI, MouseButton button)
    {
        switch(button)
        {
            case MouseButton.Left:

                selectedConnection = null;

                break;
        }

        return (false, null);
    }

    public bool ValidateConnection(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to)
    {
        return false;
    }
}
