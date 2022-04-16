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
        private FrameBuffer texture = new FrameBuffer(1, 1);

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
            if ((int)Size.X != texture.Width || (int)Size.Y != texture.Height)
            {
                texture.EnsureSize(Size.X, Size.Y);
                g.DrawInFrameBuffer(texture, () =>
                {
                    Vector2 ellipseSize = new Vector2(Size.Y / 2, Size.Y);

                    Vector2 barSize = new Vector2(Size.X - ellipseSize.X * 2, Size.Y);

                    //Højre halv cirkel
                    g.DrawRectangle(Vector2.Zero, ellipseSize, Colors.White, Texture.WhiteFlatCircle2, new Rectangle(0, 0, 0.5f, 1), true);
                    g.DrawRectangle(new Vector2(ellipseSize.X, 0), barSize, Colors.White);
                    //Venstre halv cirkel
                    g.DrawRectangle(new Vector2(Size.X - ellipseSize.X, 0), ellipseSize, Colors.White, Texture.WhiteFlatCircle2, new Rectangle(0.5f, 0, 0.5f, 1), true);
                });
            }

            var sliderTexture = texture.Texture;
            g.DrawRectangle(Position, Size, BackgroundColor, sliderTexture);

            double zeroOne = smoothInternalValue.Map(MinimumValue, MaximumValue, 0, 1);

            Vector2 foregroundSize = new Vector2((float)(Size.X * zeroOne), Size.Y);
            g.DrawRectangle(Position, foregroundSize, ForegroundColor, sliderTexture, new Rectangle(0, 0, (float)zeroOne, 1), true);
            
            //g.DrawString($"Value: {Value}", Font.DefaultFont, Input.MousePosition, Colors.Red);
            //g.DrawFrameBuffer(Input.MousePosition + new Vector2(50), Colors.White, texture);
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
