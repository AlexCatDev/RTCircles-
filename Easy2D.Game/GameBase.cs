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
        static GameBase()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"Fatal CRASH ERROR UNHANDLED EXCEPTION: {e.ExceptionObject.ToString()}");
            };
        }

        public ClearBufferMask ClearBufferMask = ClearBufferMask.ColorBufferBit;

        public readonly IView View;

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

        public Vector2D<int> Size => View.Size;

        public double MaxAllowedDeltaTime = double.MaxValue;

        public double TotalTime { get; private set; }
        public double DeltaTime { get; private set; }

        public double TimeScale = 1;

        public int UPS { get; private set; }

        public int FPS { get; private set; }

        public GameBase()
        {
            Silk.NET.Windowing.Sdl.SdlWindowing.Use();

            ViewOptions options = ViewOptions.Default;
            options.Samples = 0;
            //TODO: 
            //Event driven is actually slower than just inserting a threadsleep LMAO
            //options.IsEventDriven = true;
            options.API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 0));
            options.PreferredBitDepth = new Silk.NET.Maths.Vector4D<int>(8, 8, 8, 0);

            var window = Silk.NET.Windowing.Window.GetView(options);

            View = window;

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

            OnRender();
            GPUSched.Instance.RunPendingTasks();
            PostProcessing.Update((float)DeltaTime);
            View.SwapBuffers();
            GL.Instance.Clear(ClearBufferMask);
            /*
            if (freeToRender)
            {
                if (System.Threading.Monitor.TryEnter(renderLock))
                {
                    OnRender();

                    freeToRender = false;

                    System.Threading.Monitor.Exit(renderLock);
                }
            }
            else
            {
                System.Threading.Thread.Sleep(1);
            }
            */
        }

        private double elapsedFPS;
        private int fps;

        private Sdl sdl;

        private readonly object renderLock = new object();
        private bool freeToRender = false;

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

            /*
            View.ClearContext();
            new System.Threading.Thread(() => {
                Utils.Log($"Started render thread!", LogLevel.Info);

                View.GLContext.MakeCurrent();

                Stopwatch sw = new Stopwatch();

                while (!View.IsClosing)
                {
                    double delta = ((double)sw.ElapsedTicks / Stopwatch.Frequency) * TimeScale;
                    sw.Restart();

                    elapsedFPS += delta;

                    if (!freeToRender)
                    {
                        if (System.Threading.Monitor.TryEnter(renderLock))
                        {
                            fps++;

                            GL.Instance.Clear(ClearBufferMask);

                            GPUSched.Instance.RunPendingTasks();

                            freeToRender = true;

                            System.Threading.Monitor.Exit(renderLock);

                            if (View.IsClosing)
                                break;

                            View.SwapBuffers();
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1);
                    }

                    if (elapsedFPS >= 1)
                    {
                        FPS = fps;
                        elapsedFPS -= 1;
                        fps = 0;
                    }

                    //Add alternative mode, thats not vsync!
                    //if (!VSync)
                    //renderThrottler.Update();
                }
            }).Start();
            */
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

                    if (ev.Key.Keysym.Mod == 4352 && ev.Key.Keysym.Sym == 13)
                    {
                        ToggleFullScreen();
                        return 0;
                    }
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

    /*
    public abstract class Game
    {
        public static Game Instance { get; private set; }

        static Game()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"CRASH_ERROR_UNHANDLED_EXCEPTION: {e.ExceptionObject.ToString()}");
            };
        }

        public static int FPS { get; private set; }

        public bool PrintFPS;
        public bool PrintUPS;

        public IView View;

        public void Load(IInputContext inputContext)
        {
            if (Instance is not null)
                throw new Exception("A instance is already loaded, dont call this method anywhere, it's automatic from the respected hosts");

            Instance = this;

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
                Utils.Log($"Failed to set GCMode to lowlatency: {ex.Message}", LogLevel.Error);
            }

            View.FocusChanged += (IsForeground) =>
            {
                GPUSched.Instance.Enqueue(() =>
                {
                    if (IsForeground)
                        View.GLContext.SwapInterval(0);
                    else
                        View.GLContext.SwapInterval(-1);
                });
            };

            View.GLContext.SwapInterval(0);
            GL.Instance.DepthFunc(Silk.NET.OpenGLES.DepthFunction.Less);
            Input.SetContext(inputContext);

            OnLoad();
        }

        public double MaxAllowedDeltaTime = double.MaxValue;

        private int fpsCounter;
        private double fpsElapsed = 0;

        private bool backgroundThrottle = false;

        /// <summary>
        /// This gets called by the game host
        /// </summary>
        /// <param name="delta"></param>
        public void Render(double delta)
        {
            if (delta > MaxAllowedDeltaTime)
            {
                Utils.Log($"Frametime has been capped {delta * 1000:F2}ms > {MaxAllowedDeltaTime * 1000:F2} !", LogLevel.Performance);
                delta = MaxAllowedDeltaTime;
            }

            DeltaTime = delta;

            GL.Instance.Clear(ClearBufferMask.ColorBufferBit);

            //Unfortunately syncing of the rendering and upating is kinda mandatory
            //So this will block the update thread while its rendering,
            //Causing oppotunities for input lag, since input thread alos manages window input

            //Conversely, this would wait for the update thread to complete, who ever locks it first  i guess

            OnUpdate(delta);
            OnRender(delta);

            if (View.IsClosing == false)
                View.SwapBuffers();

            GPUSched.Instance.RunPendingTasks();
            PostProcessing.Update((float)delta);

            fpsCounter++;

            TotalTime += delta;
            DeltaTime = delta;

            fpsElapsed += DeltaTime;

            if (fpsElapsed >= 1)
            {
                FPS = fpsCounter;
                fpsElapsed -= 1;
                fpsCounter = 0;

                if (PrintFPS)
                    Console.WriteLine($"FPS: {FPS}");
            }
        }

        /// <summary>
        /// This gets called by the game host
        /// </summary>
        /// <param name="delta"></param>
        public void Update(double delta) { }

        public double TotalTime { get; private set; }
        public double DeltaTime { get; private set; }

        public abstract void OnRender(double delta);
        public abstract void OnUpdate(double delta);

        public abstract void OnImportFile(string path);

        public abstract void OnLoad();

        public abstract void OnResize(int width, int height);
    }
    */
    /*
    public abstract class Game
    {
        public static Game Instance { get; private set; }

        static Game()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"CRASH_ERROR_UNHANDLED_EXCEPTION: {e.ExceptionObject.ToString()}");
            };
        }

        public static int FPS { get; private set; }
        public static int UPS { get; private set; }

        public bool PrintFPS;
        public bool PrintUPS;

        private volatile bool isMultiThreaded;
        private volatile int renderThreadID = INACTIVE_THREAD;

        private const int INACTIVE_THREAD = int.MinValue;

        public IView View;

        private readonly object renderLock = new object();

        public bool IsMultiThreaded
        {
            get
            {
                return isMultiThreaded;
            }
            set
            {
                if (value != isMultiThreaded)
                {
                    isMultiThreaded = value;

                    //Set the id so it now knows to no longer accept render invokes from mainthread
                    renderThreadID = Int32.MaxValue;

                    //Wait for the current render to complete before clearing the context
                    lock (renderLock)
                    {
                        //Clear the context.
                        View.GLContext.Clear();
                    }

                    if (isMultiThreaded)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new((o) =>
                        {
                            renderThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
                            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

                            Stopwatch sw = new Stopwatch();
                            while (!View.IsClosing && isMultiThreaded)
                            {
                                double delta = ((double)sw.ElapsedTicks / Stopwatch.Frequency);
                                sw.Restart();

                                Render(delta);
                            }

                            if (View.IsClosing)
                                return;

                            lock (renderLock)
                            {
                                View.GLContext.Clear();
                                renderThreadID = INACTIVE_THREAD;
                            }
                        }));
                    }
                }
            }
        }

        public void Load(IInputContext inputContext)
        {
            if (Instance is not null)
                throw new Exception("A instance is already loaded, dont call this method anywhere, it's automatic from the respected hosts");

            Instance = this;

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
                Utils.Log($"Failed to set GCMode to lowlatency: {ex.Message}", LogLevel.Error);
            }

            View.GLContext.SwapInterval(0);
            GL.Instance.DepthFunc(Silk.NET.OpenGLES.DepthFunction.Less);
            Input.SetContext(inputContext);

            OnLoad();
        }

        private double totalRenderTime;
        private int fps;
        /// <summary>
        /// This gets called by the game host
        /// </summary>
        /// <param name="delta"></param>
        public void Render(double delta)
        {
            //Return if multithreaded and current render is not comming from the right thread;
            if (renderThreadID != INACTIVE_THREAD && System.Threading.Thread.CurrentThread.ManagedThreadId != renderThreadID)
                return;

            lock (renderLock)
            {
                if (View.GLContext.IsCurrent == false)
                {
                    View.GLContext.MakeCurrent();
                    View.GLContext.SwapInterval(0);
                }

                RenderDeltaTime = delta;

                GL.Instance.Clear(ClearBufferMask.ColorBufferBit);

                //Unfortunately syncing of the rendering and upating is kinda mandatory
                //So this will block the update thread while its rendering,
                //Causing oppotunities for input lag, since input thread alos manages window input

                //Conversely, this would wait for the update thread to complete, who ever locks it first  i guess
                OnRender(delta);

                if (View.IsClosing == false)
                    View.SwapBuffers();

                GPUSched.Instance.RunPendingTasks();
                PostProcessing.Update((float)delta);

                fps++;

                totalRenderTime += delta;

                TotalTime += delta;
                DeltaTime = delta;

                if (totalRenderTime >= 1)
                {
                    FPS = fps;
                    totalRenderTime -= 1;
                    fps = 0;

                    if (PrintFPS)
                        Console.WriteLine($"FPS: {FPS}");
                }
            }
        }

        private double elapsedUpdate;
        private int update;
        /// <summary>
        /// This gets called by the game host
        /// </summary>
        /// <param name="delta"></param>
        public void Update(double delta) 
        {
            elapsedUpdate += delta;
            update++;

            if(elapsedUpdate >= 1)
            {
                UPS = update;
                elapsedUpdate -= 1;
                update = 0;
            }

            OnUpdate(delta);
        }

        public double TotalTime { get; private set; }
        public double DeltaTime { get; private set; }

        public double RenderDeltaTime { get; private set; }

        public abstract void OnRender(double delta);
        public abstract void OnUpdate(double delta);

        public abstract void OnImportFile(string path);

        public abstract void OnLoad();

        public abstract void OnResize(int width, int height);
    }
    */
}
