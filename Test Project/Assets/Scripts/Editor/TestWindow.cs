using Staple;
using Staple.Editor;

namespace TestGame
{
	public class TestWindow : EditorWindow
	{
		[MenuItem("Game/Window")]
		public static void Create()
		{
			var window = GetWindow<TestWindow>();
			
			window.title = "Test Window from game!";

			window.size = new Vector2Int(300, 300);
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
}