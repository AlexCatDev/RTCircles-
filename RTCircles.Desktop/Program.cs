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
        //i dont know what im doing
        static void Main(string[] args)
        {
            WindowOptions b = WindowOptions.Default;
            b.VSync = false;
            b.Title = "nabo";
            b.Size = new Vector2D<int>(1280, 720);
            b.API = new GraphicsAPI(ContextAPI.OpenGLES, new APIVersion(3, 0));
            b.WindowBorder = WindowBorder.Resizable;
            b.TransparentFramebuffer = false;
            b.ShouldSwapAutomatically = false;
            b.IsContextControlDisabled = true;
            Silk.NET.Windowing.Sdl.SdlWindowing.Use();

            IWindow window = Window.Create(b);

            var game = new MainGame();
            game.View = window;

            bool shiftDown = false;

            window.Load += () =>
            {
                var input = window.CreateInput();
                var sdl = Silk.NET.Windowing.Sdl.SdlWindowing.GetExistingApi(window);

                //Set the full screen mode to the size of the current monitor, todo fix for multiple monitors, but i dont have anything to test with
                unsafe
                {
                    
                    var fullRes = window.Monitor.VideoMode.Resolution.Value;
                    var refreshRate = window.Monitor.VideoMode.RefreshRate.Value;
                    Silk.NET.SDL.DisplayMode k = new Silk.NET.SDL.DisplayMode(null, fullRes.X, fullRes.Y, refreshRate, null);
                    sdl.SetWindowDisplayMode(ref Unsafe.AsRef<Silk.NET.SDL.Window>((void*)window.Native.Sdl.Value), ref k);
                    
                }

                input.Keyboards[0].KeyDown += (s, e, x) =>
                {
                    if (e == Key.ShiftRight)
                        shiftDown = true;

                    if (e == Key.Backspace && shiftDown)
                    {
                        //var value = window.WindowState != WindowState.Normal ? 0 : 1;

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

                unsafe
                {
                    sdl?.AddEventWatch(
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

                                    case Silk.NET.SDL.EventType.Windowevent:
                                        return 0;
                                    case Silk.NET.SDL.EventType.Keydown:
                                        if (ev.Key.Keysym.Sym == 1073742094)
                                            Easy2D.Game.Input.BackPressed();

                                        break;
                                }
                                return 1;
                            })), null);
                }

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
    }

}
