using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Easy2D;
using Silk.NET.Input;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;

namespace New.Droid
{

    [Activity(MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, Theme = "@android:style/Theme.NoTitleBar")]
    public class MainActivity : SilkActivity
    {
        public SystemUiFlags UIVisibilityFlags
        {
            get => (SystemUiFlags)Window.DecorView.SystemUiVisibility;
            set
            {
                systemUiFlags = value;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)value;
            }
        }

        private SystemUiFlags systemUiFlags;

        protected override void OnCreate(Bundle savedInstanceState)
        {

            //why doesn't this work if the app is started in portrait mode
            //but if it's started in landscape mode, it works for both landscape and portrait
            UIVisibilityFlags = SystemUiFlags.LayoutFlags | SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen;
            base.OnCreate(savedInstanceState);
        }

        protected override void OnRun()
        {
            new NewGame().Run();
        }
    }
}