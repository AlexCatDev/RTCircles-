using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK;
using Easy2D;
using Silk.NET.Input;
using Easy2D.Game;

namespace RTCircles
{
    public class SliderBar : Drawable
    {
        public bool IsVisible = true;

        public string Text;

        public int MinValue = 1;
        public int MaxValue = 100;

        private int internalValue;

        public int Value {
            get { return internalValue; }
            set {
                if (!hasFocus) {
                    internalValue = value.Clamp(MinValue, MaxValue);
                    lastValue = internalValue;
                }
            }
        }

        public override Rectangle Bounds => new Rectangle(buttonPos, ButtonSize);

        private int lastValue;

        public float BarLength;
        public float BarThickness = 3f;
        public Vector2 Position;

        public Vector4 BarColor = Colors.Blue;

        public Vector2 ButtonSize = new Vector2(40, 40);

        public Texture ButtonTexture;

        private Vector2 buttonPos;
        private Vector2 clickOffset;

        private bool hasFocus = false;

        public event Action<int> ValueChanged;
        public event Action<int> DragStart;
        public event Action<int> DragEnd;

        private float soundPlay = 0.06f;

        public void SetValue(int value)
        {
            if (!hasFocus)
                internalValue = value.Clamp(MinValue, MaxValue);
        }

        public override bool OnMouseDown(MouseButton args) {
            if (!IsVisible)
                return false;

            float x = Input.MousePosition.X;
            float y = Input.MousePosition.Y;

            if(new Rectangle(x, y, 2, 2).IntersectsWith(new Rectangle(buttonPos, ButtonSize)) && args == MouseButton.Left) {
                hasFocus = true;
                clickOffset = buttonPos - new Vector2(x, y);
                DragStart?.Invoke(Value);
                return true;
            }

            return false;
        }

        private bool OnMouseMove(float x, float y) {
            if (!IsVisible)
                return false;

            if (hasFocus) {
                x += clickOffset.X;
                y += clickOffset.Y;

                float value = MathUtils.Map(x, Position.X, Position.X + BarLength - ButtonSize.X, MinValue, MaxValue).Clamp(MinValue, MaxValue);

                internalValue = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            }

            return false;
        }

        public override bool OnMouseUp(MouseButton args) {
            if (!IsVisible)
                return false;

            float x = Input.MousePosition.X;
            float y = Input.MousePosition.Y;

            if (hasFocus)
            {
                hasFocus = false;
                DragEnd?.Invoke(Value);
            }
            return false;
        }

        private Vector2 lastMousePos;

        public override void Update(float delta) {
            if (!IsVisible)
                return;

            if (Input.MousePosition != lastMousePos)
            {
                lastMousePos = Input.MousePosition;
                OnMouseMove(lastMousePos.X, lastMousePos.Y);
            }

            internalValue = internalValue.Clamp(MinValue, MaxValue);

            buttonPos.X = MathUtils.Map(Value, MinValue, MaxValue, Position.X, Position.X + BarLength - ButtonSize.X);
            buttonPos.Y = Position.Y - ButtonSize.Y / 2f;

            soundPlay -= delta;

            if (lastValue != Value) {
                lastValue = Value;

                if (soundPlay <= 0) {
                    soundPlay = 0.06f;
                    Skin.Hover.Play(true);
                }

                ValueChanged?.Invoke(Value);
            }
        }

        public override void Render(Graphics g)
        {
            if (!IsVisible)
                return;

            g.DrawRectangle(new Vector2(Position.X, Position.Y - BarThickness / 2f), new Vector2(BarLength, BarThickness), BarColor);

            g.DrawRectangle(buttonPos, ButtonSize, hasFocus ? Colors.LightGray : Colors.White, ButtonTexture);

            if (hasFocus && Text != null)
            {
                Vector2 textSize = Font.DefaultFont.MessureString(Text, 0.5f * MainGame.Scale);

                var padding = new Vector2(10, 10);

                var pos = Input.MousePosition + new Vector2(16) * MainGame.Scale;

                pos.X = pos.X.Clamp(0, MainGame.WindowWidth - textSize.X);
                pos.Y = pos.Y.Clamp(0, MainGame.WindowHeight - textSize.Y);

                g.DrawRectangle(pos - padding / 2f, textSize + padding, new Vector4(0.05f, 0.05f, 0.05f, 1f));
                g.DrawString(Text, Font.DefaultFont, pos, Colors.White, 0.5f * MainGame.Scale);
            }
        }
    }
}
