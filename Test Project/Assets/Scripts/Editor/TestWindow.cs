using Staple;
using Staple.Editor;

namespace TestGame;

public class TestWindow : EditorWindow
{
	[MenuItem("Game/Window")]
	public static void Create()
	{
		GetWindow<TestWindow>();
	}

	public TestWindow()
	{
        title = "Test Window from game!";

        size = new Vector2Int(300, 300);
    }

    public override void OnGUI()
	{
		EditorGUI.Button("X", "Close", () =>
		{
			Close();
		});
		
		EditorGUI.Label("Test Window from game!");
	}
}
