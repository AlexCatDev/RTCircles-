using Easy2D;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RTCircles
{
    //This is a fine class, but i want to redo with better pragmatism
    public static class ScreenManager
    {
        private static List<Screen> screens = new List<Screen>();
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

                        screens.Add(screen);
                    }
                }
            }
        }

        public static T GetScreen<T>() where T : Screen {
            foreach (var screen in screens) {
                if (screen is T)
                    return (screen as T);
            }

            return null;
        }

        private static Screen getScreen(Type type)
        {
            foreach (var screen in screens)
            {
                if (screen.GetType() == type)
                    return screen;
            }

            return null;
        }

        public static Screen ActiveScreen() => currentScreen;
        public static bool InTransition => inIntroSequence || inOutroSequence;

        private static Stack<Type> screenStack = new Stack<Type>();

        public static void GoBack()
        {
            if(screenStack.Count == 0)
            {
                Utils.Log($"Tried to go back but no more screens!", LogLevel.Error);
                return;
            }

            if (inIntroSequence || inOutroSequence)
            {
                Utils.Log($"{currentScreen.GetType().Name} tried to go back to {screenStack.Peek().Name} mid-transition!", LogLevel.Error);
                return;
            }

            Type screenType = screenStack.Pop();

            var screen = getScreen(screenType);

            Utils.Log($"{screen.GetType().Name} <- {currentScreen.GetType().Name}", LogLevel.Info);

            inIntroSequence = true;
            inOutroSequence = false;
            currentScreen.OnExiting();
            screenToTransitionTo = screen;
            OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);

        }

        public static void SetScreen<T>(bool allowGoBack = true, bool force = false) where T : Screen {
            if(currentScreen is null)
            {
                currentScreen = screens.Find((o) => o.GetType() == typeof(T));
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

            foreach (var screen in screens) {
                if (screen.GetType() == typeof(T)) {

                    if(allowGoBack)
                        screenStack.Push(currentScreen.GetType());

                    inIntroSequence = true;
                    inOutroSequence = false;
                    currentScreen.OnExiting();
                    screenToTransitionTo = screen;
                    OnScreenChange?.Invoke(currentScreen, screenToTransitionTo);

                    return;
                }
            }
            throw new Exception("Theres no such screen in the collection");
        }
        public static void Render(Graphics g) {
            currentScreen.Render(g);

            float DeltaTime = (float)MainGame.Instance.RenderDeltaTime;

            if (inIntroSequence) {
                bool? hasCompleted = OnIntroTransition?.Invoke(DeltaTime);
                if ((hasCompleted.HasValue && hasCompleted.Value == true) || !hasCompleted.HasValue) {
                    currentScreen.OnExit();
                    currentScreen = screenToTransitionTo;

                    currentScreen.OnEntering();

                    //Potential lag spike ahead because of content loading and previous content unloading!
                    //set the lag flag
                    LagFlag = true;
                    inOutroSequence = true;
                    inIntroSequence = false;
                }
            } else if (inOutroSequence) {
                //If lag flag is set, set the outro update delta this one time to 0 for smoother animation
                if (LagFlag) {
                    DeltaTime = 0;
                    LagFlag = false;
                }

                bool? hasCompleted = OnOutroTransition?.Invoke(DeltaTime);
                if ((hasCompleted.HasValue && hasCompleted.Value == true) || !hasCompleted.HasValue) {
                    currentScreen.OnEnter();
                    inOutroSequence = false;
                    inIntroSequence = false;
                }
            }
        }

        public static void Update(float delta) {
            currentScreen.Update(delta);
        }

        public static void OnTextInput(char c)
        {
            if (!inIntroSequence)
                currentScreen.OnTextInput(c);
        }

        public static void OnKeyDown(Key key) {
            if (!inIntroSequence)
                currentScreen.OnKeyDown(key);
        }
        public static void OnKeyUp(Key key) {
            if (!inIntroSequence)
                currentScreen.OnKeyUp(key);
        }
        public static void OnMouseDown(MouseButton button)
        {
            if (!inIntroSequence)
                currentScreen.OnMouseDown(button);
        }
        public static void OnMouseUp(MouseButton button) {
            if (!inIntroSequence)
                currentScreen.OnMouseUp(button);
        }
        public static void OnMouseWheel(float delta) {
            if (!inIntroSequence)
                currentScreen.OnMouseWheel(delta);
        }
    }
}
