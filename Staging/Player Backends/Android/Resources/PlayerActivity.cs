using Android.App;
using Android.Content.PM;
using Android.OS;
using Staple;

[Activity(Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance)]
public class PlayerActivity : StapleActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        TypeCacheRegistration.RegisterAll();

        base.OnCreate(savedInstanceState);
    }
}
