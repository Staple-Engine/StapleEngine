namespace Staple.Editor;

public class NodeEditor : Editor
{
    public NodeUI nodeUI = new();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        nodeUI?.Draw();
    }
}
