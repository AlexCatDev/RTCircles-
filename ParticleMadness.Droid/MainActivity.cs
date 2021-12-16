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

namespace ParticleMadness.Droid
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
            ViewOptions options = ViewOptions.Default;
            options.Samples = 0;
            options.VSync = false;
            options.API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 0));
            options.PreferredBitDepth = new Silk.NET.Maths.Vector4D<int>(8, 8, 8, 0);
            options.ShouldSwapAutomatically = false;
            options.IsContextControlDisabled = true;

            IView view = Silk.NET.Windowing.Window.GetView(options);
            var game = new ParticleMadness.Game();
            game.View = view;
            view.Load += () =>
            {
                var input = view.CreateInput();
                var sdl = Silk.NET.Windowing.Sdl.SdlWindowing.GetExistingApi(view);

                //Add event filter to watch for touch inputs, since silk hasn't official support for them yet
                unsafe
                {
                    sdl.AddEventWatch(
                        new Silk.NET.SDL.PfnEventFilter(
                            new Silk.NET.SDL.EventFilter((@event, sex) => {
                                Silk.NET.SDL.Event ev = *sex;

                                switch ((Silk.NET.SDL.EventType)ev.Type)
                                {
                                    case Silk.NET.SDL.EventType.Fingermotion:
                                        var motion = ev.Tfinger;
                                        Easy2D.Game.Input.FingerMove(motion);
                                        return 0;

                                    case Silk.NET.SDL.EventType.Fingerdown:
                                        var down = ev.Tfinger;
                                        Easy2D.Game.Input.FingerDown(down);
                                        return 0;

                                    case Silk.NET.SDL.EventType.Fingerup:
                                        var up = ev.Tfinger;
                                        Easy2D.Game.Input.FingerUp(up);
                                        return 0;
                                    case Silk.NET.SDL.EventType.Keydown:
                                        if (ev.Key.Keysym.Sym == 1073742094)
                                            Easy2D.Game.Input.BackPressed();

                                        break;
                                }
                                return 1;
                            })), null);
                }

                GL.SetGL(Silk.NET.OpenGLES.GL.GetApi(view));

                GL.Instance.Enable(EnableCap.Texture2D);
                GL.Instance.Enable(EnableCap.ScissorTest);
                GL.Instance.Enable(EnableCap.Blend);

                GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                Sound.Init(view.Handle);
                //this did not work after all, idk what to do, cpu is at max frequency when you start the application
                //then the system scheduler just decides, okay this program is not worth using resources on after 15 seconds
                //down clocks the cpu core???
                //bad fps, basically halves fps in worst case scenario, my device: 340 fps to 180 yikes,
                //and on heavy scenes i go from 130 fps to 70 :(
                Process.SetThreadPriority(ThreadPriority.Foreground);

                game.Load(input);

                game.OnResize(view.Size.X, view.Size.Y);
            };

            view.Resize += (s) =>
            {
                game.OnResize(s.X, s.Y);
            };

            view.Render += (s) =>
            {
                game.Render(s);
            };

            view.Update += (s) =>
            {
                game.Update(s);
            };

            view.Run();
        }
    }
}