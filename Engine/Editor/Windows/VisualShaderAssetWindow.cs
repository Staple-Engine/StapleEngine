using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.Editor;

internal class VisualShaderAssetWindow : EditorWindow, INodeUIObserver
{
    public VisualShaderAssetEditor owner;
    public NodeUI nodeUI;

    public VisualShaderAssetWindow()
    {
        title = "Visual Shader Editor";
        size = new Vector2Int(300, 400);
        allowDocking = true;
        allowResize = true;

        nodeUI = new(this)
        {
            showMinimap = true,
        };
    }

    public override void OnGUI()
    {
        base.OnGUI();

        if(owner == null || owner.target is not VisualShaderAsset asset)
        {
            return;
        }

        EditorGUI.WindowFrame("VisualShaderAssetWindow.Parameters", new Vector2(200, 0), () =>
        {
            EditorGUI.Label("Parameters");

            EditorGUI.SameLine();

            EditorGUI.Button("+", "VisualShaderAssetWindow.Parameters.Add", () =>
            {
                asset.parameters.Add(new()
                {
                    name = "",
                    uniformType = ShaderUniformType.Vector3,
                });
            });

            foreach(var p in asset.parameters)
            {
                if(p.varying == false)
                {
                    p.name = EditorGUI.TextField("", $"VisualShaderAssetWindow.ParameterName{p.GetHashCode()}", p.name, simple: true);

                    EditorGUI.SameLine();

                    p.uniformType = EditorGUI.EnumDropdown("", $"VisualShaderAssetWindow.ParameterType{p.GetHashCode()}", p.uniformType, simple: true);
                }
            }
        });

        EditorGUI.SameLine();

        nodeUI.DoLayout();
    }

    public (bool, Action) OnLinkClick(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to, MouseButton button)
    {
        return (false, null);
    }

    public (bool, Action) OnNodeClick(NodeUI nodeUI, NodeUI.Node node, MouseButton button)
    {
        return (false, null);
    }

    public (bool, Action) OnWorkspaceClick(NodeUI nodeUI, MouseButton button)
    {
        return (false, null);
    }

    public bool ValidateConnection(NodeUI nodeUI, NodeUI.NodeSocket from, NodeUI.NodeSocket to)
    {
        return false;
    }
}
