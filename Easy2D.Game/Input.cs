using OpenTK.Mathematics;
using Silk.NET.Input;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy2D.Game
{
    public static class Input
    {
        public static IInputContext InputContext { get; private set; }

        public static float MouseSensitivity = 0.5f;

        private static System.Numerics.Vector2 _previousPos = System.Numerics.Vector2.Zero;

        private static Vector2 _mousePosition;

        public static Vector2 MousePosition
        {
            get
            {
                var pos = InputContext.Mice[0].Position;
                return new Vector2(pos.X, pos.Y);
            }
        }

        internal static void SetContext(IInputContext inputContext)
        {
            InputContext = inputContext;

            //This returns the Window Cursor Position if CursorMode is not raw
            //Else it returns the mouse delta
            /*
            inputContext.Mice[0].MouseMove += (s, e) =>
            {
                Console.WriteLine(e);
            };
            */
        }

        public static event Action OnBackPressed;
        public static void BackPressed()
        {
            OnBackPressed?.Invoke();
        }

        public static List<TouchFingerEvent> TouchFingerEvents = new List<TouchFingerEvent>();

        public static event Action<TouchFingerEvent> OnFingerDown;
        public static event Action<TouchFingerEvent> OnFingerUp;
        public static event Action<TouchFingerEvent> OnFingerMove;

        public static void FingerDown(TouchFingerEvent fingerEvent)
        {
            TouchFingerEvents.Add(fingerEvent);
            OnFingerDown?.Invoke(fingerEvent);
        }

        public static void FingerUp(TouchFingerEvent fingerEvent)
        {
            for (int i = 0; i < TouchFingerEvents.Count; i++)
            {
                if (TouchFingerEvents[i].FingerId == fingerEvent.FingerId)
                {
                    TouchFingerEvents.RemoveAt(i);
                    OnFingerUp?.Invoke(fingerEvent);
                    break;
                }
            }
        }

        public static void FingerMove(TouchFingerEvent fingerEvent)
        {
            for (int i = 0; i < TouchFingerEvents.Count; i++)
            {
                if (TouchFingerEvents[i].FingerId == fingerEvent.FingerId)
                {
                    TouchFingerEvents[i] = fingerEvent;
                    OnFingerMove?.Invoke(fingerEvent);
                    break;
                }
            }
        }


        public static bool IsKeyDown(Key key) => InputContext.Keyboards[0].IsKeyPressed(key);
    }
}
