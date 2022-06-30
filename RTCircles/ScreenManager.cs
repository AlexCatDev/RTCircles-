using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RTCircles
{
    //This is a fine class, but i want to redo with better pragmatism


    public static class ScreenManager
    {
        private static Dictionary<Type, Screen> screens = new Dictionary<Type, Screen>();

        private static Screen screenToTransitionTo;
        private static Screen currentScreen;
        private static bool inIntroSequence = false;
        private static bool inOutroSequence = false;
        private static bool LagFlag = false;
        /// <summary>
        /// This event is fired continuously when the screens are transitioning
        /// </summary>
        /// <param name="delta">Delta time for screen transitioning</param>
        /// <returns>a bool that determines if the transition is done</returns>
        public delegate bool TransitionFunc(float delta);
        public delegate void ScreenChangeFunc(Screen from, Screen to);
        public static event TransitionFunc OnIntroTransition;
        public static event TransitionFunc OnOutroTransition;
        public static event ScreenChangeFunc OnScreenChange;

        public static bool CanChangeScreen => inIntroSequence == false && inOutroSequence == false;
        static ScreenManager()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsInterface || type.IsAbstract)
                    continue;

                else
                {
                    if (typeof(Screen).IsAssignableFrom(type))
                    {
                        Utils.Log($"Added screen: {type.Name}", LogLevel.Debug);
                        Screen screen = (Screen)Activator.CreateInstance(type);
                        screens.Add(screen.GetType(), screen);
                    }
                }
            }
        }

        public static T GetScreen<T>() where T : Screen
        {
            return screens[typeof(T)] as T;
        }

        public static Screen ActiveScreen => currentScreen;
        public static bool InTransition => inIntroSequence || inOutroSequence;

        private static Stack<Type> screenHistory = new Stack<Type>();

        private static double transitionStartTime = 0;
        private static bool captureScreenFlag = false;
        private static bool isBackwards = false;
        private static FrameBuffer previousScreenFramebuffer = new FrameBuffer(1, 1, 
            Silk.NET.OpenGLES.FramebufferAttachment.ColorAttachment0, 
            Silk.NET.OpenGLES.InternalFormat.Rgb16f, 
            Silk.NET.OpenGLES.PixelFormat.Rgb, 
            Silk.NET.OpenGLES.PixelType.Float);

        public static void GoBack()
        {
            if (screenHistory.Count == 0)
            {
                Utils.Log($"Tried to go back but no more screens!", LogLevel.Error);
                return;
            }

            if (inIntroSequence || inOutroSequence)
            {
                Utils.Log($"{currentScreen.GetType().Name} tried to go back to {screenHistory.Peek().Name} mid-transition!", LogLevel.Error);
                return;
            }

            Type screenType = screenHistory.Pop();

            var screen = screens[screenType];

            Utils.Log($"{screen.GetType().Name} <- {currentScreen.GetType().Name}", LogLevel.Info);

            transitionStartTime = MainGame.Instance.TotalTime;
            captureScreenFlag = true;
            isBackwards = true;
            inIntroSequence = true;
            inOutroSequence = false;
            currentScreen.OnExiting();
            screenToTransitionTo = screen;
            OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);
        }

        public static void SetScreen<T>(bool allowGoBack = true, bool force = false) where T : Screen
        {
            if (currentScreen is null)
            {
                currentScreen = screens[typeof(T)];
                currentScreen.OnEnter();
                return;
            }

            if (inIntroSequence || inOutroSequence && !force)
            {
                Utils.Log($"{currentScreen.GetType().Name} tried to change to {typeof(T).Name} mid-transition!", LogLevel.Error);
                return;
            }

            if (currentScreen is T)
                return;

            Utils.Log($"{currentScreen.GetType().Name} -> {typeof(T).Name}", LogLevel.Info);


            if (screens.TryGetValue(typeof(T), out var screen))
            {
                if (allowGoBack)
                    screenHistory.Push(currentScreen.GetType());

                transitionStartTime = MainGame.Instance.TotalTime;
                isBackwards = false;
                captureScreenFlag = true;
                inIntroSequence = true;
                inOutroSequence = false;
                currentScreen.OnExiting();
                screenToTransitionTo = screen;
                OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);

                return;
            }

            throw new Exception("Theres no such screen in the collection");
        }

        public static void Render(Graphics g)
        {
            const double DURATION = 0.5;

            const double FADE_IN = 0.15;
            const double FADE_OUT = 0.3;

            if (inIntroSequence)
            {
                if (captureScreenFlag)
                {
                    previousScreenFramebuffer.EnsureSize(MainGame.WindowSize.X, MainGame.WindowSize.Y);

                    g.DrawInFrameBuffer(previousScreenFramebuffer, () =>
                    {
                        currentScreen.Render(g);
                    });

                    captureScreenFlag = false;

                    currentScreen.OnExit();

                    currentScreen = screenToTransitionTo;

                    currentScreen.OnEntering();
                }

                double startTime = transitionStartTime;
                double endTime = transitionStartTime + DURATION;
                var easing = EasingTypes.InOutExpo;

                float progress = (float)Interpolation.ValueAt(MainGame.Instance.TotalTime.Clamp(startTime, endTime),
                    0, 1, startTime, endTime, easing);

                //ååååååååå
                if (progress == 1)
                {
                    currentScreen.Render(g);

                    currentScreen.OnEnter();

                    inIntroSequence = false;
                }
                else
                {
                    var startProj = g.Projection;

                    g.Projection = Matrix4.CreateTranslation((isBackwards ? progress - 1 : 1 - progress) * MainGame.WindowWidth, 0, 0) * startProj;
                    currentScreen.Render(g);

                    //g.Projection = Matrix4.CreateTranslation((isBackwards ? progress : -progress) * MainGame.WindowWidth, 0, 0) * startProj;
                    g.DrawFrameBuffer(new Vector2(MainGame.WindowWidth * (isBackwards ? 1 : -1), 0), new Vector4(new Vector3(1), 1), previousScreenFramebuffer);
                    g.EndDraw();
                    g.Projection = startProj;
                }
            }
            else
            {
                currentScreen.Render(g);
            }
        }

        public static void Update(float delta)
        {
            currentScreen.Update(delta);
        }

        public static void OnTextInput(char c)
        {
            //if (!inIntroSequence)
            currentScreen.OnTextInput(c);
        }

        public static void OnKeyDown(Key key)
        {
            //if (!inIntroSequence)
            currentScreen.OnKeyDown(key);
        }
        public static void OnKeyUp(Key key)
        {
            //if (!inIntroSequence)
            currentScreen.OnKeyUp(key);
        }
        public static void OnMouseDown(MouseButton button)
        {
            //if (!inIntroSequence)
            currentScreen.OnMouseDown(button);
        }
        public static void OnMouseUp(MouseButton button)
        {
            //if (!inIntroSequence)
            currentScreen.OnMouseUp(button);
        }
        public static void OnMouseWheel(float delta)
        {
            //if (!inIntroSequence)
            currentScreen.OnMouseWheel(delta);
        }
    }


    //public static class ScreenManager
    //{
    //    private static List<Screen> screens = new List<Screen>();
    //    private static Screen screenToTransitionTo;
    //    private static Screen currentScreen;
    //    private static bool inIntroSequence = false;
    //    private static bool inOutroSequence = false;
    //    private static bool LagFlag = false;
    //    /// <summary>
    //    /// This event is fired continuously when the screens are transitioning
    //    /// </summary>
    //    /// <param name="delta">Delta time for screen transitioning</param>
    //    /// <returns>a bool that determines if the transition is done</returns>
    //    public delegate bool TransitionFunc(float delta);
    //    public delegate void ScreenChangeFunc(Screen from, Screen to);
    //    public static event TransitionFunc OnIntroTransition;
    //    public static event TransitionFunc OnOutroTransition;
    //    public static event ScreenChangeFunc OnScreenChange;

    //    public static bool CanChangeScreen => inIntroSequence == false && inOutroSequence == false;
    //    static ScreenManager()
    //    {
    //        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
    //        {
    //            if (type.IsInterface || type.IsAbstract)
    //                continue;

    //            else
    //            {
    //                if (typeof(Screen).IsAssignableFrom(type))
    //                {
    //                    Utils.Log($"Added screen: {type.Name}", LogLevel.Debug);
    //                    Screen screen = (Screen)Activator.CreateInstance(type);
    //                    screens.Add(screen);
    //                }
    //            }
    //        }
    //    }

    //    public static T GetScreen<T>() where T : Screen
    //    {
    //        foreach (var screen in screens)
    //        {
    //            if (screen is T)
    //                return (screen as T);
    //        }

    //        return null;
    //    }

    //    private static Screen getScreen(Type type)
    //    {
    //        foreach (var screen in screens)
    //        {
    //            if (screen.GetType() == type)
    //                return screen;
    //        }

    //        return null;
    //    }

    //    public static Screen ActiveScreen => currentScreen;
    //    public static bool InTransition => inIntroSequence || inOutroSequence;

    //    private static Stack<Type> screenStack = new Stack<Type>();

    //    public static void GoBack()
    //    {
    //        if (screenStack.Count == 0)
    //        {
    //            Utils.Log($"Tried to go back but no more screens!", LogLevel.Error);
    //            return;
    //        }

    //        if (inIntroSequence || inOutroSequence)
    //        {
    //            Utils.Log($"{currentScreen.GetType().Name} tried to go back to {screenStack.Peek().Name} mid-transition!", LogLevel.Error);
    //            return;
    //        }

    //        Type screenType = screenStack.Pop();

    //        var screen = getScreen(screenType);

    //        Utils.Log($"{screen.GetType().Name} <- {currentScreen.GetType().Name}", LogLevel.Info);

    //        inIntroSequence = true;
    //        inOutroSequence = false;
    //        currentScreen.OnExiting();
    //        screenToTransitionTo = screen;
    //        OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);
    //    }

    //    public static void SetScreen<T>(bool allowGoBack = true, bool force = false) where T : Screen
    //    {
    //        if (currentScreen is null)
    //        {
    //            currentScreen = screens.Find((o) => o.GetType() == typeof(T));
    //            currentScreen.OnEnter();
    //            return;
    //        }

    //        if (inIntroSequence || inOutroSequence && !force)
    //        {
    //            Utils.Log($"{currentScreen.GetType().Name} tried to change to {typeof(T).Name} mid-transition!", LogLevel.Error);
    //            return;
    //        }

    //        if (currentScreen is T)
    //            return;

    //        Utils.Log($"{currentScreen.GetType().Name} -> {typeof(T).Name}", LogLevel.Info);

    //        foreach (var screen in screens)
    //        {
    //            if (screen.GetType() == typeof(T))
    //            {

    //                if (allowGoBack)
    //                    screenStack.Push(currentScreen.GetType());

    //                inIntroSequence = true;
    //                inOutroSequence = false;
    //                currentScreen.OnExiting();
    //                screenToTransitionTo = screen;
    //                OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);

    //                return;
    //            }
    //        }
    //        throw new Exception("Theres no such screen in the collection");
    //    }
    //    public static void Render(Graphics g)
    //    {
    //        currentScreen.Render(g);

    //        float DeltaTime = (float)MainGame.Instance.DeltaTime;

    //        if (inIntroSequence)
    //        {
    //            bool? hasCompleted = OnIntroTransition?.Invoke(DeltaTime);
    //            if ((hasCompleted.HasValue && hasCompleted.Value == true) || !hasCompleted.HasValue)
    //            {
    //                currentScreen.OnExit();
    //                currentScreen = screenToTransitionTo;

    //                currentScreen.OnEntering();

    //                //Potential lag spike ahead because of content loading and previous content unloading!
    //                //set the lag flag
    //                LagFlag = true;
    //                inOutroSequence = true;
    //                inIntroSequence = false;
    //            }
    //        }
    //        else if (inOutroSequence)
    //        {
    //            //If lag flag is set, set the outro update delta this one time to 0 for smoother animation
    //            if (LagFlag)
    //                {
    //                    DeltaTime = 0;
    //                    LagFlag = false;
    //                }

    //            bool? hasCompleted = OnOutroTransition?.Invoke(DeltaTime);
    //            if ((hasCompleted.HasValue && hasCompleted.Value == true) || !hasCompleted.HasValue)
    //            {
    //                inOutroSequence = false;
    //                inIntroSequence = false;
    //                currentScreen.OnEnter();
    //            }
    //        }
    //    }

    //    public static void Update(float delta)
    //    {
    //        currentScreen.Update(delta);
    //    }

    //    public static void OnTextInput(char c)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnTextInput(c);
    //    }

    //    public static void OnKeyDown(Key key)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnKeyDown(key);
    //    }
    //    public static void OnKeyUp(Key key)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnKeyUp(key);
    //    }
    //    public static void OnMouseDown(MouseButton button)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnMouseDown(button);
    //    }
    //    public static void OnMouseUp(MouseButton button)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnMouseUp(button);
    //    }
    //    public static void OnMouseWheel(float delta)
    //    {
    //        if (!inIntroSequence)
    //            currentScreen.OnMouseWheel(delta);
    //    }
    //}
}
