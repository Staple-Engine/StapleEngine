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
		}
		
		public override void OnGUI()
		{
			EditorGUI.Label("Test Window from game!");
		}
	}
}