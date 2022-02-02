using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using Silk.NET.Input;
using Silk.NET.OpenGLES;
using Silk.NET.SDL;
using Silk.NET.Windowing;

namespace Easy2D.Game
{
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

                OnUpdate(delta);
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

        /// <summary>
        /// This gets called by the game host
        /// </summary>
        /// <param name="delta"></param>
        public void Update(double delta) { }

        public double TotalTime { get; private set; }
        public double DeltaTime { get; private set; }

        public double RenderDeltaTime { get; private set; }

        public abstract void OnRender(double delta);
        public abstract void OnUpdate(double delta);

        public abstract void OnImportFile(string path);

        public abstract void OnLoad();

        public abstract void OnResize(int width, int height);
    }
}
