using Easy2D;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using System.Runtime.CompilerServices;

namespace RTCircles.Desktop
{
    class Program
    {
        private static IWindow window;
        private static Silk.NET.SDL.Sdl sdl;
        private static MainGame game = new MainGame();

        //i dont know what im doing
        static void Main(string[] args)
        {
            WindowOptions b = WindowOptions.Default;
            b.VSync = false;
            b.Title = "RTCircles";
            b.Size = new Vector2D<int>(1280, 720);
            b.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3));
            b.WindowBorder = WindowBorder.Resizable;
            b.TransparentFramebuffer = false;
            b.ShouldSwapAutomatically = false;
            b.IsContextControlDisabled = true;
            Silk.NET.Windowing.Sdl.SdlWindowing.Use();

            window = Window.Create(b);
            window.WindowState = WindowState.Maximized;
            game.View = window;

            window.Load += () =>
            {
                var input = window.CreateInput();
                sdl = Silk.NET.Windowing.Sdl.SdlWindowing.GetExistingApi(window);

                //Set the full screen mode to the size of the current monitor, todo fix for multiple monitors, but i dont have anything to test with
                unsafe
                {
                    
                    var fullRes = window.Monitor.VideoMode.Resolution.Value;
                    var refreshRate = window.Monitor.VideoMode.RefreshRate.Value;
                    Silk.NET.SDL.DisplayMode k = new Silk.NET.SDL.DisplayMode(null, fullRes.X, fullRes.Y, refreshRate, null);
                    sdl.SetWindowDisplayMode(ref Unsafe.AsRef<Silk.NET.SDL.Window>((void*)window.Native.Sdl.Value), ref k);
                    

                    sdl?.SetEventFilter(new Silk.NET.SDL.PfnEventFilter(new Silk.NET.SDL.EventFilter(eventFilter)), null);
                }
                /*
                sdl?.AddEventWatch(
                    new Silk.NET.SDL.PfnEventFilter(new Silk.NET.SDL.EventFilter(eventFilter)), null);
                */

                /*
                input.Keyboards[0].KeyDown += (s, e, x) =>
                {
                    if (e == Key.ShiftRight)
                        shiftDown = true;

                    if (e == Key.Backspace && shiftDown)
                    {
                        //var value = window.WindowState != WindowState.Normal ? 0 : 1;

                        //THIS DOESNT WORK WHY, WINDOW STATE NORMAL BROKEN!!!!!!! cant toggle :(
                        window.WindowState = window.WindowState != WindowState.Normal ? WindowState.Normal : WindowState.Fullscreen;

                        unsafe 
                        {
                            //sdl.SetWindowFullscreen(ref Unsafe.AsRef<Silk.NET.SDL.Window>((void*)window.Native.Sdl.Value), (uint)value);
                        }
                    }
                };

                input.Keyboards[0].KeyUp += (s, e, x) =>
                {
                    if (e == Key.ShiftRight)
                        shiftDown = false;
                };
                */

                GL.SetGL(Silk.NET.OpenGLES.GL.GetApi(window));
                
                GL.Instance.Enable(EnableCap.Texture2D);
                GL.Instance.Enable(EnableCap.ScissorTest);
                GL.Instance.Enable(EnableCap.Blend);

                GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                Sound.Init(window.Handle);

                game.Load(input);
                game.OnResize(window.Size.X, window.Size.Y);
            };

            window.FileDrop += (files) =>
            {
                foreach (var file in files)
                {
                    game.OnImportFile(file);
                }
            };

            window.Resize += (s) =>
            {
                game.OnResize(s.X, s.Y);
            };

            window.Render += (s) =>
            {
                game.Render(s);
            };

            window.Update += (s) =>
            {
                game.Update(s);
            };

            window.Run();
        }

        static unsafe int eventFilter(void* args, Silk.NET.SDL.Event* @event) {
            Silk.NET.SDL.Event ev = *@event;

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

                    if (ev.Key.Keysym.Mod == 4352 && ev.Key.Keysym.Sym == 13)
                    {
                        toggleFullScreen();
                        return 0;
                    }else if(ev.Key.Keysym.Sym == 1073741892)
                    {
                        toggleFullScreen();
                        return 0;
                    }

                    break;
            }
            return 1;
        }

        static unsafe void toggleFullScreen()
        {
            if(window.WindowState != WindowState.Fullscreen)
                window.WindowState = WindowState.Fullscreen;
            else
            {
                sdl?.SetWindowFullscreen((Silk.NET.SDL.Window*)window.Native.Sdl.Value, 0);
                window.WindowState = WindowState.Normal;
            }
            game.OnResize(game.View.Size.X, game.View.Size.Y);
        }
    }
}
