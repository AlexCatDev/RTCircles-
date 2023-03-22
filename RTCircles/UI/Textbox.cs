using Easy2D;
using System.Numerics;
using System;
using Silk.NET.Input;
using Silk.NET.OpenGLES;
using Easy2D.Game;

namespace RTCircles
{
    public class FallingText : Drawable
    {
        private Vector2 position;
        private string text;
        private float scale;
        private Vector4 color;
        private float gravity = 0;
        private Textbox parent;

        public FallingText(Textbox parent, Vector2 position, Vector4 color, string text, float scale)
        {
            this.position = position;
            this.text = text;
            this.scale = scale;
            this.color = color;

            this.parent = parent;
        }

        public override Rectangle Bounds => new Rectangle(position, new Vector2(100, 100));

        public override void Render(Graphics g)
        {
            g.DrawClippedString(text, Font.DefaultFont, position, color, parent.Bounds, scale, alignText: false);
            //g.DrawStringNoAlign(text, Font.DefaultFont, position, color, scale);
        }

        public override void Update(float delta)
        {
            position.Y += gravity * delta;
            gravity += 5000f * delta;

            if (position.Y > 3000)
                IsDead = true;
        }
    }

    //recode this piece of shit
    public class Textbox : Drawable
    {
        private DrawableContainer fallingTextContainer = new DrawableContainer();

        public override Rectangle Bounds => new Rectangle(Position, Size);

        private string text = "";
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                if(insertIndex > text.Length)
                    insertIndex = text.Length;
            }
        }

        public string TextHint = "";

        private Animation caretAnimation = new Animation();

        public bool HasFocus = false;

        public Vector2 Position;
        public Vector2 Size = new Vector2(256, 40);

        public Vector4 TextColor = Colors.Black;
        public Vector4 Color = Colors.White;
        public Vector4 CaretColor = Colors.Black;
        public Vector4 TextHintColor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        public Vector4 TextDeleteColor = Colors.Red;

        public float CaretThickness => Size.Y / 12f;

        public event EventHandler OnEnter;
        public event Action OnTextChanged;

        public bool RemoveFocusOnEnter = true;
        public bool RemoveFocus = true;

        public bool Disabled;

        private int insertIndex;

        public Textbox()
        {
            caretAnimation.From = 1f;
            caretAnimation.To = 0f;
            caretAnimation.Easing = EasingTypes.Out;
            caretAnimation.Duration = 0.5f;
        }

        public override void OnRemove()
        {

        }

        private bool controlDown;

        public override bool OnKeyDown(Key key)
        {
            if (Disabled)
                return false;

            string startText = Text;
            if (HasFocus)
            {
                if (key == Key.ControlLeft)
                    controlDown = true;

                if (key == Key.Backspace && Text.Length > 0)
                {
                    if (controlDown)
                    {
                        int nearestSpace = Text.LastIndexOf(' ');

                        if (nearestSpace == -1)
                            nearestSpace = 0;

                        Text = Text.Remove(nearestSpace, insertIndex - nearestSpace);
                        insertIndex = nearestSpace;
                    }
                    else
                    {
                        if (insertIndex > 0)
                        {
                            insertIndex = (insertIndex - 1).Clamp(0, Text.Length - 1);
                            Text = Text.Remove(insertIndex, 1);
                        }
                    }
                }

                if (key == Key.Enter)
                {
                    if (RemoveFocusOnEnter)
                        HasFocus = false;

                    OnEnter?.Invoke(this, EventArgs.Empty);
                    Input.InputContext.Keyboards[0].EndInput();
                }

                
                if (key == Key.V && controlDown)
                {
                    string clipboard = Input.InputContext.Keyboards[0].ClipboardText;
                    clipboard = clipboard.Replace('\n', '\0');
                    Text = Text.Insert(insertIndex, clipboard);
                    insertIndex += clipboard.Length;
                }
                

                if (startText != Text)
                {
                    OnTextChanged?.Invoke();

                    if (Text.Length < startText.Length)
                    {
                        fallingTextContainer.Add(new FallingText(this, realCaretPos, TextDeleteColor, startText.Substring(insertIndex, startText.Length - Text.Length), textScale));
                    }
                }

                if(key == Key.Left)
                {
                    insertIndex = (insertIndex - 1).Clamp(0, Text.Length - 1);

                    if (controlDown)
                        insertIndex = 0;
                }
                else if (key == Key.Right)
                {
                    insertIndex = (insertIndex + 1).Clamp(0, Text.Length);

                    if (controlDown)
                        insertIndex = Text.Length;
                }
            }

            return false;
        }

        public override bool OnKeyUp(Key key)
        {
            if (Disabled)
                return false;

            if (key == Key.ControlLeft)
                controlDown = false;

            return false;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (Disabled)
                return false;

            if (new Rectangle(Position, Size).IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
            {
                HasFocus = true;
                Input.InputContext.Keyboards[0].BeginInput();
                caretAnimation.Reset();

                insertIndex = text.Length;

                return true;
            }
            else
            {
                Input.InputContext.Keyboards[0].EndInput();

                if (RemoveFocus)
                    HasFocus = false;

                return false;
            }
        }

        private Vector2 caretPos;

        private Vector2 textSize => Font.DefaultFont.MessureString(Text.Substring(0, insertIndex), textScale, true);

        private Vector2 realCaretPos => new Vector2(textPos.X + textSize.X, Position.Y + 1);

        private float textScale => (Size.Y / Font.DefaultFont.Size);
        private Vector2 textPos => new Vector2(Position.X, Position.Y) + caretSize;
        private Vector2 caretSize => new Vector2(CaretThickness, 0);

        public override void Render(Graphics g)
        {
            g.DrawRectangle(Position, Size, Color);
            
            if (!string.IsNullOrEmpty(TextHint) && string.IsNullOrEmpty(Text))
                g.DrawStringNoAlign(TextHint, Font.DefaultFont, textPos, TextHintColor, textScale);

            if (!string.IsNullOrEmpty(Text))
                g.DrawStringNoAlign(Text, Font.DefaultFont, textPos, TextColor, textScale);

            fallingTextContainer.Render(g);

            float caretAlpha = caretAnimation.Output;

            if(OsuContainer.CurrentBeatTimingPoint != null)
                caretAlpha = (float)OsuContainer.CurrentBeat.OscillateValue(0, 1);

            if (HasFocus)
                g.DrawRectangle(caretPos, new Vector2(CaretThickness, Size.Y - CaretThickness / 2f), new Vector4(CaretColor.X, CaretColor.Y, CaretColor.Z, caretAlpha));
        }

        public override void Update(float delta)
        {
            caretAnimation.Update(delta);

            if (caretAnimation.IsCompleted)
            {
                caretAnimation.From = caretAnimation.From == 1f ? 0f : 1f;
                caretAnimation.To = caretAnimation.To == 1f ? 0f : 1f;
                caretAnimation.Reset();
            }

            caretPos = Vector2.Lerp(caretPos, realCaretPos, 80f * delta);

            if (caretPos == Vector2.Zero)
                caretPos = textPos;

            fallingTextContainer.Update(delta);
        }

        public override bool OnTextInput(char c)
        {
            if (Disabled)
                return false;

            if (HasFocus)
            {
                Text = Text.Insert(insertIndex, c.ToString());
                insertIndex += 1;
                OnTextChanged?.Invoke();
            }

            return false;
        }
    }
}
