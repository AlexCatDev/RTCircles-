using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class GoodSliderBar : Drawable
    {
        public Vector2 Position;

        public Vector2 Size;

        public override Rectangle Bounds => new Rectangle(Position, Size);

        public Vector4 BackgroundColor;
        public Vector4 ForegroundColor;

        public bool IsLocked = false;

        public double MinimumValue = 0;
        public double MaximumValue = 1;

        private double internalValue;

        public double Value { 
            get 
            { 
                return internalValue; 
            } 
            set {
                if (IsLocked)
                    return;

                internalValue = value.Clamp(MinimumValue, MaximumValue);

                internalValue = Math.Round(internalValue, 6);
            } 
        }

        public event Action<double> ValueChanged;

        public override void Render(Graphics g)
        {
            drawBar(g, BackgroundColor, 1);
            drawBar(g, ForegroundColor, smoothInternalValue.Map(MinimumValue, MaximumValue, 0, 1));
        }

        private void drawBar(Graphics g, Vector4 color, double progress)
        {
            var capTexture = Texture.WhiteFlatCircle2;

            Vector2 circleSize = new Vector2(Size.Y / 2f, Size.Y);

            Vector2 leftCircleSize = new Vector2((float)progress.Map(0, 1, 0, Size.X).Clamp(0, circleSize.X), circleSize.Y);
            float leftCircleUV = leftCircleSize.X.Map(0, circleSize.X, 0, 0.5f);
            //Venstre halv cirkel
            g.DrawRectangle(Position, leftCircleSize, color, capTexture, new Rectangle(0, 0, leftCircleUV, 1), true);

            Vector2 barSize = new Vector2((float)(progress.Map(0, 1, 0, Size.X) - circleSize.X).Clamp(0, Size.X - circleSize.X * 2), circleSize.Y);

            //Midbar
            g.DrawRectangle(Position + new Vector2(circleSize.X, 0), barSize, color);

            Vector2 rightCircleSize = new Vector2((float)(progress.Map(0, 1, 0, Size.X) - Size.X + circleSize.X).Clamp(0, circleSize.X), circleSize.Y);
            float rightCircleUV = rightCircleSize.X.Map(0, circleSize.X, 0, 0.5f);

            //Højre halv cirkel
            g.DrawRectangle(Position + new Vector2(Size.X - circleSize.X, 0), rightCircleSize, color, capTexture, new Rectangle(0.5f, 0, rightCircleUV, 1), true, 0);

        }

        private Vector2? mouseDownPos;

        private bool mouseHover => new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(Bounds);

        public double KeyboardStep = 0.01;

        public override bool OnKeyDown(Key key)
        {
            if(mouseHover)
            {
                if (key == Key.Left)
                {
                    Value -= KeyboardStep;
                    ValueChanged?.Invoke(Value);
                    return true;
                }
                else if(key == Key.Right)
                {
                    Value += KeyboardStep;
                    ValueChanged?.Invoke(Value);
                    return true;
                }
            }

            return false;
        }

        public override bool OnMouseDown(MouseButton button)
        {
            if(button == MouseButton.Left && mouseHover)
            {
                mouseDownPos = Input.MousePosition;
                return true;
            }

            mouseDownPos = null;

            return false;
        }

        public override bool OnMouseUp(MouseButton button)
        {
            if(button == MouseButton.Left)
                mouseDownPos = null;

            return base.OnMouseUp(button);
        }

        public int StepDecimals = 1;

        private float playSound = 0;

        private double smoothInternalValue = 0;

        public override void Update(float delta)
        {
            //smoothInternalValue = MathHelper.Lerp(smoothInternalValue, internalValue, 10f * delta);
            smoothInternalValue = Interpolation.Damp(smoothInternalValue, internalValue, 0.000001, delta);


            var mousePos = Input.MousePosition;

            playSound += delta;

            if (mouseDownPos.HasValue && !IsLocked)
            {
                double x = mousePos.X;
                double val = x.Map(Position.X, Position.X + Size.X, MinimumValue, MaximumValue).Clamp(MinimumValue, MaximumValue);

                val = Math.Round(val, StepDecimals);
                if(val != internalValue)
                {
                    internalValue = val;

                    ValueChanged?.Invoke(internalValue);

                    if (playSound >= 0.06f)
                    {
                        Skin.Click.Play(true);
                        playSound = 0;
                    }
                }
            }
        }
    }
}
