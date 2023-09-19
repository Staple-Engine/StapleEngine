using Android.App;
using Android.OS;

namespace Player
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class StapleActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
        }
    }
}