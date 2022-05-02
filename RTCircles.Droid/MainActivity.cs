using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Silk.NET.Input;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;
using System.Net.Http;


//Still using the old xamarin because .net 6 is missing some crucial functionality.
//I would like to use .net 6 though, since it performs alot better
namespace RTCircles.Android
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
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback += (request, certificate, chain, error) => true;

            MainGame game = new MainGame();

            game.GetHttpClientFunc = () => new HttpClient(handler);

            game.Run();
        }
    }
}