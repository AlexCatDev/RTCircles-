using Easy2D;
using OpenTK.Mathematics;
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

        public FallingText(Vector2 position, Vector4 color, string text, float scale)
        {
            this.position = position;
            this.text = text;
            this.scale = scale;
            this.color = color;
        }

        public override Rectangle Bounds => new Rectangle(position, new Vector2(100, 100));

        public override void Render(Graphics g)
        {
            g.DrawStringNoAlign(text, Font.DefaultFont, position, color, scale);
        }

        public override void Update(float delta)
        {
            position.Y += gravity * delta;
            gravity += 5000f * delta;

            if (position.Y > 3000)
                IsDead = true;
        }
    }

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

        private FrameBuffer textFramebuffer = new FrameBuffer(1,1, textureComponentCount: InternalFormat.Rgba16f, pixelType: PixelType.Float);

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

                /*
                if (key == Key.V && controlDown)
                {
                    string clipboard = Game.Instance.GetClipboard();
                    clipboard = clipboard.Replace('\n', '\0');
                    Text = Text.Insert(insertIndex, clipboard);
                    insertIndex += clipboard.Length;
                }
                */

                if (startText != Text)
                {
                    OnTextChanged?.Invoke();

                    if (Text.Length < startText.Length)
                        fallingTextContainer.Add(new FallingText(fboCaretPos, TextDeleteColor, startText.Substring(insertIndex, startText.Length - Text.Length), textScale));
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
            if (key == Key.ControlLeft)
                controlDown = false;

            if (Disabled)
                return false;

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
        private Vector2 textPos => new Vector2(Position.X, Position.Y) + fboTextPos;
        private Vector2 fboTextPos => new Vector2(CaretThickness, 0);
        private Vector2 fboCaretPos => new Vector2(textSize.X, 1) + fboTextPos;

        public override void Render(Graphics g)
        {
            g.DrawRectangle(Position, Size, Color);

            textFramebuffer.Resize(Size.X, Size.Y);
            
            g.DrawInFrameBuffer(textFramebuffer, () =>
            {
                if (!string.IsNullOrEmpty(TextHint) && string.IsNullOrEmpty(Text))
                    g.DrawStringNoAlign(TextHint, Font.DefaultFont, fboTextPos, TextHintColor, textScale);

                if (!string.IsNullOrEmpty(Text))
                    g.DrawStringNoAlign(Text, Font.DefaultFont, fboTextPos, TextColor, textScale);

                fallingTextContainer.Render(g);
            });
            
            g.DrawFrameBuffer(Position, Colors.White, textFramebuffer);

            if (HasFocus)
                g.DrawRectangle(caretPos, new Vector2(CaretThickness, Size.Y - CaretThickness / 2f), new Vector4(CaretColor.X, CaretColor.Y, CaretColor.Z, caretAnimation.Output));
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

    //Todo revamp, better animations, highlight and cursor control, better design, sounds etc etc
    //public class Textbox : Drawable
    //{
    //    public override Rectangle Bounds => new Rectangle(Position, Size);

    //    public string Text = "";
    //    public string TextHint = "";

    //    private Animation caretAnimation = new Animation();

    //    public bool HasFocus = false;

    //    public Vector2 Position;
    //    public Vector2 Size = new Vector2(256, 40);

    //    public event EventHandler OnEnter;
    //    public event Action OnTextChanged;

    //    public bool RemoveFocusOnEnter = true;
    //    public bool RemoveFocus = true;

    //    public bool Disabled;

    //    public Textbox()
    //    {
    //        caretAnimation.From = 1f;
    //        caretAnimation.To = 0f;
    //        caretAnimation.Easing = EasingTypes.Out;
    //        caretAnimation.Duration = 0.5f;
    //    }

    //    public override void OnRemove()
    //    {

    //    }

    //    public override bool OnKeyDown(KeyboardKeyEventArgs key)
    //    {
    //        int hash = Text.GetHashCode();
    //        if (HasFocus)
    //        {
    //            if (key.Key == Keys.Backspace && Text.Length > 0)
    //            {
    //                if (key.Control)
    //                    Text = "";
    //                else
    //                    Text = Text.Remove(Text.Length - 1);
    //            }

    //            if(key.Key == Keys.Enter && !key.IsRepeat)
    //            {
    //                if(RemoveFocusOnEnter)
    //                    HasFocus = false;

    //                OnEnter?.Invoke(this, EventArgs.Empty);
    //            }

    //            if (key.Key == Keys.V && key.Control)
    //            {
    //                string clipboard = Game.Instance.GetClipboard();
    //                clipboard = clipboard.Replace('\n', '\0');
    //                Text += clipboard;
    //            }

    //            if(hash != Text.GetHashCode())
    //                OnTextChanged?.Invoke();
    //        }

    //        return false;
    //    }

    //    public override bool OnKeyUp(KeyboardKeyEventArgs key)
    //    {
    //        return false;
    //    }

    //    public override bool OnMouseDown(float x, float y, MouseButton args)
    //    {
    //        if (Disabled)
    //            return false;

    //        if (new Rectangle(Position, Size).IntersectsWith(new Rectangle(x, y, 2, 2)))
    //        {
    //            HasFocus = true;
    //            caretAnimation.Reset();
    //        }
    //        else
    //        {
    //            if(RemoveFocus)
    //                HasFocus = false;
    //        }
    //            return false;
    //    }

    //    public override bool OnMouseMove(float x, float y)
    //    {
    //        return false;
    //    }

    //    public override bool OnMouseUp(float x, float y, MouseButton args)
    //    {
    //        return false;
    //    }

    //    public override bool OnMouseWheel(float delta)
    //    {
    //        return false;
    //    }

    //    public override void Render(Graphics g)
    //    {
    //        float textScale = (Size.Y / Font.DefaultFont.Size);

    //        Vector2 textSize = Font.DefaultFont.MessureString(Text, textScale, true);

    //        g.DrawRectangle(Position, Size, Colors.White);

    //        Vector2 textPos = new Vector2(Position.X + 4, Position.Y);

    //        Vector2 caretPos = new Vector2(textPos.X + textSize.X, Position.Y + 1);

    //        if (!string.IsNullOrEmpty(TextHint) && string.IsNullOrEmpty(Text))
    //            g.DrawStringNoAlign(TextHint, Font.DefaultFont, textPos, new Vector4(0.5f, 0.5f, 0.5f, 0.5f), textScale);

    //        if (!string.IsNullOrEmpty(Text))
    //            g.DrawStringNoAlign(Text, Font.DefaultFont, textPos, Colors.Black, textScale);

    //        if (HasFocus)
    //            g.DrawRectangle(caretPos, new Vector2(4, Size.Y - 2), new Vector4(0f, 0f, 0f, caretAnimation.Output));
    //    }

    //    public override void Update(float delta)
    //    {
    //        caretAnimation.Update(delta);

    //        if (caretAnimation.IsCompleted)
    //        {
    //            caretAnimation.From = caretAnimation.From == 1f ? 0f : 1f;
    //            caretAnimation.To = caretAnimation.To == 1f ? 0f : 1f;
    //            caretAnimation.Reset();
    //        }
    //    }

    //    public override bool OnTextInput(TextInputEventArgs args)
    //    {
    //        if (HasFocus)
    //        {
    //            Text += args.AsString;
    //            OnTextChanged?.Invoke();
    //        }

    //        return false;
    //    }
    //}
}
