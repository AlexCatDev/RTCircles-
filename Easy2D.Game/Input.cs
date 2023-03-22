using System.Numerics;
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

        /// <summary>
        /// Only affects raw cursor mode
        /// </summary>
        public static float MouseSensitivity = 1f;

        private static Vector2 _mousePosition;

        public static Vector2 MousePosition
        {
            get
            {
                if (InputContext.Mice[0].Cursor.CursorMode == CursorMode.Raw)
                    return _mousePosition;
                else
                {
                    var windowMousePos = InputContext.Mice[0].Position;
                    return new Vector2(windowMousePos.X, windowMousePos.Y);
                }
            }
        }

        private static CursorMode cursorMode;
        public static CursorMode CursorMode
        {
            get { return cursorMode; }
            set
            {
                cursorMode = value;
                InputContext.Mice[0].Cursor.CursorMode = value;
            }
        }

        public static void SetContext(IInputContext inputContext)
        {
            InputContext = inputContext;

            //This returns the Window Cursor Position if CursorMode is not raw
            //Else it returns the mouse delta

            var mouse = inputContext.Mice[0];

            Vector2 previousRaw = Vector2.Zero;

            _mousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);

            mouse.MouseMove += (s, e) =>
            {
                if(mouse.Cursor.CursorMode == CursorMode.Raw)
                {
                    Vector2 rawMousePos = new Vector2(e.X, e.Y);

                    Vector2 mouseDelta = rawMousePos - previousRaw;
                    previousRaw = rawMousePos;

                    _mousePosition += (MouseSensitivity * mouseDelta);
                    _mousePosition.X.ClampRef(0, Viewport.Width);
                    _mousePosition.Y.ClampRef(0, Viewport.Height);
                }
                else
                {
                    previousRaw = Vector2.Zero;
                    _mousePosition = new Vector2(e.X, e.Y);
                }
            };
            
        }

        public static event Action OnBackPressed;
        public static void BackPressed()
        {
            OnBackPressed?.Invoke();
        }

        public static IReadOnlyList<TouchFingerEvent> TouchInputs => TouchFingerEvents.AsReadOnly();

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
