using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Windowing;

namespace Easy2D.Game
{
    public abstract class GameBase
    {
        public ClearBufferMask ClearBufferMask = ClearBufferMask.ColorBufferBit;

        public readonly IView View;

        public string Version;

        public string WindowTitle
        {
            get
            {
                if (View is IWindow window)
                    return window.Title;
                else
                    return string.Empty;
            }
            set
            {
                if (View is IWindow window)
                    window.Title = value;
            }
        }

        private bool vsync = true;
        public bool VSync
        {
            get
            {
                return vsync;
            }
            set
            {
                GPUSched.Instance.Enqueue(() =>
                {
                    vsync = value;

                    int interval = value ? -1 : 0;

                    View.GLContext.SwapInterval(interval);
                });
            }
        }

        public bool IsClosing => View.IsClosing;

        public Vector2D<int> Size
        {
            get
            {
                return View.Size;
            }
            set
            {
                if (View is IWindow window)
                    window.Size = value;
            }
        }

        public double MaxAllowedDeltaTime = double.MaxValue;

        public double TotalTime { get; private set; }
        public double DeltaTime { get; private set; }

        public double TimeScale = 1;

        public int UPS { get; private set; }

        public int FPS { get; private set; }

        public GameBase()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"Fatal CRASH ERROR UNHANDLED EXCEPTION: {e.ExceptionObject.ToString()}");

                if (View is IWindow wnd)
                {
                    wnd.WindowState = WindowState.Fullscreen;
                    ToggleFullScreen();
                    wnd.DoEvents();
                }
            };

            Silk.NET.Windowing.Sdl.SdlWindowing.Use();

            ViewOptions options = ViewOptions.Default;
            options.Samples = 0;
            //TODO: 
            //Event driven is actually slower than just inserting a threadsleep LMAO
            //options.IsEventDriven = true;
            options.API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 0));
            options.PreferredBitDepth = new Silk.NET.Maths.Vector4D<int>(8, 8, 8, 8);
            
            var view = Silk.NET.Windowing.Window.GetView(options);

            View = view;

            View.IsContextControlDisabled = true;
            View.ShouldSwapAutomatically = false;

            View.Load += View_Load;
            View.Update += View_Update;
            View.Resize += View_Resize;

            if (View is IWindow wnd)
            {
                wnd.FileDrop += (string[] files) =>
                {
                    foreach (var file in files)
                    {
                        OnOpenFile(file);
                    }
                };

                wnd.FocusChanged += Wnd_FocusChanged;
            }
        }

        private void Wnd_FocusChanged(bool hasFocus)
        {
            GPUSched.Instance.Enqueue(() =>
            {
                //If we lost focus, always enable vsync no matter what
                if (!hasFocus)
                    View.GLContext.SwapInterval(-1);
                else
                {
                    //If aquire focus, only disable vsync if we have explicitely disabled it ourselves
                    if (!VSync)
                        View.GLContext.SwapInterval(0);
                }
            });

            if (!hasFocus)
                Input.InputContext.Mice[0].Cursor.CursorMode = CursorMode.Normal;
            else
                Input.InputContext.Mice[0].Cursor.CursorMode = Input.CursorMode;

            if (!hasFocus)
            {
                if (View is IWindow wnd && wnd.WindowState == WindowState.Fullscreen)
                {
                    wnd.WindowState = WindowState.Minimized;
                }
            }
        }

        private void View_Resize(Vector2D<int> obj)
        {
            OnResize(obj.X, obj.Y);
        }

        private int viewUpdate = 0;
        private double viewElapsed = 0;
        private void View_Update(double delta)
        {
            if (delta > MaxAllowedDeltaTime)
            {
                Utils.Log($"Frametime has been capped {delta * 1000:F2}ms > {MaxAllowedDeltaTime * 1000:F2} !", LogLevel.Performance);
                delta = MaxAllowedDeltaTime;
            }

            viewElapsed += delta;
            viewUpdate++;

            if (viewElapsed >= 1)
            {
                UPS = viewUpdate;

                FPS = viewUpdate;

                viewElapsed -= 1;
                viewUpdate = 0;
            }

            DeltaTime = delta;
            TotalTime += delta;

            OnUpdate();

            PostProcessing.Update((float)DeltaTime);
            OnRender();
            GPUSched.Instance.RunPendingTasks();
            View.SwapBuffers();
            GL.Instance.Clear(ClearBufferMask);
        }

        private double elapsedFPS;
        private int fps;

        private Sdl sdl;

        private void View_Load()
        {
            try
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            }
            catch
            {
                Utils.Log($"Unable to set process priority to high!", LogLevel.Warning);
            }

            try
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
            catch (Exception ex)
            {
                Utils.Log($"Failed to set GCMode to lowlatency: {ex.Message}", LogLevel.Warning);
            }

            setupSDL();

            Input.SetContext(View.CreateInput());
            GL.SetGL(View.CreateOpenGLES());
            Sound.Init(View.Handle);

            GL.Instance.Enable(EnableCap.Texture2D);
            GL.Instance.Enable(EnableCap.ScissorTest);
            GL.Instance.Enable(EnableCap.Blend);

            GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            OnLoad();
            OnResize(View.Size.X, View.Size.Y);
        }

        private unsafe void setupSDL()
        {
            sdl = Silk.NET.Windowing.Sdl.SdlWindowing.GetExistingApi(View);

            if (sdl is null)
                return;

            unsafe
            {
                if (View is IWindow window)
                {
                    var fullRes = window.Monitor.VideoMode.Resolution.Value;
                    var refreshRate = window.Monitor.VideoMode.RefreshRate.Value;
                    Silk.NET.SDL.DisplayMode k = new Silk.NET.SDL.DisplayMode(null, fullRes.X, fullRes.Y, refreshRate, null);
                    sdl.SetWindowDisplayMode(ref Unsafe.AsRef<Silk.NET.SDL.Window>((void*)window.Native.Sdl.Value), ref k);
                }

                sdl.SetEventFilter(new PfnEventFilter(new EventFilter(eventFilter)), null);
            }
        }

        //Hacks :tf:
        private unsafe int eventFilter(void* args, Silk.NET.SDL.Event* @event)
        {
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

                    else if (ev.Key.Keysym.Sym == 1073741892)
                    {
                        ToggleFullScreen();
                        return 0;
                    }

                    break;
            }
            return 1;
        }

        public void ToggleFullScreen()
        {
            if (View is IWindow window)
            {
                if (window.WindowState != WindowState.Fullscreen)
                    window.WindowState = WindowState.Fullscreen;
                else
                {
                    unsafe
                    {
                        sdl?.SetWindowFullscreen((Silk.NET.SDL.Window*)window.Native.Sdl.Value, 0);
                    }
                    window.WindowState = WindowState.Normal;
                }

                OnResize(View.Size.X, View.Size.Y);
            }
        }

        public abstract void OnLoad();

        public abstract void OnUpdate();
        public abstract void OnRender();

        public abstract void OnResize(int width, int height);

        public abstract void OnOpenFile(string fullpath);

        public void Run() => View.Run();
    }
}
