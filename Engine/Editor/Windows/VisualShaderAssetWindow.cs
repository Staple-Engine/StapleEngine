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

        windowFlags = EditorWindowFlags.Resizable |
            EditorWindowFlags.Dockable |
            EditorWindowFlags.MenuBar;

        nodeUI = new(this)
        {
            showMinimap = true,
        };
    }

    public override void OnGUI()
    {
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
                    p.name = EditorGUI.TextField("", $"VisualShaderAssetWindow.ParameterName{p.GetHashCode()}", p.name);

                    EditorGUI.SameLine();

                    p.uniformType = EditorGUI.EnumDropdown("", $"VisualShaderAssetWindow.ParameterType{p.GetHashCode()}", p.uniformType);
                }
            }
        });

        EditorGUI.SameLine();

        nodeUI.DoLayout();

        nodeUI.EndFrame();
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
